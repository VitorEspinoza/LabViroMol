using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Authorization;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using SchedulingPeriod = LabViroMol.Modules.Scheduling.Domain.Schedules.Scheduling;

namespace LabViroMol.Modules.AdminBff.IntegrationTests.Dashboard;

public class DashboardSummaryEndpointTests : BaseIntegrationTest
{
    private const string Route = "/api/admin/dashboard/summary";

    public DashboardSummaryEndpointTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn401_WhenUserIsNotAuthenticated()
    {
        ClearAuthentication();

        var response = await Client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn403_WhenUserHasNoDashboardSectionPermission()
    {
        AuthenticateAs([Permissions.Identity.UsersView]);

        var response = await Client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnOnlySchedulingSection_WhenUserHasOnlySchedulingPermission()
    {
        AuthenticateAs([Permissions.Scheduling.SchedulesView]);
        await SeedScheduledAsync(businessDaysFromToday: 1);
        await SeedPendingAsync(businessDaysFromToday: 2);

        var response = await Client.GetAsync(Route);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected OK, got {response.StatusCode}. Body: {body}");
        Assert.Equal(1, json.GetProperty("scheduling").GetProperty("pendingSchedulesCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("inventory").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("assets").ValueKind);
    }

    [Fact]
    public async Task ShouldReturnInventoryLowStockSummary_WhenUserHasInventoryManagePermission()
    {
        AuthenticateAs([Permissions.Inventory.MaterialsManage]);
        await SeedMaterialAsync(name: "Baixo", stock: 3m, minStock: 10m);
        await SeedMaterialAsync(name: "Normal", stock: 20m, minStock: 10m);

        var response = await Client.GetAsync(Route);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = JsonSerializer.Deserialize<JsonElement>(body);
        var inventory = json.GetProperty("inventory");

        Assert.Equal(1, inventory.GetProperty("lowStockMaterialsCount").GetInt32());
        Assert.Equal("Baixo", inventory.GetProperty("lowStockMaterials")[0].GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("scheduling").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("assets").ValueKind);
    }

    [Fact]
    public async Task ShouldReturnAssetsActiveMaintenanceSummary()
    {
        AuthenticateAs([Permissions.Assets.MaintenanceView]);
        var requested = await SeedMaintenanceRequestAsync();
        var inProgress = await SeedMaintenanceRequestAsync();
        var done = await SeedMaintenanceRequestAsync();
        var cancelled = await SeedMaintenanceRequestAsync();
        inProgress.Start();
        done.Start();
        done.Done();
        cancelled.Cancel();
        await AssetsDbContext.SaveChangesAsync();

        var response = await Client.GetAsync(Route);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var assets = json.GetProperty("assets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, assets.GetProperty("activeMaintenanceRequestsCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("scheduling").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("inventory").ValueKind);
        Assert.NotEqual(requested.Id, Guid.Empty);
    }

    [Fact]
    public async Task ShouldReturnSchedulingCountsAndUpcomingSchedules()
    {
        AuthenticateAs([Permissions.Scheduling.SchedulesView]);

        // Calcula as datas antes de sedar para derivar o expected count dinamicamente.
        // Se o teste rodar nos últimos dias úteis do mês, businessDaysFromToday: 3
        // pode cair no mês seguinte — approvedSchedulesThisMonthCount só conta o mês atual.
        var thisMonth = DateOnly.FromDateTime(DateTime.Today).Month;
        var expectedApprovedThisMonth = new[] { 1, 2, 3 }.Count(n => NextBusinessDate(n).Month == thisMonth);

        await SeedScheduledAsync(businessDaysFromToday: 1, schedulerName: "Primeiro");
        await SeedScheduledAsync(businessDaysFromToday: 3, schedulerName: "Terceiro");
        await SeedScheduledAsync(businessDaysFromToday: 2, schedulerName: "Segundo");
        await SeedPendingAsync(businessDaysFromToday: 1);

        var response = await Client.GetAsync(Route);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var scheduling = json.GetProperty("scheduling");
        var upcoming = scheduling.GetProperty("upcomingSchedules");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, scheduling.GetProperty("pendingSchedulesCount").GetInt32());
        Assert.Equal(expectedApprovedThisMonth, scheduling.GetProperty("approvedSchedulesThisMonthCount").GetInt32());
        Assert.Equal("Primeiro", upcoming[0].GetProperty("schedulerName").GetString());
        Assert.Equal("Segundo", upcoming[1].GetProperty("schedulerName").GetString());
        Assert.Equal("Terceiro", upcoming[2].GetProperty("schedulerName").GetString());
    }

    private async Task<Material> SeedMaterialAsync(string name, decimal stock, decimal minStock)
    {
        var type = MaterialType.Create($"Tipo {name}");
        await InventoryDbContext.MaterialTypes.AddAsync(type);

        var material = Material.Create(
            name,
            "Sala A",
            (Quantity)minStock,
            (Quantity)stock,
            Unit.Gram,
            type).Data!;

        await InventoryDbContext.Materials.AddAsync(material);
        await InventoryDbContext.SaveChangesAsync();

        return material;
    }

    private async Task<MaintenanceRequest> SeedMaintenanceRequestAsync()
    {
        var equipment = Equipment.Create(
            "Microscopio",
            "Marca",
            "Modelo",
            $"EQ-{Guid.NewGuid():N}",
            "Descricao").Data!;

        await AssetsDbContext.Equipments.AddAsync(equipment);

        var maintenanceRequest = MaintenanceRequest.Create(
            "Manutencao preventiva",
            "Ruido anormal",
            equipment.Id.Value).Data!;

        await AssetsDbContext.MaintenanceRequests.AddAsync(maintenanceRequest);
        await AssetsDbContext.SaveChangesAsync();

        return maintenanceRequest;
    }

    private Task<Schedule> SeedPendingAsync(int businessDaysFromToday)
        => SeedScheduleAsync(businessDaysFromToday);

    private async Task<Schedule> SeedScheduledAsync(int businessDaysFromToday, string schedulerName = "Maria")
    {
        var schedule = await SeedScheduleAsync(businessDaysFromToday, schedulerName);
        schedule.Approve(UserId.From(Guid.NewGuid()));
        await SchedulingDbContext.SaveChangesAsync();
        return schedule;
    }

    private async Task<Schedule> SeedScheduleAsync(
        int businessDaysFromToday,
        string schedulerName = "Maria")
    {
        var date = NextBusinessDate(businessDaysFromToday);
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
        var end = new DateTimeOffset(date.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero);
        var scheduling = SchedulingPeriod.Create(date, start, end).Data!;

        var schedule = Schedule.Create(
            new Scheduler(schedulerName, "Biomedicina", $"{schedulerName.ToLowerInvariant()}@test.com"),
            scheduling,
            acceptTerm: true,
            advisorProfessor: "Prof. Joao",
            projectTitle: "Projeto",
            description: "Descricao",
            equipments: [new ScheduleEquipment(Guid.NewGuid(), "Microscopio")]).Data!;

        await SchedulingDbContext.Schedules.AddAsync(schedule);
        await SchedulingDbContext.SaveChangesAsync();

        return schedule;
    }

    private static DateOnly NextBusinessDate(int businessDaysFromToday)
    {
        var date = DateOnly.FromDateTime(DateTime.Today);
        var remaining = businessDaysFromToday;

        while (remaining > 0)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            remaining--;
        }

        return date;
    }
}
