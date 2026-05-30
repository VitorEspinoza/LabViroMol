using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using NSubstitute;

namespace LabViroMol.Modules.Inventory.IntegrationTests;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>
{
    public IProjectChecker ProjectCheckerMock { get; } = Substitute.For<IProjectChecker>();

    public IntegrationTestWebAppFactory()
    {
        ProjectCheckerMock
            .IsEligibleForOrdersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        ProjectCheckerMock
            .IsEligibleForConsumptionAsync(Arg.Any<Guid>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
    }

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
                options.UseInMemoryDatabase("LabViroMol_Identity_Integration_Db"));
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
                options.UseInMemoryDatabase("LabViroMol_Integration_Db");
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            db.Database.EnsureCreated();

            var checkerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IProjectChecker));
            if (checkerDescriptor != null) services.Remove(checkerDescriptor);

            services.AddSingleton(ProjectCheckerMock);
        });
    }
}
