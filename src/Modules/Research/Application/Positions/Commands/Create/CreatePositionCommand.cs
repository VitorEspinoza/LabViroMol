namespace LabViroMol.Modules.Research.Application.Positions.Commands.Create;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record CreatePositionCommand(string Name, string Description) : ICommand<Result>;
