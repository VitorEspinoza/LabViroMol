using System;

namespace LabViroMol.Modules.Research.Application.Partners.ViewModels;

public record PartnerSummaryViewModel(Guid Id, string Name, DateTimeOffset CreatedAt);
