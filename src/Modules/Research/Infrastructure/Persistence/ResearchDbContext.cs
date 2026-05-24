using LabViroMol.Modules.Shared.Infrastructure.Persistence.Extensions;

namespace LabViroMol.Modules.Research.Infrastructure.Persistence;

using LabViroMol.Modules.Shared.Infrastructure.Persistence.Converters;
using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore;

public class ResearchDbContext : DbContext
{
    public ResearchDbContext(DbContextOptions<ResearchDbContext> options) : base(options) { }

    public DbSet<Partner> Partners { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Researcher> Researchers { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<Publication> Publications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("research");
        modelBuilder.ApplyLabViroMolConfigurations();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResearchDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        var domainAssembly = typeof(PartnerId).Assembly;
        var sharedAssembly = typeof(UserId).Assembly;

        configurationBuilder.AddLabViroMolConventions(sharedAssembly);
        configurationBuilder.AddLabViroMolConventions(domainAssembly);
    }
}
