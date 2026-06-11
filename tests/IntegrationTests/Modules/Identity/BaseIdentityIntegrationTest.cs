using System.Net.Http.Headers;
using LabViroMol.Modules.Identity.Infrastructure.Identity;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Identity.IntegrationTests;

[CollectionDefinition("IdentityIntegrationTests")]
public class IdentityTestCollection : ICollectionFixture<IdentityIntegrationTestWebAppFactory> { }

[Collection("IdentityIntegrationTests")]
public abstract class BaseIdentityIntegrationTest : IDisposable
{
    protected readonly HttpClient Client;
    private readonly IServiceScope _scope;
    protected readonly LabViroMolIdentityDbContext DbContext;
    protected readonly UserManager<ApplicationUser> UserManager;
    protected readonly RoleManager<ApplicationRole> RoleManager;
    protected readonly IdentityIntegrationTestWebAppFactory Factory;

    protected BaseIdentityIntegrationTest(IdentityIntegrationTestWebAppFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<LabViroMolIdentityDbContext>();
        UserManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    }

    protected void AuthenticateAs(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<string>? permissions = null)
    {
        var token = JwtTokenHelper.GenerateToken(userId, email, firstName, lastName, permissions: permissions);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthentication()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();
        _scope.Dispose();
    }
}
