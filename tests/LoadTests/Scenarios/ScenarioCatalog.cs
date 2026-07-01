using LabViroMol.LoadTests.Infrastructure;
using NBomber.Contracts;
using NBomber.CSharp;

namespace LabViroMol.LoadTests.Scenarios;

public static class ScenarioCatalog
{
    public static IReadOnlyList<ScenarioProps> Create(LoadTestRuntime runtime)
    {
        return runtime.Options.Scenario.ToLowerInvariant() switch
        {
            "read-simple" => ReadSimpleScenarios.Create(runtime),
            "read-complex" => ReadComplexScenarios.Create(runtime),
            "admin-dashboard" => ReadComplexScenarios.Create(runtime),
            "public-read" => InstitutionalReadScenarios.Create(runtime),
            "public-schedule-write" => PublicScheduleWriteScenarios.Create(runtime),
            "write" => WriteScenarios.Create(runtime),
            "mixed" => MixedWorkloadScenario.Create(runtime),
            "mixed-public-admin" => MixedPublicAdminScenario.Create(runtime),
            "resilience" => ResilienceScenarios.Create(runtime),
            _ => [.. ReadSimpleScenarios.Create(runtime), .. InstitutionalReadScenarios.Create(runtime), .. ReadComplexScenarios.Create(runtime), .. WriteScenarios.Create(runtime)]
        };
    }
}
