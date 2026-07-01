using LabViroMol.LoadTests.Infrastructure;
using NBomber.Contracts;
using NBomber.CSharp;

namespace LabViroMol.LoadTests.Scenarios;

public static class ReadSimpleScenarios
{
    public static IReadOnlyList<ScenarioProps> Create(
        LoadTestRuntime runtime,
        int? closedCopiesOverride = null,
        int? openRateOverride = null)
    {
        return
        [
            CreatePagedGet(runtime, "inventory_materials_read", "/api/inventory/materials", closedCopiesOverride, openRateOverride),
            CreatePagedGet(runtime, "assets_equipments_read", "/api/assets/equipments", closedCopiesOverride, openRateOverride),
            CreatePagedGet(runtime, "scheduling_schedules_read", "/api/scheduling/schedules", closedCopiesOverride, openRateOverride),
            CreatePagedGet(runtime, "research_projects_read", "/api/research/projects", closedCopiesOverride, openRateOverride)
        ];
    }

    private static ScenarioProps CreatePagedGet(
        LoadTestRuntime runtime,
        string name,
        string baseRoute,
        int? closedCopiesOverride,
        int? openRateOverride)
    {
        var scenario = Scenario.Create(name, async context =>
        {
            var step = await Step.Run("get", context, async () =>
            {
                var page = runtime.RandomPageNumber();
                var pageSize = runtime.RandomPageSize();
                var request = runtime.CreateRequest(HttpMethod.Get, $"{baseRoute}?pageNumber={page}&pageSize={pageSize}");
                return await runtime.SendAsync(name, request, TimeSpan.FromMilliseconds(50));
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
            Threshold.Create(stats => stats.Ok.Latency.Percent95 <= 2000),
            Threshold.Create(stats => stats.Ok.Latency.Percent99 <= 4000));

        return scenario;
    }

    private static bool IsOpenProfile(string profile) =>
        profile is "stress" or "spike" or "breakpoint";

}
