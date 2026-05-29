namespace LabViroMol.Modules.Research.Application.Partners.Commands.Delete;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record DeletePartnerCommand(Guid PartnerId) : ICommand<Result>;
