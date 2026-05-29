namespace LabViroMol.Modules.Research.Application.Publications.Commands.RemoveResearcher;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record RemovePublicationResearcherCommand(
    Guid PublicationId,
    Guid ResearcherId) : ICommand<Result>;
