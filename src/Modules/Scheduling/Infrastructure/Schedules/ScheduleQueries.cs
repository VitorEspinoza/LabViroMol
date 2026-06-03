using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabViroMol.Modules.Scheduling.Application.Schedules.Mappings;
using LabViroMol.Modules.Scheduling.Application.Schedules.ViewModels;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Scheduling.Infrastructure.Schedules;

public class ScheduleQueries
{
    private readonly SchedulingDbContext _context;

    public ScheduleQueries(SchedulingDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    public async Task<List<ScheduleViewModel>> GetAllSchedules()
    {
        var schedules = await _context.Schedules
            .Select(s => ScheduleMapper.FromEntity(s))
            .ToListAsync();

        return schedules;
    }

    public async Task<List<ScheduleViewModel>> GetAllSchedulesPending()
    {
        var schedulesPending = await _context.Schedules
            .Where(s => s.Status.Equals(ScheduleStatus.PENDING) && 
                        s.Scheduling.StartDateHour > DateTimeOffset.Now)
            .Select(s => ScheduleMapper.FromEntity(s))
            .ToListAsync();
        
        return schedulesPending;
    }
}