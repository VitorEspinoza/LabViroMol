namespace LabViroMol.Modules.Research.Application.Projects.Commands.Update;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record UpdateProjectCommand(
    Guid ProjectId,
    string Title,
    string Description,
    Guid RequestedById) : ICommand<Result>;
