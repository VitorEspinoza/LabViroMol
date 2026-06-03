using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public interface IScheduleRepository
{
    Task<Schedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Schedule schedule, CancellationToken cancellationToken);
    
    Task<List<Schedule>> GetSchedulesConflictAsync(DateTimeOffset start, DateTimeOffset end, List<Guid> equipmentIds, CancellationToken cancellationToken);
}