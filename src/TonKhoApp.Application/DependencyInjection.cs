using Microsoft.Extensions.DependencyInjection;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Application.Services;

namespace TonKhoApp.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IAuditLogger, AuditLogger>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IVendorService, VendorService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IStockService, StockService>();
            services.AddScoped<IDashboardService, DashboardService>();

            return services;
        }
    }
}
