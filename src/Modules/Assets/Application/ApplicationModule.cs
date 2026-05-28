using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Assets.Application;

public static class ApplicationModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}