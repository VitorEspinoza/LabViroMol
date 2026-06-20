using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Domain.References;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("InventoryOrders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .ValueGeneratedNever();

        builder.Property(o => o.Status)
            .HasConversion<string>();

        builder.HasOne<Material>()
            .WithMany()
            .HasForeignKey(o => o.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(o => o.Processing, p =>
        {
            p.Property(x => x.Notes)
                .HasColumnName("ProcessingNotes")
                .HasMaxLength(1000);
            
            p.Property(x => x.ProcessedAt)
                .HasColumnName("ProcessingDate");

            p.Property(x => x.ProcessedBy)
                .HasColumnName("ProcessedByUser");

            p.Property(x => x.ProcessedByName)
                .HasColumnName("ProcessedByName");
        });

        builder.OwnsOne(o => o.Receipt, r =>
        {
            r.Property(x => x.ReceivedAt)
                .HasColumnName("ReceivedAt")
                .IsRequired();
            
            r.Property(x => x.Notes)
                .HasColumnName("ReceiptNotes")
                .HasMaxLength(1000);
            
            r.Property(x => x.Quantity)
                .HasColumnName("ReceivedQuantity")
                .HasPrecision(18, 4);
            
            r.Property(x => x.ReceivedByName)
                .HasColumnName("ReceivedByName");
            
            r.Property(x => x.ReceivedBy)
                .HasColumnName("ReceivedByUserId");
            
        });
    }
}
