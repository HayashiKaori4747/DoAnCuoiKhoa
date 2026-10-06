namespace TonKhoApp.Application.Common
{
    public class SingleResponse<T>
    {
        public bool Success { get; set; } = true;
        public T? Data { get; set; }
    }
}
