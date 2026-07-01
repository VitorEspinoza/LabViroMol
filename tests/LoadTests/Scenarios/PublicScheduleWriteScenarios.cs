using LabViroMol.LoadTests.Infrastructure;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace LabViroMol.LoadTests.Scenarios;

public static class PublicScheduleWriteScenarios
{
    public static IReadOnlyList<ScenarioProps> Create(LoadTestRuntime runtime)
    {
        var scenario = Scenario.Create("public_schedule_create", async context =>
            {
                var equipmentId = runtime.RandomEquipmentId();
                var date = DateOnly.FromDateTime(DateTime.Today.AddDays(Random.Shared.Next(2, 20)));
                var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(Random.Shared.Next(8, 16), 0)), TimeSpan.Zero);
                var end = start.AddHours(1);

                var request = runtime.CreateRequest(HttpMethod.Post, "/api/scheduling/public/schedules", authenticated: false)
                    .WithJsonBody(new CreateScheduleCommand(
                        new SchedulerInput("Usuario Institucional", "Biomedicina", $"public-{Guid.NewGuid():N}@test.local"),
                        new SchedulingInput(date, start, end),
                        true,
                        "Prof. Institucional",
                        "Teste leve de agendamento publico",
                        "Validacao de fluxo publico fora do mix de capacidade",
                        [new ScheduleEquipmentInput(equipmentId, $"Equip-{equipmentId:N}")]));

                return await runtime.SendAsync(
                    "public_schedule_create",
                    request,
                    TimeSpan.FromMilliseconds(1_500),
                    OperationGroups.PublicWrite);
            })
            .WithoutWarmUp()
            .WithLoadSimulations(Simulation.Inject(rate: 1, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(5)))
            .WithThresholds(
                Threshold.Create(stats => stats.Fail.Request.Percent < 1.0),
                Threshold.Create(stats => stats.Ok.Latency.Percent95 <= 1_500),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 <= 4_000));

        return [scenario];
    }
}
