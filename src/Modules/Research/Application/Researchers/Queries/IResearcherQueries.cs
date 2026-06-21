namespace LabViroMol.Modules.Research.Application.Researchers.Queries;

using LabViroMol.Modules.Research.Application.Researchers.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

public interface IResearcherQueries
{
    Task<PagedResponse<ResearcherSummaryViewModel>> GetAllInstitutionalAsync(PagedRequest request, string? language);
}
