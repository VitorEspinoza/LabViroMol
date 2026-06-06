namespace LabViroMol.Modules.Research.Application.Projects.ViewModels;

public record ProjectAdminSummaryViewModel(
    Guid Id,
    string Title,
    string PartnerName,
    string ManagerName,
    string Status,
    DateTimeOffset CreatedAt);
