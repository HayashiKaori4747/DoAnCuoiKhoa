using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TonKhoApp.Domain.Constants;
using TonKhoApp.Domain.Entities;

namespace TonKhoApp.Infrastructure.Persistence.Configurations
{
    public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
    {
        public void Configure(EntityTypeBuilder<StockTransaction> builder)
        {
            builder.ToTable("StockTransactions", t =>
            {
                t.HasCheckConstraint("CK_ST_Type",
                    "[TransactionType] IN ('IN','OUT','ADJUST','REVERSAL')");

                t.HasCheckConstraint("CK_ST_RefType",
                    "[RefType] IS NULL OR [RefType] IN ('ORDER','MANUAL','REVERSAL')");

                t.HasCheckConstraint("CK_ST_Balance",
                    "[BalanceAfter] = [BalanceBefore] + [Quantity]");

                t.HasCheckConstraint("CK_ST_BalanceSign",
                    "[BalanceBefore] >= 0 AND [BalanceAfter] >= 0");

                t.HasCheckConstraint("CK_ST_QuantitySign",
                    "([TransactionType] = 'IN'       AND [Quantity] > 0) OR " +
                    "([TransactionType] = 'OUT'      AND [Quantity] < 0) OR " +
                    "([TransactionType] = 'ADJUST'   AND [Quantity] <> 0) OR " +
                    "([TransactionType] = 'REVERSAL' AND [Quantity] <> 0)");

                t.HasCheckConstraint("CK_ST_Reversal",
                    "([TransactionType] =  'REVERSAL' AND [OriginalTransactionId] IS NOT NULL) OR " +
                    "([TransactionType] <> 'REVERSAL' AND [OriginalTransactionId] IS NULL)");

                t.HasCheckConstraint("CK_ST_UnitPrice", "[UnitPrice] IS NULL OR [UnitPrice] >= 0");
            });

            builder.HasKey(t => t.Id);

            builder.Property(t => t.TransactionType).IsRequired().HasColumnType("varchar(10)");

            builder.Property(t => t.Quantity).HasColumnType("decimal(18,3)");
            builder.Property(t => t.BalanceBefore).HasColumnType("decimal(18,3)");
            builder.Property(t => t.BalanceAfter).HasColumnType("decimal(18,3)");
            builder.Property(t => t.UnitPrice).HasColumnType("decimal(18,2)");

            builder.Property(t => t.RefType).HasColumnType("varchar(20)");

            builder.Property(t => t.TransactionDate).HasColumnType("datetime2(3)");

            builder.Property(t => t.Note).HasMaxLength(500);

            builder.HasIndex(t => new { t.ProductId, t.TransactionDate })
                   .IncludeProperties(t => new { t.Quantity, t.BalanceBefore, t.BalanceAfter })
                   .HasDatabaseName("IX_StockTransactions_Product_Date");

            builder.HasIndex(t => new { t.RefType, t.RefId })
                   .HasDatabaseName("IX_StockTransactions_Ref");

            builder.HasIndex(t => new { t.RefId, t.ProductId })
                   .IsUnique()
                   .HasFilter($"[RefType] = '{StockRefType.Order}'")
                   .HasDatabaseName("UX_StockTransactions_Order_Product");

            builder.HasIndex(t => t.OriginalTransactionId)
                   .IsUnique()
                   .HasFilter("[OriginalTransactionId] IS NOT NULL")
                   .HasDatabaseName("UX_StockTransactions_Reversal");

            builder.HasIndex(t => t.CreatedBy).HasDatabaseName("IX_StockTransactions_CreatedBy");

            builder.HasOne(t => t.Product)
                   .WithMany(p => p.StockTransactions)
                   .HasForeignKey(t => t.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.CreatedByUser)
                   .WithMany()
                   .HasForeignKey(t => t.CreatedBy)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.OriginalTransaction)
                   .WithMany()
                   .HasForeignKey(t => t.OriginalTransactionId)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
