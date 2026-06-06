using System.Reflection;
using FluentValidation;
using LabViroMol.Modules.Notify.Application.Notifications;
using LabViroMol.Modules.Notify.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Notify.Application;

public static class ApplicationModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<ISendNotification, SendNotificationService>();
        return services;
    }
}