using LabViroMol.Modules.Research.Domain.Publications;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Publications.EventHandlers;

public sealed record PublicationTranslationEvent(PublicationId PublicationId) : INotification;