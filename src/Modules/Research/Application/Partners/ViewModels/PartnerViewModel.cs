using System;

namespace LabViroMol.Modules.Research.Application.Partners.ViewModels;

public record PartnerViewModel(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt);
