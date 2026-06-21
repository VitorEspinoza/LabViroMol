using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Identity;
using SchedulingPeriod = LabViroMol.Modules.Scheduling.Domain.Schedules.Scheduling;

namespace LabViroMol.Modules.Scheduling.IntegrationTests.Schedules;

public static class ScheduleDataSeeder
{
    public static DateOnly NextBusinessDay()
    {
        var date = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            date = date.AddDays(1);

        return date;
    }

    public static (DateOnly Date, DateTimeOffset Start, DateTimeOffset End) NextBusinessSlot()
    {
        var date = NextBusinessDay();
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
        var end = new DateTimeOffset(date.ToDateTime(new TimeOnly(11, 0)), TimeSpan.Zero);

        return (date, start, end);
    }

    public static async Task<Guid> SeedPendingAsync(
        SchedulingDbContext dbContext,
        Guid? equipmentId = null)
    {
        var schedule = CreateSchedule(equipmentId);

        await dbContext.Schedules.AddAsync(schedule);
        await dbContext.SaveChangesAsync();

        return schedule.Id.Value;
    }

    public static async Task<Guid> SeedScheduledAsync(
        SchedulingDbContext dbContext,
        Guid? equipmentId = null,
        Guid? approvedBy = null)
    {
        var schedule = CreateSchedule(equipmentId);
        schedule.Approve(UserId.From(approvedBy ?? Guid.NewGuid()));

        await dbContext.Schedules.AddAsync(schedule);
        await dbContext.SaveChangesAsync();

        return schedule.Id.Value;
    }

    private static Schedule CreateSchedule(Guid? equipmentId)
    {
        var (date, start, end) = NextBusinessSlot();

        var scheduling = SchedulingPeriod.Create(date, start, end).Data!;

        return Schedule.Create(
            new Scheduler("Maria Silva", "Biomedicina", "maria.silva@test.com"),
            scheduling,
            acceptTerm: true,
            advisorProfessor: "Prof. João Souza",
            projectTitle: "Estudo de Virologia",
            description: "Análise de amostras virais",
            equipments: [new ScheduleEquipment(equipmentId ?? Guid.NewGuid(), "Microscópio Eletrônico")]).Data!;
    }
}
