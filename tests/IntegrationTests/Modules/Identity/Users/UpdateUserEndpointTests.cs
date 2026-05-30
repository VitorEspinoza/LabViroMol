using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Presentation.Users;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public class UpdateUserTests : UserEndpointsTestBase
{
    public UpdateUserTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenRequestIsValid()
    {
        // Arrange
        await SeedAuthenticatedAdmin();
        var (targetId, _) = await SeedBasicUser();

        var request = new UpdateUserRequest(
            new UserInfo("Atualizado", "Nome", null, null, null),
            []);

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{targetId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenFirstNameIsEmpty()
    {
        // Arrange
        await SeedAuthenticatedAdmin();
        var (targetId, _) = await SeedBasicUser();

        var request = new UpdateUserRequest(
            new UserInfo("", "Nome", null, null, null),
            []);

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{targetId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn401_WhenNotAuthenticated()
    {
        // Arrange
        ClearAuthentication();
        var request = new UpdateUserRequest(
            new UserInfo("Atualizado", "Nome", null, null, null),
            []);

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{Guid.NewGuid()}", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
