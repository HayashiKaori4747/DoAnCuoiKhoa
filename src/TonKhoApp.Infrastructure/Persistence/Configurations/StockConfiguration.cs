using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Persistence.Configurations
{
    public class StockConfiguration : IEntityTypeConfiguration<Stock>
    {
        public void Configure(EntityTypeBuilder<Stock> builder)
        {
            builder.ToTable("Stock", t =>
            {
                t.HasCheckConstraint("CK_Stock_Quantity", "[Quantity] >= 0");
            });

            builder.HasKey(s => s.Id);

            builder.Property(s => s.ProductId).IsRequired();

            builder.Property(s => s.Quantity).HasColumnType("decimal(18,3)");

            builder.Property(s => s.RowVersion).IsRowVersion();

            builder.Property(s => s.CreatedAt).HasColumnType("datetime2(3)");
            builder.Property(s => s.UpdatedAt).HasColumnType("datetime2(3)");

            builder.HasIndex(s => s.ProductId).IsUnique().HasDatabaseName("UX_Stock_ProductId");

            builder.HasOne(s => s.Product)
                   .WithOne(p => p.Stock)
                   .HasForeignKey<Stock>(s => s.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<User>().WithMany().HasForeignKey(s => s.CreatedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Stock_Users_CreatedBy");
            builder.HasOne<User>().WithMany().HasForeignKey(s => s.UpdatedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Stock_Users_UpdatedBy");

            builder.HasIndex(s => s.CreatedBy).HasDatabaseName("IX_Stock_CreatedBy");
            builder.HasIndex(s => s.UpdatedBy).HasDatabaseName("IX_Stock_UpdatedBy");
        }
    }
}
