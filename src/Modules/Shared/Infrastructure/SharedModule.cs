using LabViroMol.Modules.Shared.Kernel.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Shared.Infrastructure;

public static class SharedModule
{
    public static IServiceCollection AddSharedModule(this IServiceCollection services)
    {
        services.AddSingleton<ICurrentUser, CurrentUserMock>();
        return services;
    }
}