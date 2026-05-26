using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Converters;
using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence.Converters;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Kit> Kits { get; set; }
    public DbSet<Material> Materials { get; set; }
    public DbSet<MaterialType> MaterialTypes { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<StockTransaction> StockTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema("inventory");
        
        modelBuilder.ApplyPersistenceConfigs();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Quantity>()
            .HaveConversion<QuantityConverter>();
  
        var assembly = typeof(MaterialId).Assembly; 
    
        var strongIdTypes = assembly.GetTypes()
            .Where(t => t.IsValueType && t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStrongId<>)));

        configurationBuilder.Properties<UserId>().HaveConversion<StrongIdConverter<UserId>>();
        
        foreach (var idType in strongIdTypes)
        {
            var converterType = typeof(StrongIdConverter<>).MakeGenericType(idType);
        
            configurationBuilder.Properties(idType).HaveConversion(converterType);
        }
        
    }
}