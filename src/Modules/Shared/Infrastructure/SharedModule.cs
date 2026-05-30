using LabViroMol.Modules.Shared.Infrastructure.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Shared.Infrastructure;

public static class SharedModule
{
    public static IServiceCollection AddSharedModule(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}