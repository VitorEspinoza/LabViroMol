namespace LabViroMol.Modules.Research.Application.Projects.Commands.Cancel;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record CancelProjectCommand(Guid ProjectId, Guid ResearcherId) : ICommand<Result>;
