namespace TonKhoApp.Domain.Common
{
    public interface ISoftDeletable
    {
        bool Deleted { get; set; }
        DateTime? DeletedAt { get; set; }
        int? DeletedBy { get; set; }
    }
}
