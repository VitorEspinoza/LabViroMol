using LabViroMol.Modules.Research.Domain.Positions;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Positions.EventHandlers;

public sealed record PositionTranslationEvent(
    PositionId PositionId
) : INotification;