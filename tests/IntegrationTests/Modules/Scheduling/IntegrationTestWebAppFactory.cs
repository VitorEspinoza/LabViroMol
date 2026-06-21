using LabViroMol.IntegrationTests.Shared;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LabViroMol.Modules.Scheduling.IntegrationTests;

public class IntegrationTestWebAppFactory : LabViroMolWebAppFactory
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        var existingConfigurations = services
            .Where(d => d.ServiceType == typeof(IConfigureOptions<RateLimiterOptions>))
            .ToList();
        foreach (var descriptor in existingConfigurations)
            services.Remove(descriptor);

        services.Configure<RateLimiterOptions>(options =>
            options.AddFixedWindowLimiter("SchedulingPolicy", opt =>
            {
                opt.PermitLimit = int.MaxValue;
                opt.Window = TimeSpan.FromHours(1);
            }));
    }
}
