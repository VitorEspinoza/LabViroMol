using Bogus;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Abstractions.Identity;

namespace LabViroMol.Modules.Scheduling.Domain.UnitTests.Common;

internal static class Fakers
{
    private static readonly Faker f = new("pt_BR");
    
    #region Primitives
    
    public static UserId AnyUserId() => UserId.New(); 
    
    #endregion
    
    #region Scheduler
    
    public static Scheduler CreateScheduler(
        string? name = null,
        string? course = null,
        string? email = null)
        => new(
            name ?? f.Name.FullName(),
            course ?? f.Commerce.Categories(1)[0],
            email ?? f.Internet.Email()
        );
    
    #endregion
    
    #region Scheduling
    
    public static Domain.Schedules.Scheduling CreateScheduling(
        DateOnly? date = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null)
    {
        var baseDate = date ?? DateOnly.FromDateTime(DateTime.Now.AddDays(1));

        var startTime = start ?? new DateTimeOffset(
            baseDate.ToDateTime(new TimeOnly(9, 0)),
            TimeSpan.Zero
        );

        var endTime = end ?? startTime.AddHours(1);

        return Domain.Schedules.Scheduling.Create(baseDate, startTime, endTime).Data!;
    }
    
    #endregion
    
    #region Schedule
    
    #region ScheduleEquipment

    public static ScheduleEquipment CreateScheduleEquipment(
        Guid? equipmentId = null,
        string? name = null)
        => new(
            equipmentId ?? Guid.NewGuid(),
            name ?? f.Commerce.ProductName()
        );

    public static List<ScheduleEquipment> CreateScheduleEquipments(int count = 2)
        => Enumerable.Range(0, count)
            .Select(_ => CreateScheduleEquipment())
            .ToList();

    #endregion
    
    public static Schedule CreateSchedule(
        Scheduler? scheduler = null,
        Domain.Schedules.Scheduling? scheduling = null,
        bool? acceptTerm = null,
        string? advisorProfessor = null,
        string? projectTitle = null,
        string? description = null,
        List<ScheduleEquipment>? equipments = null)
        => Schedule.Create(
            scheduler ?? CreateScheduler(),
            scheduling ?? CreateScheduling(),
            acceptTerm ?? true,
            advisorProfessor ?? f.Name.FullName(),
            projectTitle ?? f.Commerce.ProductName(),
            description ?? f.Lorem.Sentence(),
            equipments ?? CreateScheduleEquipments()
        ).Data!;
    
    #endregion
}