using System.Net;
using LabViroMol.LoadTests.Data;
using LabViroMol.LoadTests.Infrastructure;
using LabViroMol.Modules.Inventory.Presentation.Materials;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Presentation.Projects;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;

namespace LabViroMol.LoadTests.Scenarios;

public static class MixedPublicAdminScenario
{
    public static IReadOnlyList<ScenarioProps> Create(LoadTestRuntime runtime)
    {
        var profile = runtime.Config.GetProfile(runtime.Options.Profile);
        var fallbackInstitutionalCopies = Math.Max(1, (int)Math.Round(profile.ClosedCopies * 0.8));
        var institutionalCopies = profile.InstitutionalClosedCopies ?? fallbackInstitutionalCopies;
        var adminCopies = profile.AdminClosedCopies ?? Math.Max(1, profile.ClosedCopies - institutionalCopies);
        var institutionalOpenRate = profile.InstitutionalOpenRate ?? Math.Max(1, (int)Math.Round(profile.OpenRate * 0.8));
        var adminOpenRate = profile.AdminOpenRate ?? Math.Max(1, profile.OpenRate - institutionalOpenRate);

        return
        [
            CreateInstitutionalFlow(runtime, institutionalCopies, institutionalOpenRate),
            CreateAdminFlow(runtime, adminCopies, adminOpenRate)
        ];
    }

