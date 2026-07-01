using Microsoft.Extensions.Configuration;

namespace LabViroMol.LoadTests.Infrastructure;

public sealed class LoadTestConfig
{
    public string? ConnectionString { get; init; }

    public string RequireConnectionString() =>
        ConnectionString ?? throw new InvalidOperationException(
            "ConnectionStrings:LabViroMol não configurada — necessária para --command=seed/reset.");
    public required string BaseUrl { get; init; }
    public required bool AllowInsecureTls { get; init; }
    public required bool KeepAliveEnabled { get; init; }
    public required int MaxConnectionsPerServer { get; init; }
    public required int RequestTimeoutSeconds { get; init; }
    public required int PooledConnectionLifetimeMinutes { get; init; }
    public required AuthSettings Auth { get; init; }

    public required DataSettings Data { get; init; }
    public required IReadOnlyDictionary<string, ProfileSettings> Profiles { get; init; }

    public ProfileSettings GetProfile(string profile) =>
        Profiles.TryGetValue(profile, out var settings)
            ? settings
            : throw new InvalidOperationException($"Perfil de carga '{profile}' não foi encontrado.");

    public ThresholdSettings? GetThresholds(string profile) =>
        GetProfile(profile).Thresholds;

    public static LoadTestConfig Load(IConfiguration configuration, CommandLineOptions options)
    {
        var section = configuration.GetSection("LoadTests");

        var globalData = section.GetSection("Data").Get<DataSettings>() ?? throw new InvalidOperationException("LoadTests:Data não configurado.");
        var profiles = section.GetSection("Profiles").Get<Dictionary<string, ProfileSettings>>() ?? throw new InvalidOperationException("LoadTests:Profiles não configurado.");

        var effectiveData = profiles.TryGetValue(options.Profile, out var activeProfile) && activeProfile.Data is not null
            ? activeProfile.Data
            : globalData;

        var config = new LoadTestConfig
        {
            ConnectionString = configuration.GetConnectionString("LabViroMol"),
            BaseUrl = options.BaseUrlOverride ?? section["BaseUrl"] ?? throw new InvalidOperationException("LoadTests:BaseUrl não configurado."),
            AllowInsecureTls = section.GetValue("AllowInsecureTls", true),
            KeepAliveEnabled = options.KeepAliveEnabled,
            MaxConnectionsPerServer = section.GetValue("MaxConnectionsPerServer", 1024),
            RequestTimeoutSeconds = section.GetValue("RequestTimeoutSeconds", 30),
            PooledConnectionLifetimeMinutes = section.GetValue("PooledConnectionLifetimeMinutes", 30),
            Auth = section.GetSection("Auth").Get<AuthSettings>() ?? throw new InvalidOperationException("LoadTests:Auth não configurado."),
            Data = effectiveData,
            Profiles = profiles
        };

        return config;
    }
}

public sealed class AuthSettings
{
    public required string EmailPrefix { get; init; }
    public required string Password { get; init; }
    public required int UserCount { get; init; }
    public required string RoleName { get; init; }
}

public sealed class DataSettings
{
    public required string SeedCatalogPath { get; init; }
    public required int Materials { get; init; }
    public required int Equipments { get; init; }
    public required int Projects { get; init; }
    public required int SchedulesPending { get; init; }
    public required int SchedulesApproved { get; init; }
    public required int Batches { get; init; }
}

public sealed class ProfileSettings
{
    public required int WarmUpSeconds { get; init; }
    public required int DurationSeconds { get; init; }
    public required int ClosedCopies { get; init; }
    public required int OpenRate { get; init; }
    public int? InstitutionalClosedCopies { get; init; }
    public int? AdminClosedCopies { get; init; }
    public int? InstitutionalOpenRate { get; init; }
    public int? AdminOpenRate { get; init; }
    public double? MinThinkTimeSeconds { get; init; }
    public double? MaxThinkTimeSeconds { get; init; }

    public DataSettings? Data { get; init; }

    public ThresholdSettings? Thresholds { get; init; }
}

public sealed class ThresholdSettings
{
    public required double P95Ms { get; init; }
    public required double P99Ms { get; init; }
    public required double MaxErrorRatePercent { get; init; }
}
