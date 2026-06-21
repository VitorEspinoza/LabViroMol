using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Notify.IntegrationTests;

namespace LabViroMol.Modules.Notify.IntegrationTests.Notifications;

public class GetNotificationsTests : NotificationEndpointsTestBase
{
    public GetNotificationsTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200WithNotification_WhenUserHasMatchingPermission()
    {
        await SeedAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        Assert.Single(notifications!);
    }

    [Fact]
    public async Task ShouldReturnEmpty_WhenUserHasNoMatchingPermission()
    {
        await SeedAsync(targetPermission: "Some.Other.Permission");

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var notifications = await response.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        Assert.Empty(notifications!);
    }

    [Fact]
    public async Task ShouldReturn401_WhenNotAuthenticated()
    {
        ClearAuthentication();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class DismissNotificationTests : NotificationEndpointsTestBase
{
    public DismissNotificationTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204_WhenNotificationExists()
    {
        var notificationId = await SeedAsync();

        var response = await Client.PostAsync($"{BaseRoute}/dismiss/{notificationId}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var listResponse = await Client.GetAsync(BaseRoute);
        var notifications = await listResponse.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        Assert.Empty(notifications!);
    }

    [Fact]
    public async Task ShouldReturn422_WhenNotificationDoesNotExist()
    {
        var response = await Client.PostAsync($"{BaseRoute}/dismiss/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn204_WhenDismissingAlreadyDismissedNotification()
    {
        var notificationId = await SeedAsync();
        await Client.PostAsync($"{BaseRoute}/dismiss/{notificationId}", null);

        var response = await Client.PostAsync($"{BaseRoute}/dismiss/{notificationId}", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn401_WhenNotAuthenticated()
    {
        ClearAuthentication();

        var response = await Client.PostAsync($"{BaseRoute}/dismiss/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public class DismissAllNotificationsTests : NotificationEndpointsTestBase
{
    public DismissAllNotificationsTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn204AndClearAllUnread_WhenNotificationsExist()
    {
        await SeedAsync();
        await SeedAsync();

        var response = await Client.PostAsJsonAsync($"{BaseRoute}/dismiss/all", new { });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var listResponse = await Client.GetAsync(BaseRoute);
        var notifications = await listResponse.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        Assert.Empty(notifications!);
    }

    [Fact]
    public async Task ShouldReturn204_WhenThereAreNoNotificationsToDismiss()
    {
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/dismiss/all", new { });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ShouldNotDismissNotificationForOtherUsers()
    {
        await SeedAsync();

        var dismissResponse = await Client.PostAsJsonAsync($"{BaseRoute}/dismiss/all", new { });
        Assert.Equal(HttpStatusCode.NoContent, dismissResponse.StatusCode);

        AuthenticateAsUser();
        var listResponse = await Client.GetAsync(BaseRoute);
        var notifications = await listResponse.Content.ReadFromJsonAsync<List<NotificationResponse>>();
        Assert.Single(notifications!);
    }
}

public record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    string Type,
    string ReferenceId,
    string ReferenceModule,
    DateTimeOffset CreatedAt);
