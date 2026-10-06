using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Constants;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Domain.Exceptions;

namespace TonKhoApp.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IAuditLogger _auditLogger;
        private readonly IUnitOfWork _unitOfWork;

        public RoleService(
            IRoleRepository roleRepository,
            IAuditLogger auditLogger,
            IUnitOfWork unitOfWork)
        {
            _roleRepository = roleRepository;
            _auditLogger = auditLogger;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            var roles = await _roleRepository.GetAllAsync(cancellationToken);

            var data = new List<RoleDto>(roles.Count);
            foreach (var r in roles)
            {
                data.Add(new RoleDto
                {
                    Id = r.Id,
                    Code = r.Code,
                    Name = r.Name,
                    Description = r.Description ?? "",
                    IsSystem = r.IsSystem,
                    UserCount = await _roleRepository.CountUsersInRoleAsync(r.Id, cancellationToken)
                });
            }

            return ApiResponse.Ok(data: data);
        }

        public async Task<ApiResponse> CreateRoleAsync(
            RoleRequest request, CancellationToken cancellationToken = default)
        {
            var code = request.Code.Trim();
            var name = request.Name.Trim();

            if (await _roleRepository.ExistsByCodeAsync(code, cancellationToken))
            {
                throw new BusinessRuleException($"Mã vai trò '{code}' đã tồn tại");
            }

            if (await _roleRepository.ExistsByNameAsync(name, cancellationToken))
            {
                throw new BusinessRuleException($"Tên vai trò '{name}' đã tồn tại");
            }

            var role = new Role
            {
                Code = code,
                Name = name,
                Description = request.Description?.Trim(),

                IsSystem = false
            };

            await _roleRepository.AddAsync(role, cancellationToken);

            try
            {
                await _auditLogger.LogAsync(
                    nameof(Role), null, AuditAction.Create,
                    after: new { role.Code, role.Name, role.Description },
                    description: $"Tạo vai trò '{code}'",
                    cancellationToken: cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DataPersistenceException)
            {
                throw new BusinessRuleException($"Mã hoặc tên vai trò đã tồn tại");
            }

            return ApiResponse.Ok($"Đã tạo vai trò '{name}'");
        }

        public async Task<ApiResponse> UpdateRoleAsync(
            int id, RoleRequest request, CancellationToken cancellationToken = default)
        {
            var role = await _roleRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy vai trò");

            var newCode = request.Code.Trim();
            var newName = request.Name.Trim();

            if (role.IsSystem && !string.Equals(role.Code, newCode, StringComparison.Ordinal))
            {
                throw new BusinessRuleException(
                    $"'{role.Code}' là vai trò hệ thống, không đổi được mã (code đang tham chiếu trực tiếp tới mã này). " +
                    "Bạn vẫn đổi được tên hiển thị và mô tả.");
            }

            if (!string.Equals(role.Code, newCode, StringComparison.Ordinal) &&
                await _roleRepository.ExistsByCodeAsync(newCode, cancellationToken))
            {
                throw new BusinessRuleException($"Mã vai trò '{newCode}' đã tồn tại");
            }

            if (!string.Equals(role.Name, newName, StringComparison.OrdinalIgnoreCase) &&
                await _roleRepository.ExistsByNameAsync(newName, cancellationToken))
            {
                throw new BusinessRuleException($"Tên vai trò '{newName}' đã tồn tại");
            }

            var before = new { role.Code, role.Name, role.Description };

            role.Code = newCode;
            role.Name = newName;
            role.Description = request.Description?.Trim();

            await _auditLogger.LogAsync(
                nameof(Role), role.Id.ToString(), AuditAction.Update,
                before, new { role.Code, role.Name, role.Description },
                $"Cập nhật vai trò '{role.Code}'", cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok("Cập nhật vai trò thành công");
        }

        public async Task<ApiResponse> DeleteRoleAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var role = await _roleRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException("Không tìm thấy vai trò");

            if (role.IsSystem)
            {
                throw new BusinessRuleException(
                    $"'{role.Name}' là vai trò hệ thống, không xoá được. " +
                    "Code đang tham chiếu trực tiếp tới mã này trong phân quyền API.");
            }

            var userCount = await _roleRepository.CountUsersInRoleAsync(role.Id, cancellationToken);
            if (userCount > 0)
            {
                throw new BusinessRuleException(
                    $"Còn {userCount} tài khoản đang giữ vai trò '{role.Name}'. " +
                    "Hãy chuyển họ sang vai trò khác trước khi xoá.");
            }

            _roleRepository.Remove(role);

            await _auditLogger.LogAsync(
                nameof(Role), role.Id.ToString(), AuditAction.Delete,
                before: new { role.Code, role.Name, role.Description },
                description: $"Xoá vai trò '{role.Code}'",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok($"Đã xoá vai trò '{role.Name}'");
        }
    }
}
