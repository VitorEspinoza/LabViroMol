using LabViroMol.Modules.Research.Domain.Projects;
using Mediator;

namespace LabViroMol.Modules.Research.Application.Projects.EventHandlers;

public sealed record ProjectTranslationEvent(ProjectId ProjectId) : INotification;