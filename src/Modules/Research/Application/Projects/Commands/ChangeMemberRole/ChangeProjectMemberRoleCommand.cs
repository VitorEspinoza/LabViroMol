namespace LabViroMol.Modules.Research.Application.Projects.Commands.ChangeMemberRole;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record ChangeProjectMemberRoleCommand(
    Guid ProjectId,
    Guid ResearcherId,
    string NewRole,
    Guid RequestedById) : ICommand<Result>;
