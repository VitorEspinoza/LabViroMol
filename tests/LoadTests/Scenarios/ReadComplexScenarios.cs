using LabViroMol.LoadTests.Infrastructure;
using NBomber.Contracts;
using NBomber.CSharp;

namespace LabViroMol.LoadTests.Scenarios;

public static class ReadComplexScenarios
{
    public static IReadOnlyList<ScenarioProps> Create(
        LoadTestRuntime runtime,
        int? closedCopiesOverride = null,
        int? openRateOverride = null)
    {
        return
        [
            CreateDashboardScenario(runtime, closedCopiesOverride, openRateOverride)
        ];
    }

    private static ScenarioProps CreateDashboardScenario(
        LoadTestRuntime runtime,
        int? closedCopiesOverride,
        int? openRateOverride)
    {
        return Scenario.Create("admin_dashboard_summary_read", async context =>
            {
                var step = await Step.Run("dashboard", context, async () =>
                {
                    var request = runtime.CreateRequest(HttpMethod.Get, "/api/admin/dashboard/summary");
                    return await runtime.SendAsync(
                        "admin_dashboard_summary_read",
                        request,
                        TimeSpan.FromMilliseconds(150),
                        OperationGroups.Dashboard);
                });

                return step;
            })
            .WithWarmUpDuration(runtime.WarmUpDuration())
            .WithLoadSimulations(runtime.CreateLoadSimulations(
                openModel: IsOpenProfile(runtime.Options.Profile),
                closedCopiesOverride,
                openRateOverride))
            .WithThresholds(
                Threshold.Create(stats => stats.Fail.Request.Percent < 0.1),
                Threshold.Create(stats => stats.Ok.Latency.Percent95 <= 150),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 <= 600));
    }

    private static bool IsOpenProfile(string profile) =>
        profile is "stress" or "spike" or "breakpoint";

}
