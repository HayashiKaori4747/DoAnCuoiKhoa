using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Persistence.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders", t =>
            {
                t.HasCheckConstraint("CK_Orders_Status",
                    "[Status] IN ('DRAFT','CONFIRMED','RECEIVED','CANCELLED')");

                t.HasCheckConstraint("CK_Orders_Cancel",
                    "([Status] <> 'CANCELLED' AND [CancelledAt] IS NULL AND [CancelReason] IS NULL) OR " +
                    "([Status] =  'CANCELLED' AND [CancelledAt] IS NOT NULL AND " +
                    "[CancelReason] IS NOT NULL AND LEN(LTRIM([CancelReason])) > 0)");

                t.HasCheckConstraint("CK_Orders_Deleted",
                    "([Deleted] = 0 AND [DeletedAt] IS NULL) OR ([Deleted] = 1 AND [DeletedAt] IS NOT NULL)");

                t.HasCheckConstraint("CK_Orders_NoDeleteReceived",
                    "[Deleted] = 0 OR [Status] <> 'RECEIVED'");
            });

            builder.HasKey(o => o.Id);

            builder.Property(o => o.OrderNo).IsRequired().HasMaxLength(50);
            builder.Property(o => o.VendorId).IsRequired();
            builder.Property(o => o.OrderDate).IsRequired();
            builder.Property(o => o.Status).IsRequired().HasColumnType("varchar(20)");
            builder.Property(o => o.Note).HasMaxLength(500);

            builder.Property(o => o.CancelReason).HasMaxLength(500);
            builder.Property(o => o.CancelledAt).HasColumnType("datetime2(3)");

            builder.Property(o => o.Deleted).IsRequired().HasDefaultValue(false);

            builder.Property(o => o.CreatedAt).HasColumnType("datetime2(3)");
            builder.Property(o => o.UpdatedAt).HasColumnType("datetime2(3)");
            builder.Property(o => o.DeletedAt).HasColumnType("datetime2(3)");

            builder.HasIndex(o => o.OrderNo).IsUnique().HasDatabaseName("UX_Orders_OrderNo");

            builder.HasIndex(o => o.VendorId).HasDatabaseName("IX_Orders_VendorId");
            builder.HasIndex(o => o.HandlerUserId).HasDatabaseName("IX_Orders_HandlerUserId");

            builder.HasIndex(o => new { o.Deleted, o.Status }).HasDatabaseName("IX_Orders_Deleted_Status");
            builder.HasIndex(o => new { o.Deleted, o.OrderDate }).HasDatabaseName("IX_Orders_Deleted_OrderDate");

            builder.HasOne(o => o.Vendor)
                   .WithMany(v => v.Orders)
                   .HasForeignKey(o => o.VendorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.HandlerUser)
                   .WithMany()
                   .HasForeignKey(o => o.HandlerUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.CancelledByUser)
                   .WithMany()
                   .HasForeignKey(o => o.CancelledBy)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<User>().WithMany().HasForeignKey(o => o.CreatedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Orders_Users_CreatedBy");
            builder.HasOne<User>().WithMany().HasForeignKey(o => o.UpdatedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Orders_Users_UpdatedBy");
            builder.HasOne<User>().WithMany().HasForeignKey(o => o.DeletedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Orders_Users_DeletedBy");

            builder.HasIndex(o => o.CreatedBy).HasDatabaseName("IX_Orders_CreatedBy");
            builder.HasIndex(o => o.UpdatedBy).HasDatabaseName("IX_Orders_UpdatedBy");
            builder.HasIndex(o => o.DeletedBy).HasDatabaseName("IX_Orders_DeletedBy");
            builder.HasIndex(o => o.CancelledBy).HasDatabaseName("IX_Orders_CancelledBy");
        }
    }
}
