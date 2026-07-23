namespace LabViroMol.Modules.Research.Application.Projects.Queries;

using LabViroMol.Modules.Research.Application.Projects.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

public interface IProjectQueries
{
    Task<PagedResponse<PublicProjectViewModel>> GetAllInstitutionalAsync(PagedRequest request, string? language);

    Task<PagedResponse<ProjectAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request);

    Task<ProjectViewModel?> GetById(Guid id);
    
    Task<ProjectsCountersViewModel> GetInstitutionalProjectsCounters();
}
