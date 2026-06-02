using System.Net;
using LabViroMol.Modules.Shared.Kernel.Identity;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public class DeactivateUserTests : UserEndpointsTestBase
{
    public DeactivateUserTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenUserIsActive()
    {
        // Arrange
        await SeedAuthenticatedAdmin();
        var (targetId, _) = await SeedBasicUser();

        // Act
        var response = await Client.PostAsync($"{BaseRoute}/{targetId}/deactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        DbContext.ChangeTracker.Clear();
        var domainUser = await DbContext.DomainUsers.FindAsync(UserId.From(targetId));
        Assert.NotNull(domainUser?.DeactivatedAt);
    }

    [Fact]
    public async Task ShouldReturn200_WhenAlreadyDeactivated()
    {
        // Arrange
        await SeedAuthenticatedAdmin();
        var (targetId, _) = await SeedBasicUser();
        await Client.PostAsync($"{BaseRoute}/{targetId}/deactivate", null);

        // Act
        var response = await Client.PostAsync($"{BaseRoute}/{targetId}/deactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn401_WhenNotAuthenticated()
    {
        // Arrange
        ClearAuthentication();

        // Act
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/deactivate", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
