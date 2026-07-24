using LabViroMol.LoadTests.Infrastructure;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace LabViroMol.LoadTests.Scenarios;

public static class ResilienceScenarios
{
    public static IReadOnlyList<ScenarioProps> Create(LoadTestRuntime runtime)
    {
        var scenario = Scenario.Create("scheduling_rate_limit_resilience", async context =>
            {
                var equipmentId = runtime.RandomEquipmentId();
                var date = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
                var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
                var end = start.AddHours(1);

                var request = runtime.CreateRequest(HttpMethod.Post, "/api/scheduling/public/schedules", authenticated: false)
                    .WithJsonBody(new CreateScheduleCommand(
                        new SchedulerInput("Usuário Carga", "Biomedicina", $"resilience-{Guid.NewGuid():N}@test.local"),
                        new SchedulingInput(date, start, end),
                        true,
                        "Prof. Resiliência",
                        "Teste de burst do rate limiter",
                        "Validação do retorno 429",
                        [new ScheduleEquipmentInput(equipmentId, $"Equip-{equipmentId:N}")]));

                return await runtime.SendAsync("scheduling_rate_limit_resilience", request, TimeSpan.FromMilliseconds(200));
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(rate: 6, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(5)))
            .WithThresholds(
                Threshold.Create(stats => stats.Fail.StatusCodes.Exists("429")));

        return [scenario];
    }

}
