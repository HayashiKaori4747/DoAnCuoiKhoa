using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens", t =>
            {
                t.HasCheckConstraint("CK_RefreshTokens_Revoked",
                    "([RevokedAt] IS NULL AND [RevokedReason] IS NULL) OR " +
                    "([RevokedAt] IS NOT NULL AND [RevokedReason] IS NOT NULL)");

                t.HasCheckConstraint("CK_RefreshTokens_Reason",
                    "[RevokedReason] IS NULL OR [RevokedReason] IN " +
                    "('LOGOUT','ROTATED','ADMIN_LOCK','REUSE_DETECTED','PASSWORD_CHANGED')");

                t.HasCheckConstraint("CK_RefreshTokens_Expiry", "[ExpiresAt] > [CreatedAt]");
            });

            builder.HasKey(t => t.Id);

            builder.Property(t => t.TokenHash).IsRequired().HasColumnType("varchar(88)");

            builder.Property(t => t.ExpiresAt).HasColumnType("datetime2(3)");
            builder.Property(t => t.CreatedAt).HasColumnType("datetime2(3)");
            builder.Property(t => t.RevokedAt).HasColumnType("datetime2(3)");

            builder.Property(t => t.RevokedReason).HasColumnType("varchar(30)");

            builder.Property(t => t.CreatedByIp).HasColumnType("varchar(45)");

            builder.HasIndex(t => t.TokenHash)
                   .IsUnique()
                   .HasDatabaseName("UX_RefreshTokens_TokenHash");

            builder.HasIndex(t => new { t.UserId, t.ExpiresAt })
                   .HasDatabaseName("IX_RefreshTokens_User_Expires");

            builder.HasOne(t => t.User)
                   .WithMany(u => u.RefreshTokens)
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.ReplacedByToken)
                   .WithMany()
                   .HasForeignKey(t => t.ReplacedByTokenId)
                   .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(t => t.ReplacedByTokenId)
                   .IsUnique()
                   .HasFilter("[ReplacedByTokenId] IS NOT NULL")
                   .HasDatabaseName("UX_RefreshTokens_ReplacedBy");
        }
    }
}
