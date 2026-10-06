using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Constants;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Application.Services
{
    public class AuthService : IAuthService
    {
        private const string DummyHash =
            "$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";

        private const string InvalidCredentials = "User ID hoặc Password không đúng";

        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogger _auditLogger;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IJwtService jwtService,
            IPasswordHasher passwordHasher,
            IAuditLogger auditLogger,
            ICurrentUser currentUser,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _auditLogger = auditLogger;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
        }

        public async Task<LoginResult> LoginAsync(
            LoginRequest request, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByUserIdAsync(request.UserId, cancellationToken);

            if (user == null)
            {
                try
                {
                    _passwordHasher.Verify(request.UserPassword, DummyHash);
                }
                catch
                {
                }

                await LogFailedLoginAsync(request.UserId, "tên đăng nhập không tồn tại", cancellationToken);
                return Fail(InvalidCredentials);
            }

            if (!_passwordHasher.Verify(request.UserPassword, user.UserPassword))
            {
                await LogFailedLoginAsync(request.UserId, "sai mật khẩu", cancellationToken, user.Id);
                return Fail(InvalidCredentials);
            }

            if (!user.IsActive)
            {
                await LogFailedLoginAsync(request.UserId, "tài khoản đang bị khoá", cancellationToken, user.Id);
                return Fail("Tài khoản đã bị khoá, vui lòng liên hệ quản trị viên");
            }

            var roleCodes = ExtractRoleCodes(user);

            if (roleCodes.Count == 0)
            {
                await LogFailedLoginAsync(request.UserId, "tài khoản chưa được cấp vai trò", cancellationToken, user.Id);
                return Fail("Tài khoản chưa được cấp quyền, vui lòng liên hệ quản trị viên");
            }

            user.LastLoginAt = DateTime.UtcNow;

            var result = await IssueTokensAsync(user, roleCodes, cancellationToken);

            await _auditLogger.LogAsync(
                nameof(User), user.Id.ToString(), AuditAction.Login,
                description: $"Đăng nhập thành công ({string.Join(", ", roleCodes)})",
                actorId: user.Id, actorName: user.UserId,
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new LoginResult { Success = true, Data = result };
        }

        public async Task<LoginResult> RefreshAsync(
            string refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Fail("Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại");
            }

            var now = DateTime.UtcNow;
            var hash = _jwtService.HashRefreshToken(refreshToken);
            var stored = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

            if (stored == null)
            {
                return Fail("Phiên đăng nhập không hợp lệ, vui lòng đăng nhập lại");
            }

            if (stored.RevokedAt != null)
            {
                await _refreshTokenRepository.RevokeAllForUserAsync(
                    stored.UserId, RevokeReason.ReuseDetected, now, cancellationToken);

                await _auditLogger.LogAsync(
                    nameof(RefreshToken), stored.Id.ToString(), AuditAction.LoginFailed,
                    description: "Phát hiện refresh token đã thu hồi bị dùng lại - " +
                                 "đã thu hồi toàn bộ phiên của tài khoản",
                    cancellationToken: cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Fail("Phiên đăng nhập không hợp lệ, vui lòng đăng nhập lại");
            }

            if (stored.ExpiresAt <= now)
            {
                return Fail("Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại");
            }

            var user = stored.User;
            if (user == null || user.Deleted || !user.IsActive)
            {
                await _refreshTokenRepository.RevokeAllForUserAsync(
                    stored.UserId, RevokeReason.AdminLock, now, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Fail("Tài khoản không còn hiệu lực, vui lòng liên hệ quản trị viên");
            }

            var roleCodes = await _userRepository.GetRoleCodesAsync(user.Id, cancellationToken);
            if (roleCodes.Count == 0)
            {
                return Fail("Tài khoản chưa được cấp quyền, vui lòng liên hệ quản trị viên");
            }

            stored.RevokedAt = now;
            stored.RevokedReason = RevokeReason.Rotated;

            var result = await IssueTokensAsync(user, roleCodes, cancellationToken, replaced: stored);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new LoginResult { Success = true, Data = result };
        }

        public async Task LogoutAsync(
            string? refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            var hash = _jwtService.HashRefreshToken(refreshToken);
            var stored = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

            if (stored == null || stored.RevokedAt != null)
            {
                return;
            }

            stored.RevokedAt = DateTime.UtcNow;
            stored.RevokedReason = RevokeReason.Logout;

            await _auditLogger.LogAsync(
                nameof(User), stored.UserId.ToString(), AuditAction.Logout,
                description: "Đăng xuất",
                actorId: stored.UserId, actorName: stored.User?.UserId,
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<ApiResponse> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.GetAllActiveAsync(cancellationToken);

            var data = users.Select(u => new UserListItemDto
            {
                UserId = u.UserId,
                FullName = u.FullName ?? u.UserId,
                Roles = ExtractRoleCodes(u)
            }).ToList();

            return ApiResponse.Ok(data: data);
        }


        private async Task<LoginResponse> IssueTokensAsync(
            User user,
            IReadOnlyList<string> roleCodes,
            CancellationToken cancellationToken,
            RefreshToken? replaced = null)
        {
            var now = DateTime.UtcNow;

            var accessToken = _jwtService.GenerateAccessToken(user, roleCodes);
            var (rawToken, tokenHash) = _jwtService.GenerateRefreshToken();

            var refreshExpires = now.AddDays(_jwtService.RefreshTokenDays);

            var entity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = refreshExpires,
                CreatedByIp = _currentUser.IpAddress
            };

            await _refreshTokenRepository.AddAsync(entity, cancellationToken);

            if (replaced != null)
            {
                replaced.ReplacedByToken = entity;
            }

            return new LoginResponse
            {
                AccessToken = accessToken,
                AccessTokenExpiration = now.AddMinutes(_jwtService.AccessTokenMinutes),
                RefreshToken = rawToken,
                RefreshTokenExpiration = refreshExpires,
                UserId = user.UserId,
                FullName = user.FullName ?? "",
                Roles = roleCodes
            };
        }

        private async Task LogFailedLoginAsync(
            string attemptedUserId, string reason, CancellationToken cancellationToken,
            int? targetUserId = null)
        {
            await _auditLogger.LogAsync(
                nameof(User), targetUserId?.ToString(), AuditAction.LoginFailed,
                description: $"Đăng nhập thất bại cho '{attemptedUserId}': {reason}",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static IReadOnlyList<string> ExtractRoleCodes(User user)
            => user.UserRoles?
                   .Where(ur => ur.Role != null)
                   .Select(ur => ur.Role!.Code)
                   .Distinct(StringComparer.Ordinal)
                   .ToList()
               ?? (IReadOnlyList<string>)Array.Empty<string>();

        private static LoginResult Fail(string message)
            => new() { Success = false, ErrorMessage = message };
    }
}
