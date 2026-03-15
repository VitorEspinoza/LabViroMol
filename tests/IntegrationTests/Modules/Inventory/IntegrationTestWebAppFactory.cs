using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace LabViroMol.Modules.Inventory.IntegrationTests;

public class IntegrationTestWebAppFactory: WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>)
                         || d.ServiceType == typeof(InventoryDbContext)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericArguments().Length == 1
                             && d.ServiceType.GetGenericArguments()[0] == typeof(InventoryDbContext)))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            services.AddDbContext<InventoryDbContext>(options =>
            {
                options.UseInMemoryDatabase("LabViroMol_IntegrationTests_Db");
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            db.Database.EnsureCreated();
        });
    }
}