namespace LabViroMol.Modules.Research.Application.Positions.Commands.Delete;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record DeletePositionCommand(Guid PositionId) : ICommand<Result>;
