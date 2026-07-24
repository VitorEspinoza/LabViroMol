using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence.Configurations;

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("StockTransactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(t => t.OrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(t => t.Type)
            .HasConversion<string>();

        builder.Property(t => t.Quantity)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(t => t.Type)
            .IsRequired();

        builder.Property(t => t.Justification)
            .HasMaxLength(1000);

        builder.HasIndex(t => new { t.Type, t.TransactedAt, t.MaterialId });

        builder.HasIndex(t => new { t.ProjectId, t.Type, t.TransactedAt });

        builder.HasIndex(t => t.TransactedAt);
    }
}
