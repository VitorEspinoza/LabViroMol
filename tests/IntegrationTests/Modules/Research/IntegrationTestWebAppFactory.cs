using System.Linq;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace LabViroMol.Modules.Research.IntegrationTests;

public class ResearchIntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Key", "IntegrationTestSecretKeyThatIsLongEnoughForHmacSha256!!");
        builder.UseSetting("Jwt:Issuer", "TestIssuer");
        builder.UseSetting("Jwt:Audience", "TestAudience");

        builder.ConfigureServices(services =>
        {
            var identityDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<LabViroMolIdentityDbContext>)
                            || d.ServiceType == typeof(LabViroMolIdentityDbContext)
                            || (d.ServiceType.IsGenericType
                                && d.ServiceType.GetGenericArguments().Length == 1
                                && d.ServiceType.GetGenericArguments()[0] == typeof(LabViroMolIdentityDbContext)))
                .ToList();
            foreach (var descriptor in identityDescriptors) services.Remove(descriptor);
            services.AddDbContext<LabViroMolIdentityDbContext>(options =>
                options.UseInMemoryDatabase("LabViroMol_Identity_Research_Integration_Db"));
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ResearchDbContext>)
                         || d.ServiceType == typeof(ResearchDbContext)
                         || (d.ServiceType.IsGenericType
                             && d.ServiceType.GetGenericArguments().Length == 1
                             && d.ServiceType.GetGenericArguments()[0] == typeof(ResearchDbContext)))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            services.AddDbContext<ResearchDbContext>(options =>
                options.UseInMemoryDatabase("LabViroMol_Research_IntegrationTests_Db"));

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ResearchDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
