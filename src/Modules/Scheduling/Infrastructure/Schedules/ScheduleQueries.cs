using LabViroMol.Modules.Scheduling.Application.Schedules.Mappings;
using LabViroMol.Modules.Scheduling.Application.Schedules.ViewModels;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using LabViroMol.Modules.Shared.Kernel.Pagination;
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

    public async Task<PagedResponse<ScheduleViewModel>> GetAllAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        IQueryable<Schedule> query = _context.Schedules;

        query = request.SortBy?.ToLower() switch
        {
            "date" => request.SortDirection == "desc"
                ? query.OrderByDescending(s => s.Scheduling.StartDateHour)
                : query.OrderBy(s => s.Scheduling.StartDateHour),
            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(s => s.Status)
                : query.OrderBy(s => s.Status),
            _ => query.OrderByDescending(s => s.Scheduling.StartDateHour)
        };

        int total = await query.CountAsync();
        var entities = await query
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entities.Select(ScheduleMapper.FromEntity).ToList();
        return new PagedResponse<ScheduleViewModel>(items, request.Page, pageSize, total);
    }

    public async Task<PagedResponse<ScheduleViewModel>> GetAllPendingAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        IQueryable<Schedule> query = _context.Schedules
            .Where(s => s.Status.Equals(ScheduleStatus.PENDING) &&
                        s.Scheduling.StartDateHour > DateTimeOffset.Now);

        query = request.SortBy?.ToLower() switch
        {
            "date" => request.SortDirection == "desc"
                ? query.OrderByDescending(s => s.Scheduling.StartDateHour)
                : query.OrderBy(s => s.Scheduling.StartDateHour),
            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(s => s.Status)
                : query.OrderBy(s => s.Status),
            _ => query.OrderBy(s => s.Scheduling.StartDateHour)
        };

        int total = await query.CountAsync();
        var entities = await query
            .Skip((request.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entities.Select(ScheduleMapper.FromEntity).ToList();
        return new PagedResponse<ScheduleViewModel>(items, request.Page, pageSize, total);
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
