using Microsoft.Extensions.Configuration;
using Npgsql;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence;

public static class ConnectionStringResolver
{
    public static string ResolveLabViroMolConnectionString(this IConfiguration configuration)
    {
        var rawConnectionString = configuration.GetConnectionString("LabViroMol")
                                  ?? throw new InvalidOperationException("ConnectionStrings:LabViroMol não configurada.");

        var maxPoolSize = configuration.GetValue<int?>("LoadTest:NpgsqlMaxPoolSize");
        if (maxPoolSize is null or <= 0)
            return rawConnectionString;

        var builder = new NpgsqlConnectionStringBuilder(rawConnectionString)
        {
            MaxPoolSize = maxPoolSize.Value
        };

        return builder.ConnectionString;
    }
}
