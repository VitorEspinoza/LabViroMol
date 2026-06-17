using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Identity.Infrastructure.Identity;
using LabViroMol.Modules.Shared.Infrastructure.Extensions;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Extensions;
using LabViroMol.Modules.Shared.Kernel.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Identity.Infrastructure.Persistence;

public class LabViroMolIdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public LabViroMolIdentityDbContext(DbContextOptions<LabViroMolIdentityDbContext> options) : base(options) { }

    public DbSet<User> DomainUsers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<ApplicationUser>().ToTable("IdentityUsers");
        modelBuilder.Entity<ApplicationRole>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        modelBuilder.ApplyPersistenceConfigs();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LabViroMolIdentityDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        var domainAssembly = typeof(User).Assembly;
        var sharedAssembly = typeof(UserId).Assembly;

        configurationBuilder.AddLabViroMolConventions(sharedAssembly);
        configurationBuilder.AddLabViroMolConventions(domainAssembly);
    }
}
