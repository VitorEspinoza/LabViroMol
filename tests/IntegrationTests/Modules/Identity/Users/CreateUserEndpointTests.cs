using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LabViroMol.Modules.Identity.Application.Users.CreateUser;
using LabViroMol.Modules.Identity.Contracts;
using Xunit;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public class CreateUserTests : UserEndpointsTestBase
{
    public CreateUserTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201WithResetToken_WhenRequestIsValid()
    {
        // Arrange
        await SeedAuthenticatedAdmin();
        var command = new CreateUserCommand(
            new UserInfo("João", "Silva", null, null, null),
            "joao@labviromol.com",
            []);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ResetTokenResponse>();
        Assert.NotNull(body?.ResetToken);
    }

    [Fact]
    public async Task ShouldReturn400_WhenEmailIsEmpty()
    {
        // Arrange
        await SeedAuthenticatedAdmin();
        var command = new CreateUserCommand(
            new UserInfo("João", "Silva", null, null, null),
            "",
            []);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn401_WhenNotAuthenticated()
    {
        // Arrange
        ClearAuthentication();
        var command = new CreateUserCommand(
            new UserInfo("João", "Silva", null, null, null),
            "joao@labviromol.com",
            []);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, command);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private record ResetTokenResponse(string ResetToken);
}
