using System.Data.Common;
using LabViroMol.Modules.Assets.Infrastructure.Persistence;
using LabViroMol.Modules.Identity.Infrastructure.Persistence;
using LabViroMol.Modules.Inventory.Infrastructure.Persistence;
using LabViroMol.Modules.Notify.Infrastructure.Persistence;
using LabViroMol.Modules.Research.Infrastructure.Persistence;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace LabViroMol.IntegrationTests.Shared;

public class LabViroMolWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string[] Schemas =
    [
        "public",
        "identity",
        "inventory",
        "research",
        "scheduling",
        "assets",
        "notify"
    ];

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("labviromol_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), "labviromol-tests", Guid.NewGuid().ToString("N"));
    private DbConnection? _resetConnection;
    private Respawner? _respawner;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:LabViroMol"] = _database.GetConnectionString(),
                ["Storage:RootFolder"] = _storageRoot,
                ["Jwt:Key"] = "dev-secret-key-replace-in-production-must-be-at-least-32-chars",
                ["Jwt:Issuer"] = "LabViroMol",
                ["Jwt:Audience"] = "LabViroMol",
                ["Outbox:PollingIntervalSeconds"] = "3600",
                ["Translation:IntervalMinutes"] = "3600"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            ConfigureTestServices(services);
        });
    }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null || _resetConnection is null)
            throw new InvalidOperationException("A infraestrutura de reset do banco ainda não foi inicializada.");

        await _respawner.ResetAsync(_resetConnection);
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        Directory.CreateDirectory(_storageRoot);

        await _database.StartAsync();

        _ = Services;

        await MigrateDatabasesAsync();
        await InitializeRespawnerAsync();
        await ResetDatabaseAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_resetConnection is not null)
            await _resetConnection.DisposeAsync();

        await _database.DisposeAsync();

        if (Directory.Exists(_storageRoot))
            Directory.Delete(_storageRoot, recursive: true);

        await base.DisposeAsync();
    }

    private async Task MigrateDatabasesAsync()
    {
        using var scope = Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        await serviceProvider.GetRequiredService<LabViroMolIdentityDbContext>().Database.MigrateAsync();
        await serviceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
        await serviceProvider.GetRequiredService<ResearchDbContext>().Database.MigrateAsync();
        await serviceProvider.GetRequiredService<SchedulingDbContext>().Database.MigrateAsync();
        await serviceProvider.GetRequiredService<AssetsDbContext>().Database.MigrateAsync();
        await serviceProvider.GetRequiredService<NotifyDbContext>().Database.MigrateAsync();
    }

    private async Task InitializeRespawnerAsync()
    {
        _resetConnection = new NpgsqlConnection(_database.GetConnectionString());
        await _resetConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_resetConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = Schemas,
            TablesToIgnore =
            [
                "__IdentityMigrationsHistory",
                "__InventoryMigrationsHistory",
                "__ResearchMigrationsHistory",
                "__SchedulingMigrationsHistory",
                "__AssetsMigrationsHistory",
                "__NotifyMigrationsHistory"
            ]
        });
    }
}
