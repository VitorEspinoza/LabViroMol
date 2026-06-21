using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.Materials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence.Configurations;

public class KitConfiguration : IEntityTypeConfiguration<Kit>
{
    public void Configure(EntityTypeBuilder<Kit> builder)
    {
        builder.ToTable("Kits");

        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id)
            .ValueGeneratedNever();
        
        builder.Property(k => k.Name).IsRequired().HasMaxLength(200);
        builder.Property(k => k.Description).HasMaxLength(1000);

        builder.OwnsMany(k => k.Materials, kitItemBuilder =>
        {
            kitItemBuilder.ToTable("KitItems");
            kitItemBuilder.WithOwner().HasForeignKey("KitId");
            
            kitItemBuilder.HasKey("KitId", nameof(KitItem.MaterialId));
            kitItemBuilder.Property(i => i.Quantity)
                .HasPrecision(18, 4);

            kitItemBuilder.HasOne<Material>()
                .WithMany()
                .HasForeignKey(i => i.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        builder.Metadata.FindNavigation(nameof(Kit.Materials))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

    }
}