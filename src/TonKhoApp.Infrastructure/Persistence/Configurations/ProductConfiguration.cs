using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Persistence.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products", t =>
            {
                t.HasCheckConstraint("CK_Products_Price", "[Price] >= 0");
                t.HasCheckConstraint("CK_Products_MinQuantity", "[MinQuantity] >= 0");
                t.HasCheckConstraint("CK_Products_Deleted",
                    "([Deleted] = 0 AND [DeletedAt] IS NULL) OR ([Deleted] = 1 AND [DeletedAt] IS NOT NULL)");
            });

            builder.HasKey(p => p.Id);

            builder.Property(p => p.ItemCode).IsRequired().HasMaxLength(50);
            builder.Property(p => p.ItemName).IsRequired().HasMaxLength(255);
            builder.Property(p => p.Unit).IsRequired().HasMaxLength(50).HasDefaultValue("kg");

            builder.Property(p => p.Price).HasColumnType("decimal(18,2)");

            builder.Property(p => p.MinQuantity).HasColumnType("decimal(18,3)").HasDefaultValue(0m);

            builder.Property(p => p.IsActive).HasDefaultValue(true);
            builder.Property(p => p.Deleted).IsRequired().HasDefaultValue(false);

            builder.Property(p => p.CreatedAt).HasColumnType("datetime2(3)");
            builder.Property(p => p.UpdatedAt).HasColumnType("datetime2(3)");
            builder.Property(p => p.DeletedAt).HasColumnType("datetime2(3)");

            builder.HasIndex(p => p.ItemCode)
                   .IsUnique()
                   .HasFilter("[Deleted] = 0")
                   .HasDatabaseName("UX_Products_ItemCode");

            builder.HasIndex(p => p.ItemName).HasDatabaseName("IX_Products_ItemName");

            builder.HasOne<User>().WithMany().HasForeignKey(p => p.CreatedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Products_Users_CreatedBy");
            builder.HasOne<User>().WithMany().HasForeignKey(p => p.UpdatedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Products_Users_UpdatedBy");
            builder.HasOne<User>().WithMany().HasForeignKey(p => p.DeletedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Products_Users_DeletedBy");

            builder.HasIndex(p => p.CreatedBy).HasDatabaseName("IX_Products_CreatedBy");
            builder.HasIndex(p => p.UpdatedBy).HasDatabaseName("IX_Products_UpdatedBy");
            builder.HasIndex(p => p.DeletedBy).HasDatabaseName("IX_Products_DeletedBy");
        }
    }
}
