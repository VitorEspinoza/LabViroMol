using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Scheduling.Domain.UnitTests.Common;

internal static class Fakers
{
    private static readonly Faker f = new("pt_BR");
    
    #region Primitives
    
    public static UserId AnyUserId() => IdFactory.New<UserId>(); 
    
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
    
    public static DateOnly NextWorkday()
    {
        var date = DateOnly.FromDateTime(DateTime.Now.AddDays(2));
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            date = date.AddDays(1);
        return date;
    }
    
    public static Domain.Schedules.Scheduling CreateScheduling()
    {
        var date = NextWorkday();
        var start = date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)));
        var end   = date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)));

        return Domain.Schedules.Scheduling.Create(
            date,
            new DateTimeOffset(start),
            new DateTimeOffset(end)
        ).Data!;
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