using LabViroMol.LoadTests.Infrastructure;
using NBomber.Contracts;
using NBomber.CSharp;

namespace LabViroMol.LoadTests.Scenarios;

public static class MixedWorkloadScenario
{
    public static IReadOnlyList<ScenarioProps> Create(LoadTestRuntime runtime)
    {
        var scenarios = new List<ScenarioProps>();
        scenarios.AddRange(ReadSimpleScenarios.Create(runtime).Select(x => x.WithWeight(18)));
        scenarios.AddRange(ReadComplexScenarios.Create(runtime).Select(x => x.WithWeight(5)));
        scenarios.AddRange(WriteScenarios.Create(runtime).Select(x => x.WithWeight(7)));
        return scenarios;
    }
}
