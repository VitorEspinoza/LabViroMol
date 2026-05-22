namespace LabViroMol.Modules.Research.Application.Projects.Commands.Create;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record CreateProjectCommand(
    Guid PrincipalInvestigatorId,
    string Title,
    string Description,
    Guid PartnerId) : ICommand<Result>;
