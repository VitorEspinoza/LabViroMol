namespace LabViroMol.Modules.Research.Application.Partners.Commands.Update;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record UpdatePartnerCommand(Guid PartnerId, string Name, string? Description) : ICommand<Result>;
