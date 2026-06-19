namespace LabViroMol.Modules.Research.Application.Publications.Queries;

using LabViroMol.Modules.Research.Application.Publications.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

public interface IPublicationQueries
{
    Task<PagedResponse<PublicationSummaryViewModel>> GetAllInstitutionalAsync(PagedRequest request, string? language);

    Task<PagedResponse<PublicationAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request);

    Task<PublicationViewModel?> GetById(Guid id);
}
