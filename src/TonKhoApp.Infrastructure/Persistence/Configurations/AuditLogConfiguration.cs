using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs", t =>
            {
                t.HasCheckConstraint("CK_AuditLogs_Action",
                    "[Action] IN ('CREATE','UPDATE','DELETE','PRICE_CHANGE','LOCK','UNLOCK'," +
                    "'ROLE_GRANT','ROLE_REVOKE','PASSWORD_RESET','PASSWORD_CHANGE'," +
                    "'LOGIN','LOGIN_FAILED','LOGOUT'," +
                    "'ORDER_CONFIRM','ORDER_RECEIVE','ORDER_CANCEL'," +
                    "'STOCK_ADJUST','STOCK_ISSUE','STOCK_REVERSAL')");
            });

            builder.HasKey(a => a.Id);

            builder.Property(a => a.EntityName).IsRequired().HasColumnType("varchar(50)");
            builder.Property(a => a.EntityId).HasColumnType("varchar(50)");
            builder.Property(a => a.Action).IsRequired().HasColumnType("varchar(30)");

            builder.Property(a => a.BeforeJson).HasColumnType("nvarchar(max)");
            builder.Property(a => a.AfterJson).HasColumnType("nvarchar(max)");

            builder.Property(a => a.UserName).HasMaxLength(50);
            builder.Property(a => a.IpAddress).HasColumnType("varchar(45)");
            builder.Property(a => a.Description).HasMaxLength(500);

            builder.Property(a => a.CreatedAt).HasColumnType("datetime2(3)");

            builder.HasIndex(a => new { a.EntityName, a.EntityId, a.CreatedAt })
                   .HasDatabaseName("IX_AuditLogs_Entity");

            builder.HasIndex(a => new { a.UserId, a.CreatedAt })
                   .HasDatabaseName("IX_AuditLogs_User_Date");

            builder.HasIndex(a => new { a.Action, a.CreatedAt })
                   .HasDatabaseName("IX_AuditLogs_Action_Date");

            builder.HasOne(a => a.User)
                   .WithMany()
                   .HasForeignKey(a => a.UserId)
                   .OnDelete(DeleteBehavior.NoAction)
                   .HasConstraintName("FK_AuditLogs_Users");
        }
    }
}
