using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LabViroMol.Modules.Identity.Application.Users.CreateUser;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

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
    public async Task ShouldPersistWelcomeEmailEvent_WhenUserIsCreated()
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

        var message = await DbContext.Set<OutboxMessage>()
            .AsNoTracking()
            .SingleAsync(m => m.Type == typeof(ResetPasswordPersistentEvent).FullName);
        var @event = JsonSerializer.Deserialize<ResetPasswordPersistentEvent>(message.Content, OutboxJson.Options);

        Assert.NotNull(@event);
        Assert.Equal("joao.welcome@labviromol.com", @event.Email);
        Assert.Null(message.ProcessedOn);
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
