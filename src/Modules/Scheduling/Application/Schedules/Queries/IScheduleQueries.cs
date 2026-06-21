using LabViroMol.Modules.Scheduling.Application.Schedules.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Queries;

public interface IScheduleQueries
{
    Task<PagedResponse<ScheduleViewModel>> GetAllAsync(PagedRequest request);

    Task<ScheduleViewModel?> GetByIdAsync(Guid id);

    Task<PagedResponse<ScheduleViewModel>> GetAllPendingAsync(PagedRequest request);
}
