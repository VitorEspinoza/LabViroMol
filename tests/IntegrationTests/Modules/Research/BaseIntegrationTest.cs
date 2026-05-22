using LabViroMol.Modules.Research.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Research.IntegrationTests;

[CollectionDefinition("ResearchIntegrationTests")]
public class ResearchIntegrationTestCollection : ICollectionFixture<ResearchIntegrationTestWebAppFactory> { }

[Collection("ResearchIntegrationTests")]
public abstract class BaseIntegrationTest : IDisposable
{
    protected readonly HttpClient Client;
    private readonly IServiceScope _scope;
    protected readonly ResearchDbContext DbContext;

    protected BaseIntegrationTest(ResearchIntegrationTestWebAppFactory factory)
    {
        Client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ResearchDbContext>();
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();
        _scope.Dispose();
    }
}
