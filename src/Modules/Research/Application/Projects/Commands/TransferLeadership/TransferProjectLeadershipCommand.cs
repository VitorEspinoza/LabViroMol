namespace LabViroMol.Modules.Research.Application.Projects.Commands.TransferLeadership;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record TransferProjectLeadershipCommand(
    Guid ProjectId,
    Guid NewLeadResearcherId,
    Guid RequestedById) : ICommand<Result>;
