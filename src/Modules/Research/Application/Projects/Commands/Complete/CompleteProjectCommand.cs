namespace LabViroMol.Modules.Research.Application.Projects.Commands.Complete;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record CompleteProjectCommand(Guid ProjectId, Guid ResearcherId) : ICommand<Result>;
