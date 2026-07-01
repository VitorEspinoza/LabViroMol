using LabViroMol.LoadTests.Data;
using LabViroMol.LoadTests.Infrastructure;
using LabViroMol.Modules.Inventory.Presentation.Materials;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Presentation.Projects;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Net;

namespace LabViroMol.LoadTests.Scenarios;

public static class WriteScenarios
{
    public static IReadOnlyList<ScenarioProps> Create(
        LoadTestRuntime runtime,
        int? closedCopiesOverride = null,
        int? openRateOverride = null)
    {
        return
        [
            CreateApproveSchedule(runtime, closedCopiesOverride, openRateOverride),
            CreateProjectMemberAndRoleChange(runtime, closedCopiesOverride, openRateOverride),
            CreateMaterial(runtime, closedCopiesOverride, openRateOverride)
        ];
    }

    private static ScenarioProps CreateApproveSchedule(
        LoadTestRuntime runtime,
        int? closedCopiesOverride,
        int? openRateOverride)
    {
        return Scenario.Create("approve_schedule_write", async context =>
            {
                var step = await Step.Run("approve", context, async () =>
                {
                    return await SendApproveUntilAcceptedAsync(runtime, CancellationToken.None);
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
                Threshold.Create(stats => stats.Ok.Latency.Percent95 <= 200),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 <= 800));
    }

    private static ScenarioProps CreateProjectMemberAndRoleChange(
        LoadTestRuntime runtime,
        int? closedCopiesOverride,
        int? openRateOverride)
    {
        return Scenario.Create("project_member_role_write", async context =>
            {
                ProjectMemberWriteTarget? selectedTarget = null;
                var addStep = await Step.Run("add_member", context, async () =>
                {
                    var result = await SendAddMemberUntilAcceptedAsync(runtime, CancellationToken.None);
                    selectedTarget = result.Target;
                    return result.Response;
                });

                if (addStep.IsError)
                    return addStep;

                var changeStep = await Step.Run("change_member_role", context, async () =>
                {
                    var target = selectedTarget ?? throw new InvalidOperationException("Project member target nao foi selecionado para change_member_role.");
                    var request = runtime.CreateRequest(HttpMethod.Put, $"/api/research/projects/{target.ProjectId}/members/{target.ResearcherId}/role")
                        .WithJsonBody(new ChangeMemberRoleRequest(ProjectRole.Manager.ToString(), target.LeadResearcherId));
                    return await runtime.SendAsync("project_member_role_write:change", request, TimeSpan.FromMilliseconds(200));
                });

                return changeStep;
            })
            .WithWarmUpDuration(runtime.WarmUpDuration())
            .WithLoadSimulations(runtime.CreateLoadSimulations(
                openModel: IsOpenProfile(runtime.Options.Profile),
                closedCopiesOverride,
                openRateOverride))
            .WithThresholds(
                Threshold.Create(stats => stats.Fail.Request.Percent < 0.1),
                Threshold.Create(stats => stats.Ok.Latency.Percent95 <= 200),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 <= 800));
    }

    private static ScenarioProps CreateMaterial(
        LoadTestRuntime runtime,
        int? closedCopiesOverride,
        int? openRateOverride)
    {
        return Scenario.Create("create_material_write", async context =>
            {
                var step = await Step.Run("create_material", context, async () =>
                {
                    var request = runtime.CreateRequest(HttpMethod.Post, "/api/inventory/materials")
                        .WithJsonBody(new CreateMaterialRequest(
                            Name: $"Material-{Guid.NewGuid():N}",
                            Location: $"Sala-{Random.Shared.Next(1, 20)}",
                            MinStock: Random.Shared.Next(10, 200),
                            StockQuantity: Random.Shared.Next(100, 500),
                            Unit: LabViroMol.Modules.Inventory.Domain.Materials.Unit.Gram,
                            TypeId: runtime.RandomMaterialTypeId()));
                    return await runtime.SendAsync("create_material_write", request, TimeSpan.FromMilliseconds(200));
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
                Threshold.Create(stats => stats.Ok.Latency.Percent95 <= 200),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 <= 800));
    }

    private static bool IsOpenProfile(string profile) =>
        profile is "stress" or "spike" or "breakpoint";

    private static async Task<Response<HttpResponseMessage>> SendApproveUntilAcceptedAsync(
        LoadTestRuntime runtime,
        CancellationToken ct)
    {
        const int maxAttempts = 10;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var scheduleId = await runtime.NextPendingScheduleIdAsync(ct);
            var request = runtime.CreateRequest(HttpMethod.Post, $"/api/scheduling/schedules/{scheduleId}/approve");
            var response = await runtime.SendAsync("approve_schedule_write", request, TimeSpan.FromMilliseconds(200));

            if (!HasStatusCode(response, HttpStatusCode.UnprocessableEntity))
                return response;
        }

        var fallbackScheduleId = await runtime.NextPendingScheduleIdAsync(ct);
        var fallbackRequest = runtime.CreateRequest(HttpMethod.Post, $"/api/scheduling/schedules/{fallbackScheduleId}/approve");
        return await runtime.SendAsync("approve_schedule_write", fallbackRequest, TimeSpan.FromMilliseconds(200));
    }

    private static async Task<(ProjectMemberWriteTarget Target, Response<HttpResponseMessage> Response)> SendAddMemberUntilAcceptedAsync(
        LoadTestRuntime runtime,
        CancellationToken ct)
    {
        const int maxAttempts = 10;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var target = runtime.NextProjectMemberTarget();
            var request = runtime.CreateRequest(HttpMethod.Post, $"/api/research/projects/{target.ProjectId}/members")
                .WithJsonBody(new AddProjectMemberRequest(target.ResearcherId, ProjectRole.Collaborator.ToString(), target.LeadResearcherId));

            var response = await runtime.SendAsync("project_member_role_write:add", request, TimeSpan.FromMilliseconds(200));
            if (!HasStatusCode(response, HttpStatusCode.Conflict))
                return (target, response);
        }

        var fallbackTarget = runtime.NextProjectMemberTarget();
        var fallbackRequest = runtime.CreateRequest(HttpMethod.Post, $"/api/research/projects/{fallbackTarget.ProjectId}/members")
            .WithJsonBody(new AddProjectMemberRequest(fallbackTarget.ResearcherId, ProjectRole.Collaborator.ToString(), fallbackTarget.LeadResearcherId));

        var fallbackResponse = await runtime.SendAsync("project_member_role_write:add", fallbackRequest, TimeSpan.FromMilliseconds(200));
        return (fallbackTarget, fallbackResponse);
    }

    private static bool HasStatusCode(Response<HttpResponseMessage> response, HttpStatusCode statusCode) =>
        response.Payload.IsSome() && response.Payload.Value.StatusCode == statusCode;

}
