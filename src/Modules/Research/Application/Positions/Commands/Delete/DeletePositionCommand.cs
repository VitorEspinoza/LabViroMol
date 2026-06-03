using System;

namespace LabViroMol.Modules.Research.Application.Positions.Commands.Delete;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record DeletePositionCommand(Guid PositionId) : ICommand<Result>;
