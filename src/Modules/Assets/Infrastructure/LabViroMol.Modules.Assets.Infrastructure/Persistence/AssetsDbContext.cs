using Kernel.Persistence.Converters;
using LabViroMol.Modules.Assets.Domain;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Assets.Infrastructure.Persistence;

public class AssetsDbContext : DbContext
{
    public AssetsDbContext(DbContextOptions<AssetsDbContext> options) : base(options) {}
    
    public DbSet<Equipment> Equipments { get; set; }
    public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("assets");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssetsDbContext).Assembly);
    }
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<Quantity>()
            .HaveConversion<QuantityConverter>();
        
        var assembly = typeof(EquipmentId).Assembly; 
    
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