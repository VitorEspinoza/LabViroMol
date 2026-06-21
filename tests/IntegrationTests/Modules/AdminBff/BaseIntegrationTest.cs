using System.Net.Http.Headers;
using LabViroMol.IntegrationTests.Shared;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.AdminBff.IntegrationTests;

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory> { }

[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly IntegrationTestWebAppFactory Factory;
    private readonly IServiceScope _scope;

    protected readonly AssetsDbContext AssetsDbContext;
    protected readonly InventoryDbContext InventoryDbContext;
    protected readonly SchedulingDbContext SchedulingDbContext;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        _scope = factory.Services.CreateScope();

        AssetsDbContext = _scope.ServiceProvider.GetRequiredService<AssetsDbContext>();
        InventoryDbContext = _scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        SchedulingDbContext = _scope.ServiceProvider.GetRequiredService<SchedulingDbContext>();
    }

    protected void AuthenticateAs(IEnumerable<string> permissions) =>
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.Generate(permissions));

    protected void ClearAuthentication() =>
        Client.DefaultRequestHeaders.Authorization = null;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        await Factory.ResetDatabaseAsync();
    }
}
