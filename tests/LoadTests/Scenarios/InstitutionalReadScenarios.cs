using LabViroMol.LoadTests.Infrastructure;
using NBomber.Contracts;
using NBomber.CSharp;

namespace LabViroMol.LoadTests.Scenarios;

public static class InstitutionalReadScenarios
{
    public static IReadOnlyList<ScenarioProps> Create(
        LoadTestRuntime runtime,
        int? closedCopiesOverride = null,
        int? openRateOverride = null)
    {
        var scenario = Scenario.Create("institutional_public_read", async context =>
            {
                var target = CreateTarget(runtime);
                var step = await Step.Run(target.Operation, context, async () =>
                {
                    var request = runtime.CreateRequest(HttpMethod.Get, target.Route, authenticated: false);
                    return await runtime.SendAsync(
                        target.Operation,
                        request,
                        TimeSpan.FromMilliseconds(800),
                        OperationGroups.Institutional);
                });

                await runtime.ApplyThinkTimeAsync();
                return step;
            })
            .WithWarmUpDuration(runtime.WarmUpDuration())
            .WithLoadSimulations(runtime.CreateLoadSimulations(
                openModel: IsOpenProfile(runtime.Options.Profile),
                closedCopiesOverride,
                openRateOverride))
            .WithThresholds(
                Threshold.Create(stats => stats.Fail.Request.Percent < 1.0),
                Threshold.Create(stats => stats.Ok.Latency.Percent95 <= 800),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 <= 2_000));

        return [scenario];
    }

    internal static InstitutionalTarget CreateTarget(LoadTestRuntime runtime)
    {
        return Random.Shared.Next(0, 100) switch
        {
            < 22 => Paged("public_equipments_read", "/api/assets/public/equipments", runtime),
            < 36 => new InstitutionalTarget("public_schedulable_equipments_read", $"/api/assets/public/equipments/schedulable?language={RandomLanguage()}"),
            < 58 => Paged("public_projects_read", "/api/research/public/projects", runtime),
            < 76 => Paged("public_publications_read", "/api/research/public/publications", runtime),
            < 88 => Paged("public_partners_read", "/api/research/public/partners", runtime, includeLanguage: false),
            _ => Paged("public_researchers_read", "/api/research/public/researchers", runtime)
        };
    }

    private static InstitutionalTarget Paged(
        string operation,
        string route,
        LoadTestRuntime runtime,
        bool includeLanguage = true)
    {
        var language = includeLanguage ? $"&language={RandomLanguage()}" : string.Empty;
        return new InstitutionalTarget(
            operation,
            $"{route}?pageNumber={runtime.RandomPageNumber()}&pageSize={runtime.RandomPageSize()}{language}");
    }

    private static string RandomLanguage() =>
        Random.Shared.Next(0, 3) switch
        {
            0 => "pt",
            1 => "en",
            _ => "es"
        };

    private static bool IsOpenProfile(string profile) =>
        profile is "stress" or "spike" or "breakpoint";
}

public sealed record InstitutionalTarget(string Operation, string Route);
