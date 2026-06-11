using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Identity.Application.Users.ForgotPassword;
using LabViroMol.Modules.Notify.Contracts;
using NSubstitute;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public class ForgotPasswordTests : UserEndpointsTestBase
{
    public ForgotPasswordTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200AndSendEmail_WhenEmailExists()
    {
        // Arrange
        var (_, email) = await SeedBasicUser("forgot.exists@labviromol.com");
        var command = new ForgotPasswordCommand(email);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/forgot-password", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Factory.EmailSenderMock.Received(1).SendEmail(
            email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturn200AndNotSendEmail_WhenEmailDoesNotExist()
    {
        // Arrange
        const string email = "forgot.does-not-exist@labviromol.com";
        var command = new ForgotPasswordCommand(email);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/forgot-password", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Factory.EmailSenderMock.DidNotReceive().SendEmail(
            email, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturn400_WhenEmailIsInvalid()
    {
        // Arrange
        var command = new ForgotPasswordCommand("not-an-email");

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/forgot-password", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
