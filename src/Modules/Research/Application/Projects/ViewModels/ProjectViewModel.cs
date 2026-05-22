using LabViroMol.Modules.Research.Domain.Projects;

namespace LabViroMol.Modules.Research.Application.Projects.ViewModels;

public record ProjectViewModel(
    Guid Id,
    string Title,
    string Description,
    ProjectStatus Status,
    Guid PartnerId,
    string PartnerName,
    IReadOnlyCollection<ProjectMemberViewModel> Members,
    DateTimeOffset CreatedAt);
