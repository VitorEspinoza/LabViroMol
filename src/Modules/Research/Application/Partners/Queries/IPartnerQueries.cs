namespace LabViroMol.Modules.Research.Application.Partners.Queries;

using LabViroMol.Modules.Research.Application.Partners.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

public interface IPartnerQueries
{
    Task<PagedResponse<PartnerSummaryViewModel>> GetAllInstitutionalAsync(PagedRequest request);

    Task<PagedResponse<PartnerAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request);

    Task<PartnerViewModel?> GetById(Guid id);
}
