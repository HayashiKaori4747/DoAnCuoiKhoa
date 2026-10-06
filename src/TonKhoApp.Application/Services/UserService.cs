using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Constants;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Domain.Exceptions;

namespace TonKhoApp.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogger _auditLogger;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHasher passwordHasher,
            IAuditLogger auditLogger,
            ICurrentUser currentUser,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHasher = passwordHasher;
            _auditLogger = auditLogger;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResponse<UserDto>> GetUsersAsync(
            PaginationRequest pagination, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _userRepository.GetPagedAsync(
                pagination.Page, pagination.PageSize, pagination.Search, cancellationToken);

            return new PagedResponse<UserDto>
            {
                Success = true,
                Data = items.Select(ToDto).ToList(),
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize)
            };
        }

        public async Task<SingleResponse<UserDto>> GetUserAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdWithRolesAsync(id, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy tài khoản");

            return new SingleResponse<UserDto> { Success = true, Data = ToDto(user) };
        }

        public async Task<ApiResponse> CreateUserAsync(
            UserCreateRequest request, CancellationToken cancellationToken = default)
        {
            var userId = request.UserId.Trim();

            if (await _userRepository.ExistsByUserIdAsync(userId, cancellationToken))
            {
                throw new BusinessRuleException($"Tên đăng nhập '{userId}' đã tồn tại, hãy chọn tên khác");
            }

            var requested = (request.Roles == null || request.Roles.Count == 0)
                ? new List<string> { RoleCode.User }
                : request.Roles;

            var roles = await ResolveRolesAsync(requested, cancellationToken);

            var user = new User
            {
                UserId = userId,
                UserPassword = _passwordHasher.Hash(request.Password),
                FullName = request.FullName?.Trim(),
                IsActive = true
            };

            await _userRepository.AddAsync(user, cancellationToken);

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await AssignRolesAsync(user.Id, roles, cancellationToken);

                await _auditLogger.LogAsync(
                    nameof(User), user.Id.ToString(), AuditAction.Create,
                    after: new { user.UserId, user.FullName, Roles = roles.Select(r => r.Code) },
                    description: $"Tạo tài khoản '{userId}' với vai trò {string.Join(", ", roles.Select(r => r.Code))}",
                    cancellationToken: cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DataPersistenceException)
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new BusinessRuleException($"Tên đăng nhập '{userId}' đã tồn tại, hãy chọn tên khác");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return ApiResponse.Ok(
                $"Đã tạo tài khoản '{userId}' với vai trò {string.Join(", ", roles.Select(r => r.Name))}. " +
                "Hãy báo mật khẩu cho nhân viên và nhắc họ tự đổi ngay sau khi đăng nhập lần đầu.");
        }

        public async Task<ApiResponse> UpdateUserAsync(
            int id, UserUpdateRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdWithRolesAsync(id, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy tài khoản");

            var isSelf = _currentUser.Id == user.Id;
            var currentRoles = RoleCodesOf(user);

            List<Role>? newRoles = null;
            if (request.Roles != null)
            {
                if (request.Roles.Count == 0)
                {
                    throw new BusinessRuleException(
                        "Tài khoản phải có ít nhất một vai trò. Muốn chặn truy cập thì hãy khoá tài khoản.");
                }

                newRoles = await ResolveRolesAsync(request.Roles, cancellationToken);
            }

            var newIsActive = request.IsActive ?? user.IsActive;
            var newRoleCodes = newRoles?.Select(r => r.Code).ToList() ?? currentRoles.ToList();

            var hadAdmin = currentRoles.Contains(RoleCode.Admin);
            var hasAdmin = newRoleCodes.Contains(RoleCode.Admin);
            var losesAdmin = hadAdmin && (!hasAdmin || !newIsActive);

            if (isSelf && losesAdmin)
            {
                throw new BusinessRuleException(
                    "Không thể tự khoá hoặc tự hạ quyền tài khoản đang đăng nhập. " +
                    "Hãy nhờ một quản trị viên khác thực hiện.");
            }

            if (losesAdmin && await _userRepository.CountActiveAdminsAsync(cancellationToken) <= 1)
            {
                throw new BusinessRuleException(
                    "Đây là quản trị viên hoạt động cuối cùng. Hãy cấp quyền Admin cho " +
                    "một tài khoản khác trước khi khoá hoặc hạ quyền tài khoản này.");
            }

            var before = new { user.FullName, Roles = currentRoles, user.IsActive };

            if (request.FullName != null)
            {
                user.FullName = request.FullName.Trim();
            }

            var wasActive = user.IsActive;
            user.IsActive = newIsActive;

            var phaiThuHoiPhien = wasActive && !newIsActive;

            var canGiaoDich = losesAdmin || phaiThuHoiPhien;

            if (!canGiaoDich)
            {
                if (newRoles != null)
                {
                    await ReplaceRolesAsync(user, newRoles, cancellationToken);
                }

                await LogUserUpdateAsync(
                    user, before, currentRoles, newRoleCodes, wasActive, newIsActive, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return ApiResponse.Ok("Cập nhật tài khoản thành công");
            }

            await using var tx = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                if (newRoles != null)
                {
                    await ReplaceRolesAsync(user, newRoles, cancellationToken);
                }

                await LogUserUpdateAsync(
                    user, before, currentRoles, newRoleCodes, wasActive, newIsActive, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (phaiThuHoiPhien)
                {
                    await _refreshTokenRepository.RevokeAllForUserAsync(
                        user.Id, RevokeReason.AdminLock, DateTime.UtcNow, cancellationToken);
                }

                if (losesAdmin && await _userRepository.CountActiveAdminsAsync(cancellationToken) == 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw new BusinessRuleException(
                        "Một quản trị viên khác vừa bị hạ quyền hoặc bị khoá ngay lúc này, " +
                        "nên đây đã trở thành quản trị viên cuối cùng. Thay đổi đã được huỷ bỏ. " +
                        "Hãy tải lại trang để xem tình trạng mới nhất.");
                }

                await tx.CommitAsync(cancellationToken);
            }
            catch (BusinessRuleException)
            {
                throw; 
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            return ApiResponse.Ok("Cập nhật tài khoản thành công");
        }

        public async Task<ApiResponse> ResetPasswordAsync(
            int id, AdminResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy tài khoản");

            user.UserPassword = _passwordHasher.Hash(request.NewPassword);

            await _auditLogger.LogAsync(
                nameof(User), user.Id.ToString(), AuditAction.PasswordReset,
                description: $"Đặt lại mật khẩu cho tài khoản '{user.UserId}' và thu hồi toàn bộ phiên đăng nhập",
                cancellationToken: cancellationToken);

            await using var tx = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _refreshTokenRepository.RevokeAllForUserAsync(
                    user.Id, RevokeReason.PasswordChanged, DateTime.UtcNow, cancellationToken);

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            return ApiResponse.Ok(
                $"Đã đặt lại mật khẩu cho tài khoản '{user.UserId}'. " +
                "Hãy báo nhân viên đăng nhập và đổi lại mật khẩu ngay.");
        }

        public async Task<ApiResponse> ChangeOwnPasswordAsync(
            ChangePasswordRequest request, CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUser.Id
                ?? throw new BusinessRuleException("Không xác định được tài khoản đang đăng nhập");

            var user = await _userRepository.GetByIdAsync(currentUserId, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy tài khoản");

            if (!_passwordHasher.Verify(request.CurrentPassword, user.UserPassword))
            {
                throw new BusinessRuleException("Mật khẩu hiện tại không đúng");
            }

            if (request.CurrentPassword == request.NewPassword)
            {
                throw new BusinessRuleException("Mật khẩu mới phải khác mật khẩu hiện tại");
            }

            user.UserPassword = _passwordHasher.Hash(request.NewPassword);

            await _auditLogger.LogAsync(
                nameof(User), user.Id.ToString(), AuditAction.PasswordChange,
                description: "Người dùng tự đổi mật khẩu, đã thu hồi toàn bộ phiên đăng nhập",
                cancellationToken: cancellationToken);

            await using var tx = await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _refreshTokenRepository.RevokeAllForUserAsync(
                    user.Id, RevokeReason.PasswordChanged, DateTime.UtcNow, cancellationToken);

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            return ApiResponse.Ok("Đổi mật khẩu thành công, vui lòng đăng nhập lại");
        }


        private async Task<List<Role>> ResolveRolesAsync(
            IEnumerable<string> codes, CancellationToken cancellationToken)
        {
            var wanted = codes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (wanted.Count == 0)
            {
                throw new BusinessRuleException("Phải chọn ít nhất một vai trò");
            }

            var all = await _roleRepository.GetAllAsync(cancellationToken);

            var matched = new List<Role>();
            foreach (var code in wanted)
            {
                var role = all.FirstOrDefault(
                    r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase));

                if (role == null)
                {
                    throw new BusinessRuleException(
                        $"Vai trò '{code}' không tồn tại. Các vai trò hiện có: " +
                        string.Join(", ", all.Select(r => r.Code)));
                }

                matched.Add(role);
            }

            return matched;
        }

        private async Task AssignRolesAsync(
            int userId, IEnumerable<Role> roles, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            foreach (var role in roles)
            {
                await _userRepository.AddUserRoleAsync(new UserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    AssignedAt = now,
                    AssignedBy = _currentUser.Id
                }, cancellationToken);
            }
        }

        private async Task ReplaceRolesAsync(
            User user, List<Role> newRoles, CancellationToken cancellationToken)
        {
            var existing = user.UserRoles?.ToList() ?? new List<UserRole>();
            var keepRoleIds = newRoles.Select(r => r.Id).ToHashSet();

            var toRemove = existing.Where(ur => !keepRoleIds.Contains(ur.RoleId)).ToList();
            if (toRemove.Count > 0)
            {
                _userRepository.RemoveUserRoles(toRemove);
            }

            var currentRoleIds = existing.Select(ur => ur.RoleId).ToHashSet();
            var toAdd = newRoles.Where(r => !currentRoleIds.Contains(r.Id)).ToList();

            await AssignRolesAsync(user.Id, toAdd, cancellationToken);
        }

        private async Task LogUserUpdateAsync(
            User user,
            object before,
            IReadOnlyList<string> oldRoleCodes,
            IReadOnlyList<string> newRoleCodes,
            bool wasActive,
            bool nowActive,
            CancellationToken cancellationToken)
        {
            var after = new { user.FullName, Roles = newRoleCodes, IsActive = nowActive };
            var entityId = user.Id.ToString();

            await _auditLogger.LogAsync(
                nameof(User), entityId, AuditAction.Update, before, after,
                $"Cập nhật tài khoản '{user.UserId}'", cancellationToken: cancellationToken);

            var granted = newRoleCodes.Except(oldRoleCodes, StringComparer.Ordinal).ToList();
            if (granted.Count > 0)
            {
                await _auditLogger.LogAsync(
                    nameof(UserRole), entityId, AuditAction.RoleGrant,
                    after: new { Roles = granted },
                    description: $"Cấp vai trò {string.Join(", ", granted)} cho '{user.UserId}'",
                    cancellationToken: cancellationToken);
            }

            var revoked = oldRoleCodes.Except(newRoleCodes, StringComparer.Ordinal).ToList();
            if (revoked.Count > 0)
            {
                await _auditLogger.LogAsync(
                    nameof(UserRole), entityId, AuditAction.RoleRevoke,
                    before: new { Roles = revoked },
                    description: $"Gỡ vai trò {string.Join(", ", revoked)} khỏi '{user.UserId}'",
                    cancellationToken: cancellationToken);
            }

            if (wasActive != nowActive)
            {
                await _auditLogger.LogAsync(
                    nameof(User), entityId,
                    nowActive ? AuditAction.Unlock : AuditAction.Lock,
                    description: nowActive
                        ? $"Mở khoá tài khoản '{user.UserId}'"
                        : $"Khoá tài khoản '{user.UserId}' và thu hồi toàn bộ phiên đăng nhập",
                    cancellationToken: cancellationToken);
            }
        }

        private static IReadOnlyList<string> RoleCodesOf(User u)
            => u.UserRoles?
                 .Where(ur => ur.Role != null)
                 .Select(ur => ur.Role!.Code)
                 .Distinct(StringComparer.Ordinal)
                 .OrderBy(c => c, StringComparer.Ordinal)
                 .ToList()
               ?? (IReadOnlyList<string>)Array.Empty<string>();

        private static UserDto ToDto(User u) => new()
        {
            Id = u.Id,
            UserId = u.UserId,
            FullName = u.FullName ?? "",
            Roles = RoleCodesOf(u),
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt
        };
    }
}
