namespace LabViroMol.Modules.Research.Application.Publications.Commands.ReorderResearchers;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record ReorderPublicationResearchersCommand(
    Guid PublicationId,
    List<Guid> ResearcherIds) : ICommand<Result>;
