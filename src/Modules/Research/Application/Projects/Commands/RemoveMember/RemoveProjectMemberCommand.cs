namespace LabViroMol.Modules.Research.Application.Projects.Commands.RemoveMember;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record RemoveProjectMemberCommand(
    Guid ProjectId,
    Guid ResearcherId,
    Guid RequestedById) : ICommand<Result>;
