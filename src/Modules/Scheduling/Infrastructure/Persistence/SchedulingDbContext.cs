using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Converters;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Scheduling.Infrastructure.Persistence;

public class SchedulingDbContext : DbContext
{
    public SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : base(options) { }
    
    public DbSet<Schedule> Schedules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasDefaultSchema("scheduling");
        
        modelBuilder.ApplyPersistenceConfigs();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        var assembly = typeof(ScheduleId).Assembly; 
    
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