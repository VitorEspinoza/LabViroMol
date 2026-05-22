namespace LabViroMol.Modules.Research.Application.Partners.Commands.Create;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record CreatePartnerCommand(string Name, string? Description) : ICommand<Result>;
