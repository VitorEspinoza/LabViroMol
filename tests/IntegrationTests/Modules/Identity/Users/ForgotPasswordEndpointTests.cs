using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LabViroMol.Modules.Identity.Application.Users.ForgotPassword;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Identity.IntegrationTests.Users;

public class ForgotPasswordTests : UserEndpointsTestBase
{
    public ForgotPasswordTests(IdentityIntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200AndPersistEmailEvent_WhenEmailExists()
    {
        // Arrange
        var (_, email) = await SeedBasicUser("forgot.exists@labviromol.com");
        var command = new ForgotPasswordCommand(email);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/forgot-password", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var message = await DbContext.Set<OutboxMessage>()
            .AsNoTracking()
            .SingleAsync(m => m.Type == typeof(ForgotPasswordPersistentEvent).FullName);
        var @event = JsonSerializer.Deserialize<ForgotPasswordPersistentEvent>(message.Content, OutboxJson.Options);

        Assert.NotNull(@event);
        Assert.Equal(email, @event.Email);
        Assert.Null(message.ProcessedOn);
    }

    [Fact]
    public async Task ShouldReturn200AndNotPersistEmailEvent_WhenEmailDoesNotExist()
    {
        // Arrange
        const string email = "forgot.does-not-exist@labviromol.com";
        var command = new ForgotPasswordCommand(email);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/forgot-password", command);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var messageExists = await DbContext.Set<OutboxMessage>()
            .AsNoTracking()
            .AnyAsync(m => m.Type == typeof(ForgotPasswordPersistentEvent).FullName);

        Assert.False(messageExists);
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
