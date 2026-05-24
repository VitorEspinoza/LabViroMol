namespace LabViroMol.Modules.Research.Application.Projects.Commands.Complete;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record CompleteProjectCommand(Guid ProjectId, Guid ResearcherId) : ICommand<Result>;
