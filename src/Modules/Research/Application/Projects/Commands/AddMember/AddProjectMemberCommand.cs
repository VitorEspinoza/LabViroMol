namespace LabViroMol.Modules.Research.Application.Projects.Commands.AddMember;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record AddProjectMemberCommand(
    Guid ProjectId,
    Guid ResearcherId,
    string Role,
    Guid RequestedById) : ICommand<Result>;
