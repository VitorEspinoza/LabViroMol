using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;

namespace LabViroMol.Modules.Notify.IntegrationTests.Notifications;

public static class NotificationDataSeeder
{
    public static async Task<Guid> SeedAsync(
        NotifyDbContext dbContext,
        string? targetPermission = null,
        string title = "Novo agendamento pendente",
        string message = "Há um novo agendamento aguardando aprovação.")
    {
        var notification = Notification.Create(
            title,
            message,
            targetPermission ?? BaseIntegrationTest.TargetPermission,
            referenceId: Guid.NewGuid().ToString(),
            referenceModule: "Scheduling",
            type: "Info").Data!;

        await dbContext.Notifications.AddAsync(notification);
        await dbContext.SaveChangesAsync();

        return notification.Id.Value;
    }
}
