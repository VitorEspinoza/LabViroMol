namespace LabViroMol.Modules.Research.Application.Positions.Commands.Create;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record CreatePositionCommand(string Name, string Description) : ICommand<Result>;
