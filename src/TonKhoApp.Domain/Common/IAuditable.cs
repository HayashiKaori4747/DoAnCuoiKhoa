namespace TonKhoApp.Domain.Common
{
    public interface IAuditable
    {
        DateTime CreatedAt { get; set; }
        int? CreatedBy { get; set; }   
        DateTime? UpdatedAt { get; set; }
        int? UpdatedBy { get; set; }
    }
}
