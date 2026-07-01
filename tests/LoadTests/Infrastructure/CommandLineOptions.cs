namespace LabViroMol.LoadTests.Infrastructure;

public sealed record CommandLineOptions(
    string Command,
    string Scenario,
    string Profile,
    string Campaign,
    bool KeepAliveEnabled,
    bool ResetBeforeRun,
    string? BaseUrlOverride)
{
    public static CommandLineOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var arg in args)
        {
            if (!arg.StartsWith("--", StringComparison.Ordinal))
                continue;

            var parts = arg[2..].Split('=', 2, StringSplitOptions.TrimEntries);
            values[parts[0]] = parts.Length == 2 ? parts[1] : "true";
        }

        return new CommandLineOptions(
            Command: values.GetValueOrDefault("command", "run"),
            Scenario: values.GetValueOrDefault("scenario", "mixed"),
            Profile: values.GetValueOrDefault("profile", "load"),
            Campaign: values.GetValueOrDefault("campaign", "A"),
            KeepAliveEnabled: !string.Equals(values.GetValueOrDefault("keepAlive", "true"), "false", StringComparison.OrdinalIgnoreCase),
            ResetBeforeRun: string.Equals(values.GetValueOrDefault("resetBeforeRun", "false"), "true", StringComparison.OrdinalIgnoreCase),
            BaseUrlOverride: values.GetValueOrDefault("baseUrl"));
    }
}
