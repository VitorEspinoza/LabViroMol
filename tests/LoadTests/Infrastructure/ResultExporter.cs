using System.Text.Json;

namespace LabViroMol.LoadTests.Infrastructure;

public static class ResultExporter
{
    public static void WriteSummary(string reportFolder, LoadTestRuntime runtime, object result)
    {
        var scenarioStats = result.GetType().GetProperty("ScenarioStats")?.GetValue(result) as System.Collections.IEnumerable;
        var profile = runtime.Config.GetProfile(runtime.Options.Profile);
        var statusBreakdown = runtime.GetStatusBreakdown();
        var operationGroups = runtime.GetOperationGroups();
        var observedWindowSeconds = runtime.GetObservedWindowSeconds();
        var effectiveLoad = ResolveEffectiveLoad(runtime.Options.Scenario, profile);

        var scenarios = new List<object>();
        var steps = new List<object>();
        if (scenarioStats is not null)
        {
            foreach (var scenario in scenarioStats)
            {
                var scenarioType = scenario!.GetType();
                var ok = scenarioType.GetProperty("Ok")?.GetValue(scenario);
                var fail = scenarioType.GetProperty("Fail")?.GetValue(scenario);
                var latency = ok?.GetType().GetProperty("Latency")?.GetValue(ok);

                scenarios.Add(new
                {
                    ScenarioName = scenarioType.GetProperty("ScenarioName")?.GetValue(scenario)?.ToString(),
                    OkPercent = ok?.GetType().GetProperty("Request")?.GetValue(ok)?.GetType().GetProperty("Percent")?.GetValue(ok?.GetType().GetProperty("Request")?.GetValue(ok)!),
                    FailPercent = fail?.GetType().GetProperty("Request")?.GetValue(fail)?.GetType().GetProperty("Percent")?.GetValue(fail?.GetType().GetProperty("Request")?.GetValue(fail)!),
                    P95Ms = latency?.GetType().GetProperty("Percent95")?.GetValue(latency),
                    P99Ms = latency?.GetType().GetProperty("Percent99")?.GetValue(latency),
                    MaxMs = latency?.GetType().GetProperty("MaxMs")?.GetValue(latency)
                });

                var stepStats = scenarioType.GetProperty("StepStats")?.GetValue(scenario) as System.Collections.IEnumerable
                    ?? scenarioType.GetProperty("Steps")?.GetValue(scenario) as System.Collections.IEnumerable;

                if (stepStats is null)
                    continue;

                foreach (var step in stepStats)
                {
                    var stepType = step!.GetType();
                    var stepOk = stepType.GetProperty("Ok")?.GetValue(step);
                    var stepFail = stepType.GetProperty("Fail")?.GetValue(step);
                    var stepLatency = stepOk?.GetType().GetProperty("Latency")?.GetValue(stepOk);
                    var stepName = stepType.GetProperty("StepName")?.GetValue(step)?.ToString()
                        ?? stepType.GetProperty("Name")?.GetValue(step)?.ToString();

                    steps.Add(new
                    {
                        ScenarioName = scenarioType.GetProperty("ScenarioName")?.GetValue(scenario)?.ToString(),
                        StepName = stepName,
                        OkPercent = ReadRequestMetric(stepOk, "Percent"),
                        FailPercent = ReadRequestMetric(stepFail, "Percent"),
                        RequestCount = ReadRequestMetric(stepOk, "Count"),
                        Rps = ReadRequestMetric(stepOk, "RPS"),
                        P95Ms = stepLatency?.GetType().GetProperty("Percent95")?.GetValue(stepLatency),
                        P99Ms = stepLatency?.GetType().GetProperty("Percent99")?.GetValue(stepLatency),
                        MaxMs = stepLatency?.GetType().GetProperty("MaxMs")?.GetValue(stepLatency)
                    });
                }
            }
        }

        var operationSummaries = statusBreakdown
            .Select(kvp =>
            {
                var totalRequests = kvp.Value.Values.Sum();
                var group = operationGroups.TryGetValue(kvp.Key, out var operationGroup)
                    ? operationGroup
                    : OperationGroups.Admin;

                return new
                {
                    Operation = kvp.Key,
                    Group = group,
                    TotalRequests = totalRequests,
                    ApproxRps = CalculateRps(totalRequests, observedWindowSeconds),
                    StatusCodes = kvp.Value
                };
            })
            .OrderBy(x => x.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Operation, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var groupSummaries = operationSummaries
            .GroupBy(x => x.Group, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var totalRequests = group.Sum(x => x.TotalRequests);
                return new
                {
                    Group = group.Key,
                    TotalRequests = totalRequests,
                    ApproxRps = CalculateRps(totalRequests, observedWindowSeconds)
                };
            })
            .OrderBy(x => x.Group, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalRequests = operationSummaries.Sum(x => x.TotalRequests);

        var payload = new
        {
            runtime.Options.Campaign,
            runtime.Options.Profile,
            runtime.Options.Scenario,
            Load = new
            {
                profile.WarmUpSeconds,
                profile.DurationSeconds,
                profile.ClosedCopies,
                profile.OpenRate,
                profile.InstitutionalClosedCopies,
                profile.AdminClosedCopies,
                profile.InstitutionalOpenRate,
                profile.AdminOpenRate,
                effectiveLoad.EffectiveInstitutionalClosedCopies,
                effectiveLoad.EffectiveAdminClosedCopies,
                effectiveLoad.EffectiveInstitutionalOpenRate,
                effectiveLoad.EffectiveAdminOpenRate,
                profile.MinThinkTimeSeconds,
                profile.MaxThinkTimeSeconds,
                ObservedWindowSeconds = Math.Round(observedWindowSeconds, 2),
                TotalRequests = totalRequests,
                ApproxRps = CalculateRps(totalRequests, observedWindowSeconds)
            },
            StatusBreakdown = statusBreakdown,
            Operations = operationSummaries,
            Groups = groupSummaries,
            Apdex = runtime.GetApdexSummary(),
            Scenarios = scenarios,
            Steps = steps
        };

        File.WriteAllText(
            Path.Combine(reportFolder, "summary.json"),
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static double CalculateRps(long totalRequests, double observedWindowSeconds) =>
        observedWindowSeconds <= 0
            ? 0
            : Math.Round(totalRequests / observedWindowSeconds, 2);

    private static object? ReadRequestMetric(object? stats, string propertyName)
    {
        var request = stats?.GetType().GetProperty("Request")?.GetValue(stats);
        return request?.GetType().GetProperty(propertyName)?.GetValue(request);
    }

    private static EffectiveLoad ResolveEffectiveLoad(string scenario, ProfileSettings profile)
    {
        if (scenario.Equals("public-read", StringComparison.OrdinalIgnoreCase))
        {
            return new EffectiveLoad(profile.ClosedCopies, 0, profile.OpenRate, 0);
        }

        if (scenario.Equals("mixed-public-admin", StringComparison.OrdinalIgnoreCase))
        {
            var institutionalCopies = profile.InstitutionalClosedCopies
                ?? Math.Max(1, (int)Math.Round(profile.ClosedCopies * 0.8));
            var adminCopies = profile.AdminClosedCopies
                ?? Math.Max(1, profile.ClosedCopies - institutionalCopies);
            var institutionalOpenRate = profile.InstitutionalOpenRate
                ?? Math.Max(1, (int)Math.Round(profile.OpenRate * 0.8));
            var adminOpenRate = profile.AdminOpenRate
                ?? Math.Max(1, profile.OpenRate - institutionalOpenRate);

            return new EffectiveLoad(institutionalCopies, adminCopies, institutionalOpenRate, adminOpenRate);
        }

        return new EffectiveLoad(0, profile.ClosedCopies, 0, profile.OpenRate);
    }

    private sealed record EffectiveLoad(
        int EffectiveInstitutionalClosedCopies,
        int EffectiveAdminClosedCopies,
        int EffectiveInstitutionalOpenRate,
        int EffectiveAdminOpenRate);
}
