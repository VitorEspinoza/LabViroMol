using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Scheduling.Infrastructure.Schedules;

public class ScheduleRepository : IScheduleRepository
{
    private readonly SchedulingDbContext _context;

    public ScheduleRepository(SchedulingDbContext context)
    {
        _context = context;
    }
    
    public async Task<Schedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Schedules
            .FirstOrDefaultAsync(s => s.Id == ScheduleId.From(id), cancellationToken);
    }

    public async Task AddAsync(Schedule schedule, CancellationToken cancellationToken)
    {
        await _context.Schedules.AddAsync(schedule, cancellationToken);
    }

    public async Task<List<Schedule>> GetSchedulesConflictByDatesAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken)
    {
        return await _context.Schedules
            .Where(schedule =>
                schedule.Scheduling.StartDateHour < end &&
                schedule.Scheduling.EndDateHour > start &&
                schedule.Status.Equals(ScheduleStatus.SCHEDULED))
            .ToListAsync(cancellationToken);
    }
}