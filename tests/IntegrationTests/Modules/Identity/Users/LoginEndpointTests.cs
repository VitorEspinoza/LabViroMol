using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Identity.Application.Users.Login;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public class LoginTests : UserEndpointsTestBase
{
    public LoginTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200WithCookies_WhenCredentialsAreValid()
    {
        // Arrange
        var password = "User@123456";
        var (_, email) = await SeedBasicUser(password: password);
        ClearAuthentication();

        var command = new LoginCommand(email, password);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));
        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.Contains("X-Access-Token"));
        Assert.Contains(cookies, c => c.Contains("X-Refresh-Token"));
    }

    [Fact]
    public async Task ShouldReturnError_WhenPasswordIsWrong()
    {
        // Arrange
        var (_, email) = await SeedBasicUser();
        ClearAuthentication();

        var command = new LoginCommand(email, "WrongPassword!1");

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", command);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnError_WhenUserIsLockedOut()
    {
        // Arrange
        var (userId, email) = await SeedBasicUser();

        // Deactivate to trigger lockout via the handler
        await SeedAuthenticatedAdmin();
        await Client.PostAsync($"{BaseRoute}/{userId}/deactivate", null);
        ClearAuthentication();

        var command = new LoginCommand(email, "User@123456");

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", command);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
