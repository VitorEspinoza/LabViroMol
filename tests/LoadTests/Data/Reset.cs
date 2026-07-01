using Npgsql;
using Respawn;

namespace LabViroMol.LoadTests.Data;

public static class Reset
{
    public static async Task ExecuteAsync(string connectionString, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);

        var respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude =
            [
                "identity",
                "inventory",
                "research",
                "scheduling",
                "assets",
                "notify"
            ]
        });

        await respawner.ResetAsync(connection);
    }
}
