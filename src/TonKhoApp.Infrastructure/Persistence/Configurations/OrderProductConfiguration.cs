using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Persistence.Configurations
{
    public class OrderProductConfiguration : IEntityTypeConfiguration<OrderProduct>
    {
        public void Configure(EntityTypeBuilder<OrderProduct> builder)
        {
            builder.ToTable("OrderProducts", t =>
            {
                t.HasCheckConstraint("CK_OrderProducts_Quantity", "[Quantity] > 0");
                t.HasCheckConstraint("CK_OrderProducts_UnitPrice", "[UnitPrice] >= 0");
            });

            builder.HasKey(op => op.Id);

            builder.Property(op => op.OrderId).IsRequired();
            builder.Property(op => op.ProductId).IsRequired();

            builder.Property(op => op.ItemCode).IsRequired().HasMaxLength(50);
            builder.Property(op => op.ItemName).IsRequired().HasMaxLength(255);
            builder.Property(op => op.Unit).IsRequired().HasMaxLength(50);

            builder.Property(op => op.Quantity).HasColumnType("decimal(18,3)");
            builder.Property(op => op.UnitPrice).HasColumnType("decimal(18,2)");

            builder.Property(op => op.LineAmount)
                   .HasColumnType("decimal(18,2)")
                   .HasComputedColumnSql("CONVERT(decimal(18,2), [Quantity] * [UnitPrice])", stored: true)
                   .ValueGeneratedOnAddOrUpdate();

            builder.HasIndex(op => new { op.OrderId, op.ProductId })
                   .IsUnique()
                   .HasDatabaseName("UX_OrderProducts_Order_Product");

            builder.HasIndex(op => op.ProductId).HasDatabaseName("IX_OrderProducts_ProductId");

            builder.HasOne(op => op.Order)
                   .WithMany(o => o.OrderProducts)
                   .HasForeignKey(op => op.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(op => op.Product)
                   .WithMany(p => p.OrderProducts)
                   .HasForeignKey(op => op.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
