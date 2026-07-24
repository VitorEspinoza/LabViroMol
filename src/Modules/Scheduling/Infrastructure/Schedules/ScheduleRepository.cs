using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabViroMol.Modules.Scheduling.Infrastructure.Schedules;

internal sealed class ScheduleRepository : IScheduleRepository
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

    public async Task<List<Schedule>> GetSchedulesConflictAsync(
        DateTimeOffset start,
        DateTimeOffset end,
        List<Guid> equipmentIds,
        CancellationToken ct)
    {
        return await _context.Schedules
            .Where(s =>
                s.Scheduling.StartDateHour < end &&
                s.Scheduling.EndDateHour > start &&
                s.Status == ScheduleStatus.SCHEDULED &&
                s.Equipments.Any(e => equipmentIds.Contains(e.EquipmentId))
            )
            .ToListAsync(ct);
    }
}
