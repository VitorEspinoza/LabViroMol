namespace LabViroMol.Modules.Research.Application.Projects.Commands.Create;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record CreateProjectCommand(
    Guid PrincipalInvestigatorId,
    string Title,
    string Description,
    Guid PartnerId) : ICommand<Result>;
