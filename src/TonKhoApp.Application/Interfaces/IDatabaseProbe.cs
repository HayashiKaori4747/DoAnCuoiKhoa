namespace TonKhoApp.Application.Interfaces
{
    public interface IDatabaseProbe
    {
        Task PingAsync(CancellationToken cancellationToken = default);
    }
}
