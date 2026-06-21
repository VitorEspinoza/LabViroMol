using LabViroMol.IntegrationTests.Shared;
using LabViroMol.Modules.Identity.Infrastructure.Identity;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Identity.IntegrationTests;

[CollectionDefinition("IdentityIntegrationTests")]
public class IdentityTestCollection : ICollectionFixture<IdentityIntegrationTestWebAppFactory> { }

[Collection("IdentityIntegrationTests")]
public abstract class BaseIdentityIntegrationTest : BaseIntegrationTest<LabViroMolIdentityDbContext>
{
    protected readonly UserManager<ApplicationUser> UserManager;
    protected readonly RoleManager<ApplicationRole> RoleManager;
    protected new readonly IdentityIntegrationTestWebAppFactory Factory;
    private readonly IServiceScope _identityScope;

    protected BaseIdentityIntegrationTest(IdentityIntegrationTestWebAppFactory factory) : base(factory)
    {
        Factory = factory;
        _identityScope = factory.Services.CreateScope();
        UserManager = _identityScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager = _identityScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    }

    public override async Task DisposeAsync()
    {
        _identityScope.Dispose();
        await base.DisposeAsync();
    }

    protected void AuthenticateAs(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        IEnumerable<string>? permissions = null) =>
        AuthenticateAs(permissions ?? [], userId, email, firstName, lastName);
}
