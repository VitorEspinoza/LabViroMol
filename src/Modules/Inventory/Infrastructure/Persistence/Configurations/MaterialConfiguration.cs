using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materials");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedNever();
     
        builder.HasMany(m => m.Transactions) 
            .WithOne()
            .HasForeignKey(t => t.MaterialId)
            .IsRequired();
        
        builder.Property(m => m.StockQuantity)
            .HasPrecision(18, 4); 

        builder.Property(m => m.Name)
            .HasMaxLength(200)
            .IsRequired();
        
          
        builder.Property(m => m.Unit)
            .HasConversion<string>();
        
        builder.HasOne<MaterialType>()
            .WithMany()
            .HasForeignKey(m => m.TypeId)
            .OnDelete(DeleteBehavior.Restrict); 
    }
}