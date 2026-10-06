using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Code).IsRequired().HasColumnType("varchar(50)");

            builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
            builder.Property(r => r.Description).HasMaxLength(255);

            builder.Property(r => r.IsSystem).HasDefaultValue(false);

            builder.Property(r => r.CreatedAt).HasColumnType("datetime2(3)");
            builder.Property(r => r.UpdatedAt).HasColumnType("datetime2(3)");

            builder.HasIndex(r => r.Code).IsUnique().HasDatabaseName("UX_Roles_Code");

            builder.HasIndex(r => r.Name).IsUnique().HasDatabaseName("UX_Roles_Name");

            builder.HasOne<User>().WithMany().HasForeignKey(r => r.CreatedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Roles_Users_CreatedBy");
            builder.HasOne<User>().WithMany().HasForeignKey(r => r.UpdatedBy)
                   .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Roles_Users_UpdatedBy");

            builder.HasIndex(r => r.CreatedBy).HasDatabaseName("IX_Roles_CreatedBy");
            builder.HasIndex(r => r.UpdatedBy).HasDatabaseName("IX_Roles_UpdatedBy");
        }
    }
}
