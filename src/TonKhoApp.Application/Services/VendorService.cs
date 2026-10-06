using TonKhoApp.Application.Common;
using TonKhoApp.Application.DTOs;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Domain.Constants;
using TonKhoApp.Domain.Entities;
using TonKhoApp.Domain.Exceptions;

namespace TonKhoApp.Application.Services
{
    public class VendorService : IVendorService
    {
        private readonly IVendorRepository _vendorRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IAuditLogger _auditLogger;
        private readonly IUnitOfWork _unitOfWork;

        public VendorService(
            IVendorRepository vendorRepository,
            IOrderRepository orderRepository,
            IAuditLogger auditLogger,
            IUnitOfWork unitOfWork)
        {
            _vendorRepository = vendorRepository;
            _orderRepository = orderRepository;
            _auditLogger = auditLogger;
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResponse<VendorDto>> GetAllVendorsAsync(
            PaginationRequest pagination, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _vendorRepository.GetPagedAsync(
                pagination.Page, pagination.PageSize, pagination.Search,
                pagination.ActiveOnly ?? false, cancellationToken);

            return new PagedResponse<VendorDto>
            {
                Success = true,
                Data = items.Select(ToDto).ToList(),
                TotalCount = totalCount,
                Page = pagination.Page,
                PageSize = pagination.PageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize)
            };
        }

        public async Task<SingleResponse<VendorDto>> GetVendorDetailAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var vendor = await _vendorRepository.GetByIdAsync(id, cancellationToken);

            if (vendor == null)
            {
                throw new NotFoundException("Không tìm thấy nhà cung cấp");
            }

            return new SingleResponse<VendorDto> { Success = true, Data = ToDto(vendor) };
        }

        public async Task<ApiResponse> CreateVendorAsync(
            VendorRequest request, CancellationToken cancellationToken = default)
        {
            var vendorName = request.VendorName?.Trim() ?? "";
            if (string.IsNullOrEmpty(vendorName))
            {
                throw new BusinessRuleException("Tên NCC không được để trống");
            }

            if (await _vendorRepository.ExistsByVendorNameAsync(vendorName, cancellationToken))
            {
                throw new BusinessRuleException("Tên NCC đã tồn tại");
            }

            var vendorCode = request.VendorCode?.Trim();
            if (string.IsNullOrEmpty(vendorCode))
            {
                vendorCode = await _vendorRepository.GenerateNextVendorCodeAsync(cancellationToken);
            }
            else if (await _vendorRepository.ExistsByVendorCodeAsync(vendorCode, cancellationToken))
            {
                throw new BusinessRuleException("Mã NCC đã tồn tại");
            }

            var vendor = new Vendor
            {
                VendorCode = vendorCode,
                VendorName = vendorName,
                Address = request.Address?.Trim(),
                Phone = request.Phone?.Trim(),
                Email = request.Email?.Trim(),
                TaxCode = request.TaxCode?.Trim(),
                IsActive = request.IsActive ?? true
            };

            await _vendorRepository.AddAsync(vendor, cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DataPersistenceException)
            {
                throw new BusinessRuleException("Mã hoặc tên NCC đã tồn tại");
            }

            return ApiResponse.Ok("Tạo nhà cung cấp thành công", new { vendor.Id, vendor.VendorCode });
        }

        public async Task<ApiResponse> UpdateVendorAsync(
            int id, VendorRequest request, CancellationToken cancellationToken = default)
        {
            var vendor = await _vendorRepository.GetByIdAsync(id, cancellationToken);

            if (vendor == null)
            {
                throw new NotFoundException("Không tìm thấy nhà cung cấp");
            }

            var vendorName = request.VendorName?.Trim() ?? "";
            if (string.IsNullOrEmpty(vendorName))
            {
                throw new BusinessRuleException("Tên NCC không được để trống");
            }

            if (!string.Equals(vendorName, vendor.VendorName, StringComparison.OrdinalIgnoreCase) &&
                await _vendorRepository.ExistsByVendorNameAsync(vendorName, cancellationToken))
            {
                throw new BusinessRuleException("Tên NCC đã tồn tại");
            }

            vendor.VendorName = vendorName;
            vendor.Address = request.Address?.Trim();
            vendor.Phone = request.Phone?.Trim();
            vendor.Email = request.Email?.Trim();
            vendor.TaxCode = request.TaxCode?.Trim();
            vendor.IsActive = request.IsActive ?? vendor.IsActive;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok("Cập nhật nhà cung cấp thành công");
        }

        public async Task<ApiResponse> DeleteVendorAsync(
            int id, CancellationToken cancellationToken = default)
        {
            var vendor = await _vendorRepository.GetByIdAsync(id, cancellationToken);
            if (vendor == null)
            {
                throw new NotFoundException("Không tìm thấy nhà cung cấp");
            }

            if (await _orderRepository.ExistsByVendorIdAsync(id, cancellationToken))
            {
                if (!vendor.IsActive)
                {
                    throw new BusinessRuleException("Nhà cung cấp này đã ngừng hoạt động");
                }

                vendor.IsActive = false;

                await _auditLogger.LogAsync(
                    nameof(Vendor), vendor.VendorCode, AuditAction.Update,
                    before: new { IsActive = true }, after: new { IsActive = false },
                    description: $"Ngừng hợp tác với NCC {vendor.VendorCode} (còn đơn hàng nên không xoá)",
                    cancellationToken: cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return ApiResponse.Ok("NCC đang có đơn hàng nên đã được chuyển sang trạng thái Ngừng hoạt động");
            }

            _vendorRepository.Remove(vendor);

            await _auditLogger.LogAsync(
                nameof(Vendor), vendor.VendorCode, AuditAction.Delete,
                before: new { vendor.VendorCode, vendor.VendorName },
                description: $"Xoá nhà cung cấp {vendor.VendorCode}",
                cancellationToken: cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse.Ok("Xoá nhà cung cấp thành công");
        }

        private static VendorDto ToDto(Vendor v) => new()
        {
            Id = v.Id,
            VendorCode = v.VendorCode,
            VendorName = v.VendorName,
            Address = v.Address ?? "",
            Phone = v.Phone ?? "",
            Email = v.Email ?? "",
            TaxCode = v.TaxCode ?? "",
            IsActive = v.IsActive
        };
    }
}
