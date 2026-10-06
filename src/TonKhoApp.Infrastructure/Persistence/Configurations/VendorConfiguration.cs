using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Persistence.Configurations
{
    public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
    {
        public void Configure(EntityTypeBuilder<Vendor> builder)
        {
            builder.ToTable("Vendors", t =>
            {
                t.HasCheckConstraint("CK_Vendors_Deleted",
                    "([Deleted] = 0 AND [DeletedAt] IS NULL) OR ([Deleted] = 1 AND [DeletedAt] IS NOT NULL)");
            });

            builder.HasKey(v => v.Id);

            builder.Property(v => v.VendorCode).IsRequired().HasMaxLength(50);
            builder.Property(v => v.VendorName).IsRequired().HasMaxLength(255);
            builder.Property(v => v.Address).HasMaxLength(500);
            builder.Property(v => v.Phone).HasMaxLength(50);
            builder.Property(v => v.Email).HasMaxLength(255);
            builder.Property(v => v.TaxCode).HasMaxLength(50);

            builder.Property(v => v.IsActive).HasDefaultValue(true);
            builder.Property(v => v.Deleted).IsRequired().HasDefaultValue(false);

            builder.Property(v => v.CreatedAt).HasColumnType("datetime2(3)");
            builder.Property(v => v.UpdatedAt).HasColumnType("datetime2(3)");
            builder.Property(v => v.DeletedAt).HasColumnType("datetime2(3)");

            builder.HasIndex(v => v.VendorCode)
                   .IsUnique().HasFilter("[Deleted] = 0").HasDatabaseName("UX_Vendors_VendorCode");
            builder.HasIndex(v => v.VendorName)
                   .IsUnique().HasFilter("[Deleted] = 0").HasDatabaseName("UX_Vendors_VendorName");

            builder.HasOne<User>().WithMany().HasForeignKey(v => v.CreatedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Vendors_Users_CreatedBy");
            builder.HasOne<User>().WithMany().HasForeignKey(v => v.UpdatedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Vendors_Users_UpdatedBy");
            builder.HasOne<User>().WithMany().HasForeignKey(v => v.DeletedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Vendors_Users_DeletedBy");

            builder.HasIndex(v => v.CreatedBy).HasDatabaseName("IX_Vendors_CreatedBy");
            builder.HasIndex(v => v.UpdatedBy).HasDatabaseName("IX_Vendors_UpdatedBy");
            builder.HasIndex(v => v.DeletedBy).HasDatabaseName("IX_Vendors_DeletedBy");
        }
    }
}
