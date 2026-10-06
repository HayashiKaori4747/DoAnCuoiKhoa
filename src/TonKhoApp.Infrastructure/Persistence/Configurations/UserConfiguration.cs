using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", t =>
            {
                t.HasCheckConstraint("CK_Users_Deleted",
                    "([Deleted] = 0 AND [DeletedAt] IS NULL) OR ([Deleted] = 1 AND [DeletedAt] IS NOT NULL)");
            });

            builder.HasKey(u => u.Id);

            builder.Property(u => u.UserId).IsRequired().HasMaxLength(50);

            builder.Property(u => u.UserPassword)
                   .IsRequired()
                   .HasColumnType("varchar(255)");

            builder.Property(u => u.FullName).HasMaxLength(100);

            builder.Property(u => u.IsActive).HasDefaultValue(true);

            builder.Property(u => u.Deleted).IsRequired().HasDefaultValue(false);

            builder.Property(u => u.LastLoginAt).HasColumnType("datetime2(3)");
            builder.Property(u => u.CreatedAt).HasColumnType("datetime2(3)");
            builder.Property(u => u.UpdatedAt).HasColumnType("datetime2(3)");
            builder.Property(u => u.DeletedAt).HasColumnType("datetime2(3)");

            builder.HasIndex(u => u.UserId)
                   .IsUnique()
                   .HasFilter("[Deleted] = 0")
                   .HasDatabaseName("UX_Users_UserId");

            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(u => u.CreatedBy)
                   .OnDelete(DeleteBehavior.NoAction)
                   .HasConstraintName("FK_Users_Users_CreatedBy");

            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(u => u.UpdatedBy)
                   .OnDelete(DeleteBehavior.NoAction)
                   .HasConstraintName("FK_Users_Users_UpdatedBy");

            builder.HasOne<User>()
                   .WithMany()
                   .HasForeignKey(u => u.DeletedBy)
                   .OnDelete(DeleteBehavior.NoAction)
                   .HasConstraintName("FK_Users_Users_DeletedBy");

            builder.HasIndex(u => u.CreatedBy).HasDatabaseName("IX_Users_CreatedBy");
            builder.HasIndex(u => u.UpdatedBy).HasDatabaseName("IX_Users_UpdatedBy");
            builder.HasIndex(u => u.DeletedBy).HasDatabaseName("IX_Users_DeletedBy");
        }
    }
}
