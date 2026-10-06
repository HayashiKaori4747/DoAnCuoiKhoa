using TonKhoApp.Application.Interfaces;

namespace TonKhoApp.WebApi.Services
{
    public static class DatabaseWakeup
    {
        private static int Read(IConfiguration config, string envKey, string jsonKey)
        {
            var fromEnv = config.GetValue(envKey, 0);
            return fromEnv > 0 ? fromEnv : config.GetValue(jsonKey, 0);
        }

        public static async Task WarmUpAsync(WebApplication app)
        {
            var seconds = Read(app.Configuration, "DBWARMUPSECONDS", "Database:WarmupSeconds");
            if (seconds <= 0)
            {
                return;
            }

            var logger = app.Services.GetRequiredService<ILoggerFactory>()
                                     .CreateLogger(nameof(DatabaseWakeup));
            var startedAt = DateTime.UtcNow;
            var deadline = startedAt.AddSeconds(seconds);
            var attempt = 0;

            while (true)
            {
                attempt++;
                try
                {
                    using var scope = app.Services.CreateScope();
                    var probe = scope.ServiceProvider.GetRequiredService<IDatabaseProbe>();

                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await probe.PingAsync(timeout.Token);

                    logger.LogInformation(
                        "Database đã sẵn sàng sau {Attempt} lần thử ({Elapsed:n0} giây).",
                        attempt, (DateTime.UtcNow - startedAt).TotalSeconds);
                    return;
                }
                catch (Exception ex) when (DateTime.UtcNow < deadline)
                {
                    logger.LogWarning(
                        "Database chưa trả lời (lần {Attempt}): {Message}. Thử lại sau 5 giây...",
                        attempt, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Đã chờ {Seconds} giây mà database vẫn không kết nối được. " +
                        "App vẫn khởi động; hãy kiểm tra TONKHO_CONNECTIONSTRING và tường lửa của SQL Server.",
                        seconds);
                    return;
                }
            }
        }

        public static IServiceCollection AddDatabaseKeepAlive(
            this IServiceCollection services, IConfiguration configuration)
        {
            if (Read(configuration, "DBKEEPALIVEMINUTES", "Database:KeepAliveMinutes") > 0)
            {
                services.AddHostedService<DatabaseKeepAliveService>();
            }
            return services;
        }
    }

    internal sealed class DatabaseKeepAliveService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DatabaseKeepAliveService> _logger;
        private readonly TimeSpan _interval;

        public DatabaseKeepAliveService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<DatabaseKeepAliveService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var minutes = configuration.GetValue("DBKEEPALIVEMINUTES", 0);
            if (minutes <= 0)
            {
                minutes = configuration.GetValue("Database:KeepAliveMinutes", 10);
            }
            _interval = TimeSpan.FromMinutes(minutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Giữ kết nối database mỗi {Minutes} phút để nó không tự ngủ.",
                _interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_interval, stoppingToken);

                    using var scope = _scopeFactory.CreateScope();
                    var probe = scope.ServiceProvider.GetRequiredService<IDatabaseProbe>();
                    await probe.PingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ping giữ kết nối database thất bại, sẽ thử lại ở chu kỳ sau.");
                }
            }
        }
    }
}
