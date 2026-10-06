using System.Globalization;
using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Constants;
using TonKhoApp.Domain.Exceptions;

namespace TonKhoApp.Application.Services
{
    public interface IAuditLogService
    {
        Task<PagedResponse<AuditLogDto>> GetLogsAsync(
            AuditLogFilter filter, CancellationToken cancellationToken = default);
    }

    public class AuditLogService : IAuditLogService
    {
        private const int MaxPageSize = 100;

        private readonly IAuditLogRepository _auditLogRepository;

        public AuditLogService(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        public async Task<PagedResponse<AuditLogDto>> GetLogsAsync(
            AuditLogFilter filter, CancellationToken cancellationToken = default)
        {
            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

            if (!string.IsNullOrWhiteSpace(filter.Action) && !AuditAction.All.Contains(filter.Action))
            {
                throw new BusinessRuleException(
                    "Loại hành động không hợp lệ. Chỉ nhận: " + string.Join(", ", AuditAction.All));
            }

            var fromUtc = ParseDate(filter.FromDate, "Từ ngày");

            var toRaw = ParseDate(filter.ToDate, "Đến ngày");
            var toUtc = toRaw?.AddDays(1);

            if (fromUtc.HasValue && toUtc.HasValue && fromUtc >= toUtc)
            {
                throw new BusinessRuleException("Khoảng thời gian không hợp lệ: từ ngày phải trước đến ngày");
            }

            var (items, totalCount) = await _auditLogRepository.GetPagedAsync(
                page, pageSize,
                filter.EntityName, filter.EntityId, filter.UserId, filter.Action,
                fromUtc, toUtc, cancellationToken);

            return new PagedResponse<AuditLogDto>
            {
                Success = true,
                Data = items.Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    EntityName = a.EntityName,
                    EntityId = a.EntityId,
                    Action = a.Action,
                    ActionText = GetActionText(a.Action),
                    BeforeJson = a.BeforeJson,
                    AfterJson = a.AfterJson,
                    UserId = a.UserId,

                    UserName = a.User?.FullName ?? a.UserName ?? "(hệ thống)",

                    IpAddress = a.IpAddress,
                    Description = a.Description,

                    CreatedAt = DateTime.SpecifyKind(a.CreatedAt, DateTimeKind.Utc)
                        .ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                }).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }

        private static DateTime? ParseDate(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (!DateTime.TryParseExact(
                    value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                throw new BusinessRuleException($"{fieldName} không hợp lệ (định dạng yyyy-MM-dd)");
            }

            return parsed;
        }

        private static string GetActionText(string action) => action switch
        {
            AuditAction.Create => "Thêm mới",
            AuditAction.Update => "Cập nhật",
            AuditAction.Delete => "Xoá",
            AuditAction.PriceChange => "Sửa giá",
            AuditAction.Lock => "Khoá tài khoản",
            AuditAction.Unlock => "Mở khoá tài khoản",
            AuditAction.RoleGrant => "Cấp vai trò",
            AuditAction.RoleRevoke => "Gỡ vai trò",
            AuditAction.PasswordReset => "Đặt lại mật khẩu",
            AuditAction.PasswordChange => "Đổi mật khẩu",
            AuditAction.Login => "Đăng nhập",
            AuditAction.LoginFailed => "Đăng nhập thất bại",
            AuditAction.Logout => "Đăng xuất",
            AuditAction.OrderConfirm => "Xác nhận đơn",
            AuditAction.OrderReceive => "Nhập kho theo đơn",
            AuditAction.OrderCancel => "Huỷ đơn",
            AuditAction.StockAdjust => "Điều chỉnh tồn",
            AuditAction.StockIssue => "Xuất kho",
            AuditAction.StockReversal => "Bút toán đảo",
            _ => action
        };
    }
}
