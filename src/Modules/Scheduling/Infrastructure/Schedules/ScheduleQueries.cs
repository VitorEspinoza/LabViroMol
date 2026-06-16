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
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Schedule> query = _context.Schedules;

        query = query.WhereSearch(request.Search,
            s => s.ProjectTitle, s => s.Description, s => s.AdvisorProfessor,
            s => s.Scheduler.Name, s => s.Scheduler.Course, s => s.Scheduler.Email);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "date" => request.SortDirection == "desc"
                ? query.OrderByDescending(s => s.Scheduling.StartDateHour)
                : query.OrderBy(s => s.Scheduling.StartDateHour),
            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(s => s.Status)
                : query.OrderBy(s => s.Status),
            "user" => request.SortDirection == "desc"
                ? query.OrderByDescending(s => s.Scheduler.Name)
                : query.OrderBy(s => s.Scheduler.Name),
            "createdat" => request.SortDirection == "desc"
                ? query.OrderByDescending(s => EF.Property<DateTimeOffset>(s, "CreatedAt"))
                : query.OrderBy(s => EF.Property<DateTimeOffset>(s, "CreatedAt")),
            _ => query.OrderByDescending(s => s.Scheduling.StartDateHour)
        };

        var entities = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entities.Select(ScheduleMapper.FromEntity).ToList();
        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<ScheduleViewModel?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == ScheduleId.From(id));

        return entity is null ? null : ScheduleMapper.FromEntity(entity);
    }

    public async Task<PagedResponse<ScheduleViewModel>> GetAllPendingAsync(PagedRequest request)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var pageNumber = Math.Max(request.PageNumber, 1);

        IQueryable<Schedule> query = _context.Schedules
            .Where(s => s.Status.Equals(ScheduleStatus.PENDING) &&
                        s.Scheduling.StartDateHour > DateTimeOffset.Now);

        query = query.WhereSearch(request.Search,
            s => s.ProjectTitle, s => s.Description, s => s.AdvisorProfessor,
            s => s.Scheduler.Name, s => s.Scheduler.Course, s => s.Scheduler.Email);

        var totalCount = await query.CountAsync();

        query = request.SortBy?.ToLower() switch
        {
            "date" => request.SortDirection == "desc"
                ? query.OrderByDescending(s => s.Scheduling.StartDateHour)
                : query.OrderBy(s => s.Scheduling.StartDateHour),
            "status" => request.SortDirection == "desc"
                ? query.OrderByDescending(s => s.Status)
                : query.OrderBy(s => s.Status),
            "createdat" => request.SortDirection == "desc"
                ? query.OrderByDescending(s => EF.Property<DateTimeOffset>(s, "CreatedAt"))
                : query.OrderBy(s => EF.Property<DateTimeOffset>(s, "CreatedAt")),
            _ => query.OrderBy(s => s.Scheduling.StartDateHour)
        };

        var entities = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = entities.Select(ScheduleMapper.FromEntity).ToList();
        return PagedResult.Create(items, pageNumber, pageSize, totalCount);
    }
}
