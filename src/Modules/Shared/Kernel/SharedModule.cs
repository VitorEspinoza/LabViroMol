using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Kernel;

public static class SharedModule
{
    public static IServiceCollection AddSharedModule(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentUser, CurrentUserMock>();
        return services;
    }
}