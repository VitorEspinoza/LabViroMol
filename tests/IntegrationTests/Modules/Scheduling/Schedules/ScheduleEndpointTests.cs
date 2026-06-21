using System.Net;
using System.Net.Http.Json;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.IntegrationTests;
using LabViroMol.Modules.Scheduling.Presentation.Schedules;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Scheduling.IntegrationTests.Schedules;

public class CreateScheduleTests : ScheduleEndpointsTestBase
{
    public CreateScheduleTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private static CreateScheduleCommand BuildCommand(Guid equipmentId, DateOnly date, DateTimeOffset start, DateTimeOffset end) =>
        new(
            new SchedulerInput("Maria Silva", "Biomedicina", "maria.silva@test.com"),
            new SchedulingInput(date, start, end),
            true,
            "Prof. João Souza",
            "Estudo de Virologia",
            "Análise de amostras virais",
            [new ScheduleEquipmentInput(equipmentId, "Microscópio Eletrônico")]);

    [Fact]
    public async Task ShouldReturn201_WhenRequestIsValid()
    {
        var (date, start, end) = ScheduleDataSeeder.NextBusinessSlot();

        var response = await Client.PostAsJsonAsync(
            $"{PublicBaseRoute}/",
            BuildCommand(Guid.NewGuid(), date, start, end));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn400_WhenAcceptTermIsFalse()
    {
        var (date, start, end) = ScheduleDataSeeder.NextBusinessSlot();

        var command = BuildCommand(Guid.NewGuid(), date, start, end) with { AcceptTerm = false };

        var response = await Client.PostAsJsonAsync($"{PublicBaseRoute}/", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenEquipmentHasConflictingApprovedSchedule()
    {
        var equipmentId = Guid.NewGuid();
        await SeedScheduledAsync(equipmentId);

        var (date, start, end) = ScheduleDataSeeder.NextBusinessSlot();

        var response = await Client.PostAsJsonAsync(
            $"{PublicBaseRoute}/",
            BuildCommand(equipmentId, date, start, end));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenNoEquipmentIsInformed()
    {
        var (date, start, end) = ScheduleDataSeeder.NextBusinessSlot();

        var command = new CreateScheduleCommand(
            new SchedulerInput("Maria Silva", "Biomedicina", "maria.silva@test.com"),
            new SchedulingInput(date, start, end),
            true,
            "Prof. João Souza",
            "Estudo de Virologia",
            "Análise de amostras virais",
            []);

        var response = await Client.PostAsJsonAsync($"{PublicBaseRoute}/", command);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}

public class GetScheduleTests : ScheduleEndpointsTestBase
{
    public GetScheduleTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn200_WhenGettingAll()
    {
        await SeedPendingAsync();

        var response = await Client.GetAsync(BaseRoute);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn200_WhenGettingPending()
    {
        await SeedPendingAsync();

        var response = await Client.GetAsync($"{BaseRoute}/pending");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn200_WhenScheduleExists()
    {
        var scheduleId = await SeedPendingAsync();

        var response = await Client.GetAsync($"{BaseRoute}/{scheduleId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenScheduleDoesNotExist()
    {
        var response = await Client.GetAsync($"{BaseRoute}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class ApproveScheduleTests : ScheduleEndpointsTestBase
{
    public ApproveScheduleTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn202_WhenScheduleIsPending()
    {
        var scheduleId = await SeedPendingAsync();

        var response = await Client.PostAsync($"{BaseRoute}/{scheduleId}/approve", null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenScheduleIsAlreadyApproved()
    {
        var scheduleId = await SeedScheduledAsync();

        var response = await Client.PostAsync($"{BaseRoute}/{scheduleId}/approve", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenScheduleDoesNotExist()
    {
        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn403_WhenUserHasNoManagePermission()
    {
        AuthenticateAs([LabViroMol.Modules.Shared.Kernel.Authorization.Permissions.Scheduling.SchedulesView]);
        var scheduleId = await SeedPendingAsync();

        var response = await Client.PostAsync($"{BaseRoute}/{scheduleId}/approve", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

public class RefuseScheduleTests : ScheduleEndpointsTestBase
{
    public RefuseScheduleTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn202_WhenScheduleIsPending()
    {
        var scheduleId = await SeedPendingAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{scheduleId}/refuse",
            new ReproveScheduleRequest("Equipamento indisponível para manutenção programada."));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenScheduleIsAlreadyApproved()
    {
        var scheduleId = await SeedScheduledAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{scheduleId}/refuse",
            new ReproveScheduleRequest("Equipamento indisponível para manutenção programada."));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenScheduleDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/refuse",
            new ReproveScheduleRequest("Equipamento indisponível para manutenção programada."));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class CancelScheduleTests : ScheduleEndpointsTestBase
{
    public CancelScheduleTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn202_WhenScheduleIsPending()
    {
        var scheduleId = await SeedPendingAsync();

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{scheduleId}/cancel",
            new CancelScheduleRequest("Agendador desistiu da reserva."));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn422_WhenScheduleIsAlreadyCanceled()
    {
        var scheduleId = await SeedPendingAsync();
        await Client.PostAsJsonAsync(
            $"{BaseRoute}/{scheduleId}/cancel",
            new CancelScheduleRequest("Agendador desistiu da reserva."));

        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{scheduleId}/cancel",
            new CancelScheduleRequest("Agendador desistiu da reserva."));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenScheduleDoesNotExist()
    {
        var response = await Client.PostAsJsonAsync(
            $"{BaseRoute}/{Guid.NewGuid()}/cancel",
            new CancelScheduleRequest("Agendador desistiu da reserva."));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class UploadTermScheduleTests : ScheduleEndpointsTestBase
{
    public UploadTermScheduleTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task ShouldReturn202_WhenFileIsValidPdf()
    {
        var scheduleId = await SeedPendingAsync();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0x25, 0x50, 0x44, 0x46]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "termo.pdf");

        var response = await Client.PostAsync($"{BaseRoute}/{scheduleId}/term", content);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var schedule = await DbContext.Schedules.AsNoTracking()
            .FirstAsync(s => s.Id == ScheduleId.From(scheduleId));
        Assert.NotNull(schedule.TermUrl);
    }

    [Fact]
    public async Task ShouldReturn422_WhenFileExtensionIsNotAllowed()
    {
        var scheduleId = await SeedPendingAsync();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0x00, 0x01]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "termo.txt");

        var response = await Client.PostAsync($"{BaseRoute}/{scheduleId}/term", content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ShouldReturn404_WhenScheduleDoesNotExist()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0x25, 0x50, 0x44, 0x46]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "termo.pdf");

        var response = await Client.PostAsync($"{BaseRoute}/{Guid.NewGuid()}/term", content);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
