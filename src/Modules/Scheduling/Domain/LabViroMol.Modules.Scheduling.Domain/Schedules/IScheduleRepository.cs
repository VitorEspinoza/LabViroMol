namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public interface IScheduleRepository
{
    Task<Schedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Schedule schedule, CancellationToken cancellationToken);
    
    Task<List<Schedule>> GetSchedulesConflictByDatesAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken);
}