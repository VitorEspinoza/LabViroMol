namespace LabViroMol.Modules.Research.Application.Partners.Commands.Create;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record CreatePartnerCommand(string Name, string? Description) : ICommand<Result>;
