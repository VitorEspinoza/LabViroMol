using System;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Application.Projects.Commands.AddMember;

using Mediator;

public record AddProjectMemberCommand(
    Guid ProjectId,
    Guid ResearcherId,
    string Role,
    Guid RequestedById) : ICommand<Result>;
