using System;
using System.Net.Http;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LabViroMol.Modules.Inventory.IntegrationTests;


[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebAppFactory> { }

[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : IDisposable
{
    protected readonly HttpClient Client;
    private readonly IServiceScope _scope;
    
    protected readonly InventoryDbContext DbContext; 

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        Client = factory.CreateClient(); 
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();
        _scope.Dispose();
    }
}