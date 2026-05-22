namespace LabViroMol.Modules.Research.Application.Partners.Commands.Delete;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record DeletePartnerCommand(Guid PartnerId) : ICommand<Result>;
