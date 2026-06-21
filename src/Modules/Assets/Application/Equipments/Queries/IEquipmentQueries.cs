using LabViroMol.Modules.Assets.Application.Equipments.ViewModels;
using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Assets.Application.Equipments.Queries;

public interface IEquipmentQueries
{
    Task<PagedResponse<EquipmentViewModel>> GetAllInstitutionalAsync(PagedRequest request, string? language);

    Task<PagedResponse<EquipmentAdminSummaryViewModel>> GetAllAdminAsync(PagedRequest request);

    Task<EquipmentAdminDetailViewModel?> GetAdminByIdAsync(Guid id);

    Task<EquipmentViewModel?> GetEquipmentByIdInstitutional(Guid id, string? language);

    Task<List<EquipmentSchedulableViewModel>> GetSchedulableEquipments(string? language);
}
