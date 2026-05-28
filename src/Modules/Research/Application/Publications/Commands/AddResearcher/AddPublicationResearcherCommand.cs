namespace LabViroMol.Modules.Research.Application.Publications.Commands.AddResearcher;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record AddPublicationResearcherCommand(
    Guid PublicationId,
    Guid ResearcherId) : ICommand<Result>;
