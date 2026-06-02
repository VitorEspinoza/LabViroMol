using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Identity.Application.Users.ResetPassword;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public class ResetPasswordTests : UserEndpointsTestBase
{
    public ResetPasswordTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenTokenIsValid()
    {
        // Arrange
        var (userId, email) = await SeedBasicUser();
        var user = await UserManager.FindByIdAsync(userId.ToString());
        var token = await UserManager.GeneratePasswordResetTokenAsync(user!);

        var command = new ResetPasswordCommand(email, token, "NewSecure@12345");

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/reset-password", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenTokenIsInvalid()
    {
        // Arrange
        var (_, email) = await SeedBasicUser();

        var command = new ResetPasswordCommand(email, "invalid-token", "NewSecure@12345");

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/reset-password", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenPasswordTooWeak()
    {
        // Arrange
        var (userId, email) = await SeedBasicUser();
        var user = await UserManager.FindByIdAsync(userId.ToString());
        var token = await UserManager.GeneratePasswordResetTokenAsync(user!);

        var command = new ResetPasswordCommand(email, token, "123");

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/reset-password", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
