using System;

namespace LabViroMol.Modules.Research.Application.Projects.ViewModels;

public record ProjectSummaryViewModel(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string ResearchLead,
    string PartnerName,
    DateTimeOffset CreatedAt);
