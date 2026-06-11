using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Identity.Application.Users.CreateUser;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Notify.Contracts;
using NSubstitute;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public class CreateUserTests : UserEndpointsTestBase
{
    public CreateUserTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        // Arrange
        await SeedAuthenticatedAdmin();
        var command = new CreateUserCommand(
            new UserInfo("João", "Silva", null, null, null, null),
            "joao@labviromol.com",
            []);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldSendWelcomeEmail_WhenUserIsCreated()
    {
        // Arrange
        await SeedAuthenticatedAdmin();
        var command = new CreateUserCommand(
            new UserInfo("João", "Silva", null, null, null, null),
            "joao.welcome@labviromol.com",
            []);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await Factory.EmailSenderMock.Received(1).SendEmail(
            "joao.welcome@labviromol.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShouldReturn400_WhenEmailIsEmpty()
    {
        // Arrange
        await SeedAuthenticatedAdmin();
        var command = new CreateUserCommand(
            new UserInfo("João", "Silva", null, null, null, null),
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
            new UserInfo("João", "Silva", null, null, null, null),
            "joao@labviromol.com",
            []);

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, command);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
