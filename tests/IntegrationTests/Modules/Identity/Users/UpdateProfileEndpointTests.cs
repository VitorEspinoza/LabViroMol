using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Identity.Presentation.Users;
using Xunit;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public class UpdateProfileTests : UserEndpointsTestBase
{
    public UpdateProfileTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenRequestIsValid()
    {
        // Arrange
        var (userId, email) = await SeedBasicUser();
        AuthenticateAs(userId, email, "Basic", "User");

        var request = new UpdateProfileRequest(
            new UserInfo("Novo", "Nome", "123456", null, null));

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/me", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenFirstNameIsEmpty()
    {
        // Arrange
        var (userId, email) = await SeedBasicUser();
        AuthenticateAs(userId, email, "Basic", "User");

        var request = new UpdateProfileRequest(
            new UserInfo("", "Nome", null, null, null));

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/me", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
