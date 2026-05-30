using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace LabViroMol.Modules.Identity.Application;

public static class ApplicationModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
