using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using NSubstitute;
using Respawn;
using Testcontainers.PostgreSql;

namespace LabViroMol.IntegrationTests.Shared;

public class LabViroMolWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private NpgsqlConnection _respawnConnection = null!;
    private Respawner _respawner = null!;

    public string ConnectionString => _container.GetConnectionString();
    public ISendEmail EmailSenderMock { get; } = Substitute.For<ISendEmail>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:LabViroMol", ConnectionString);
        builder.UseSetting("Jwt:Key", TestJwt.Key);
        builder.UseSetting("Jwt:Issuer", TestJwt.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwt.Audience);
        builder.UseSetting("Storage:RootFolder", Path.Combine(Path.GetTempPath(), "labviromol-it"));
        builder.UseSetting("Storage:ImageFolderPath", Path.GetTempPath());

        builder.ConfigureServices(services =>
        {
            var outboxHostedService = services
                .SingleOrDefault(d => d.ServiceType == typeof(IHostedService)
                                       && d.ImplementationType == typeof(OutboxBackgroundService));
            if (outboxHostedService is not null)
                services.Remove(outboxHostedService);

            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISendEmail));
            if (emailDescriptor is not null)
                services.Remove(emailDescriptor);
            services.AddSingleton(EmailSenderMock);

            ConfigureTestServices(services);
        });
    }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    public virtual async Task InitializeAsync()
    {
        await _container.StartAsync();

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<LabViroMolIdentityDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<ResearchDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<SchedulingDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AssetsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<NotifyDbContext>().Database.MigrateAsync();

        _respawnConnection = new NpgsqlConnection(ConnectionString);
        await _respawnConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_respawnConnection, new RespawnerOptions
        {
            SchemasToInclude =
            [
                "identity", "inventory", "research", "scheduling", "assets", "notify",
            ],
            DbAdapter = DbAdapter.Postgres,
        });
    }

    public async Task ResetDatabaseAsync() => await _respawner.ResetAsync(_respawnConnection);

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _respawnConnection.DisposeAsync();
        await _container.DisposeAsync();
    }
}
