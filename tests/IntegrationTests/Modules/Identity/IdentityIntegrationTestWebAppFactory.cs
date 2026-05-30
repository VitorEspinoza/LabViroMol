using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Identity.IntegrationTests;

public class IdentityIntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Key", "IntegrationTestSecretKeyThatIsLongEnoughForHmacSha256!!");
        builder.UseSetting("Jwt:Issuer", "TestIssuer");
        builder.UseSetting("Jwt:Audience", "TestAudience");
        builder.UseSetting("Storage:ImageFolderPath", Path.GetTempPath());

        builder.ConfigureServices(services =>
        {
            ReplaceDbContext<LabViroMolIdentityDbContext>(services, "LabViroMol_Identity_IT_Db");
            ReplaceDbContext<InventoryDbContext>(services, "LabViroMol_Inventory_IT_Db");
            ReplaceDbContext<ResearchDbContext>(services, "LabViroMol_Research_IT_Db");
            ReplaceDbContext<SchedulingDbContext>(services, "LabViroMol_Scheduling_IT_Db");
            ReplaceDbContext<AssetsDbContext>(services, "LabViroMol_Assets_IT_Db");

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LabViroMolIdentityDbContext>();
            db.Database.EnsureCreated();
        });
    }

    private static void ReplaceDbContext<TContext>(IServiceCollection services, string dbName)
        where TContext : DbContext
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<TContext>)
                        || d.ServiceType == typeof(TContext)
                        || (d.ServiceType.IsGenericType
                            && d.ServiceType.GetGenericArguments().Length == 1
                            && d.ServiceType.GetGenericArguments()[0] == typeof(TContext)))
            .ToList();

        foreach (var descriptor in descriptors)
            services.Remove(descriptor);

        services.AddDbContext<TContext>(options =>
            options.UseInMemoryDatabase(dbName));
    }
}
