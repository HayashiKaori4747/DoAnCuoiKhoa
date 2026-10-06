using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TonKhoApp.Application.Interfaces;
using TonKhoApp.Infrastructure.Persistence;
using TonKhoApp.Infrastructure.Repositories;
using TonKhoApp.Infrastructure.Security;

namespace TonKhoApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString =
                configuration["TONKHO_CONNECTIONSTRING"]
                ?? configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình chuỗi kết nối database. Đặt biến môi trường TONKHO_CONNECTIONSTRING " +
                    "hoặc chạy: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<chuỗi kết nối>\"");
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IVendorRepository, VendorRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IStockRepository, StockRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IOrderStatusHistoryRepository, OrderStatusHistoryRepository>();

            services.AddScoped<ICurrentUser, AnonymousCurrentUser>();

            services.AddScoped<IDatabaseProbe, SqlDatabaseProbe>();

            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

            return services;
        }
    }
}
