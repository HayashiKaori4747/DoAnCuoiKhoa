using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Persistence.Configurations
{
    public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
    {
        public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
        {
            builder.ToTable("OrderStatusHistory", t =>
            {
                t.HasCheckConstraint("CK_OrderStatusHistory_From",
                    "[FromStatus] IS NULL OR [FromStatus] IN ('DRAFT','CONFIRMED','RECEIVED','CANCELLED')");

                t.HasCheckConstraint("CK_OrderStatusHistory_To",
                    "[ToStatus] IN ('DRAFT','CONFIRMED','RECEIVED','CANCELLED')");

                t.HasCheckConstraint("CK_OrderStatusHistory_Change",
                    "[FromStatus] IS NULL OR [FromStatus] <> [ToStatus]");

                t.HasCheckConstraint("CK_OrderStatusHistory_CancelReason",
                    "[ToStatus] <> 'CANCELLED' OR ([Reason] IS NOT NULL AND LEN(LTRIM([Reason])) > 0)");
            });

            builder.HasKey(h => h.Id);

            builder.Property(h => h.FromStatus).HasColumnType("varchar(20)");
            builder.Property(h => h.ToStatus).IsRequired().HasColumnType("varchar(20)");
            builder.Property(h => h.Reason).HasMaxLength(500);
            builder.Property(h => h.ChangedAt).HasColumnType("datetime2(3)");

            builder.HasIndex(h => new { h.OrderId, h.ChangedAt })
                   .HasDatabaseName("IX_OrderStatusHistory_Order_Date");

            builder.HasOne(h => h.Order)
                   .WithMany(o => o.StatusHistory)
                   .HasForeignKey(h => h.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(h => h.ChangedByUser)
                   .WithMany()
                   .HasForeignKey(h => h.ChangedBy)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