    private static ScenarioProps CreateInstitutionalFlow(LoadTestRuntime runtime, int closedCopies, int openRate)
    {
        return Scenario.Create("institutional_user_flow", async context =>
            {
                var target = InstitutionalReadScenarios.CreateTarget(runtime);
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
                closedCopiesOverride: closedCopies,
                openRateOverride: openRate))
            .WithThresholds(
                Threshold.Create(stats => stats.Fail.Request.Percent < 1.0),
                Threshold.Create(stats => stats.Ok.Latency.Percent95 <= 800),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 <= 2_000));
    }

    private static ScenarioProps CreateAdminFlow(LoadTestRuntime runtime, int closedCopies, int openRate)
    {
        return Scenario.Create("admin_user_flow", async context =>
            {
                var action = SelectAdminAction(runtime);
                var step = await Step.Run(action.Name, context, async () =>
                {
                    return await action.Execute();
                });

                await runtime.ApplyThinkTimeAsync();
                return step;
            })
            .WithWarmUpDuration(runtime.WarmUpDuration())
            .WithLoadSimulations(runtime.CreateLoadSimulations(
                openModel: IsOpenProfile(runtime.Options.Profile),
                closedCopiesOverride: closedCopies,
                openRateOverride: openRate))
            .WithThresholds(
                Threshold.Create(stats => stats.Fail.Request.Percent < 1.0),
                Threshold.Create(stats => stats.Ok.Latency.Percent95 <= 1_500),
                Threshold.Create(stats => stats.Ok.Latency.Percent99 <= 4_000));
    }

    private static AdminAction SelectAdminAction(LoadTestRuntime runtime)
    {
        return Random.Shared.Next(0, 100) switch
        {
            < 65 => SelectAdminReadAction(runtime),
            < 80 => new AdminAction("admin_dashboard_summary_read", () => SendDashboardAsync(runtime)),
            < 90 => new AdminAction("create_material_write", () => SendCreateMaterialAsync(runtime)),
            < 96 => new AdminAction("approve_schedule_write", () => SendApproveUntilAcceptedAsync(runtime, CancellationToken.None)),
            _ => new AdminAction("project_member_role_write", () => SendProjectMemberRoleFlowAsync(runtime, CancellationToken.None))
        };
    }

    private static AdminAction SelectAdminReadAction(LoadTestRuntime runtime)
    {
        var target = Random.Shared.Next(0, 4) switch
        {
            0 => ("inventory_materials_read", "/api/inventory/materials"),
            1 => ("assets_equipments_read", "/api/assets/equipments"),
            2 => ("scheduling_schedules_read", "/api/scheduling/schedules"),
            _ => ("research_projects_read", "/api/research/projects")
        };

        return new AdminAction(target.Item1, async () =>
        {
            var request = runtime.CreateRequest(
                HttpMethod.Get,
                $"{target.Item2}?pageNumber={runtime.RandomPageNumber()}&pageSize={runtime.RandomPageSize()}");

            return await runtime.SendAsync(target.Item1, request, TimeSpan.FromMilliseconds(500), OperationGroups.Admin);
        });
    }

    private static async Task<Response<HttpResponseMessage>> SendDashboardAsync(LoadTestRuntime runtime)
    {
        var request = runtime.CreateRequest(HttpMethod.Get, "/api/admin/dashboard/summary");
        return await runtime.SendAsync("admin_dashboard_summary_read", request, TimeSpan.FromSeconds(2), OperationGroups.Dashboard);
    }

    private static async Task<Response<HttpResponseMessage>> SendCreateMaterialAsync(LoadTestRuntime runtime)
    {
        var request = runtime.CreateRequest(HttpMethod.Post, "/api/inventory/materials")
            .WithJsonBody(new CreateMaterialRequest(
                Name: $"Material-{Guid.NewGuid():N}",
                Location: $"Sala-{Random.Shared.Next(1, 20)}",
                MinStock: Random.Shared.Next(10, 200),
                StockQuantity: Random.Shared.Next(100, 500),
                Unit: LabViroMol.Modules.Inventory.Domain.Materials.Unit.Gram,
                TypeId: runtime.RandomMaterialTypeId()));

        return await runtime.SendAsync("create_material_write", request, TimeSpan.FromSeconds(1.5), OperationGroups.Admin);
    }

    private static async Task<Response<HttpResponseMessage>> SendApproveUntilAcceptedAsync(
        LoadTestRuntime runtime,
        CancellationToken ct)
    {
        const int maxAttempts = 10;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var scheduleId = await runtime.NextPendingScheduleIdAsync(ct);
            var request = runtime.CreateRequest(HttpMethod.Post, $"/api/scheduling/schedules/{scheduleId}/approve");
            var response = await runtime.SendAsync("approve_schedule_write", request, TimeSpan.FromSeconds(1.5), OperationGroups.Admin);

            if (!HasStatusCode(response, HttpStatusCode.UnprocessableEntity))
                return response;
        }

        var fallbackScheduleId = await runtime.NextPendingScheduleIdAsync(ct);
        var fallbackRequest = runtime.CreateRequest(HttpMethod.Post, $"/api/scheduling/schedules/{fallbackScheduleId}/approve");
        return await runtime.SendAsync("approve_schedule_write", fallbackRequest, TimeSpan.FromSeconds(1.5), OperationGroups.Admin);
    }

    private static async Task<Response<HttpResponseMessage>> SendProjectMemberRoleFlowAsync(
        LoadTestRuntime runtime,
        CancellationToken ct)
    {
        var addResult = await SendAddMemberUntilAcceptedAsync(runtime, ct);
        if (addResult.Response.IsError)
            return addResult.Response;

        var request = runtime.CreateRequest(HttpMethod.Put, $"/api/research/projects/{addResult.Target.ProjectId}/members/{addResult.Target.ResearcherId}/role")
            .WithJsonBody(new ChangeMemberRoleRequest(ProjectRole.Manager.ToString(), addResult.Target.LeadResearcherId));

        return await runtime.SendAsync("project_member_role_write:change", request, TimeSpan.FromSeconds(1.5), OperationGroups.Admin);
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

            var response = await runtime.SendAsync("project_member_role_write:add", request, TimeSpan.FromSeconds(1.5), OperationGroups.Admin);
            if (!HasStatusCode(response, HttpStatusCode.Conflict))
                return (target, response);
        }

        var fallbackTarget = runtime.NextProjectMemberTarget();
        var fallbackRequest = runtime.CreateRequest(HttpMethod.Post, $"/api/research/projects/{fallbackTarget.ProjectId}/members")
            .WithJsonBody(new AddProjectMemberRequest(fallbackTarget.ResearcherId, ProjectRole.Collaborator.ToString(), fallbackTarget.LeadResearcherId));

        var fallbackResponse = await runtime.SendAsync("project_member_role_write:add", fallbackRequest, TimeSpan.FromSeconds(1.5), OperationGroups.Admin);
        return (fallbackTarget, fallbackResponse);
    }

    private static bool HasStatusCode(Response<HttpResponseMessage> response, HttpStatusCode statusCode) =>
        response.Payload.IsSome() && response.Payload.Value.StatusCode == statusCode;

    private static bool IsOpenProfile(string profile) =>
        profile is "stress" or "spike" or "breakpoint";

    private sealed record AdminAction(
        string Name,
        Func<Task<Response<HttpResponseMessage>>> Execute);
}
