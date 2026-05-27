using Kernel.Extensions;
using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Notify.Infrastructure.Persistence;

public class NotifyDbContext : DbContext
{
    public NotifyDbContext(DbContextOptions<NotifyDbContext> options) : base(options) {}
    
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("notify");
        modelBuilder.ApplyLabViroMolConfigurations();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotifyDbContext).Assembly);
    }
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        var domainAssembly = typeof(NotificationId).Assembly;
        var sharedAssembly = typeof(UserId).Assembly;

        configurationBuilder.AddLabViroMolConventions(sharedAssembly);
        configurationBuilder.AddLabViroMolConventions(domainAssembly);
    }
}