namespace LabViroMol.Modules.Research.Application.Projects.Commands.RemoveMember;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record RemoveProjectMemberCommand(
    Guid ProjectId,
    Guid ResearcherId,
    Guid RequestedById) : ICommand<Result>;
