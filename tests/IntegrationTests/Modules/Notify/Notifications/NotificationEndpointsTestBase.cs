using LabViroMol.Modules.Notify.IntegrationTests;

namespace LabViroMol.Modules.Notify.IntegrationTests.Notifications;

public abstract class NotificationEndpointsTestBase : BaseIntegrationTest
{
    protected const string BaseRoute = "/api/notify/notifications";

    protected NotificationEndpointsTestBase(IntegrationTestWebAppFactory factory) : base(factory) { }

    protected Task<Guid> SeedAsync(string? targetPermission = null)
        => NotificationDataSeeder.SeedAsync(DbContext, targetPermission);
}
