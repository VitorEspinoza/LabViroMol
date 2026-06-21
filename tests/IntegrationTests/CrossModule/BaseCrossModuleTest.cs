using System.Net.Http.Headers;
using LabViroMol.IntegrationTests.Shared;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.IntegrationTests.CrossModule;

[CollectionDefinition("CrossModuleTests")]
public class CrossModuleTestCollection : ICollectionFixture<IntegrationTestWebAppFactory> { }

[Collection("CrossModuleTests")]
public abstract class BaseCrossModuleTest : IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly IntegrationTestWebAppFactory Factory;
    private readonly IServiceScope _scope;

    protected readonly LabViroMolIdentityDbContext IdentityDbContext;
    protected readonly ResearchDbContext ResearchDbContext;
    protected readonly InventoryDbContext InventoryDbContext;
    protected readonly NotifyDbContext NotifyDbContext;

    protected BaseCrossModuleTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        _scope = factory.Services.CreateScope();

        IdentityDbContext = _scope.ServiceProvider.GetRequiredService<LabViroMolIdentityDbContext>();
        ResearchDbContext = _scope.ServiceProvider.GetRequiredService<ResearchDbContext>();
        InventoryDbContext = _scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        NotifyDbContext = _scope.ServiceProvider.GetRequiredService<NotifyDbContext>();
    }

    protected void AuthenticateAs(IEnumerable<string> permissions, Guid? userId = null) =>
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Generate(permissions, userId));

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        await Factory.ResetDatabaseAsync();
    }
}
