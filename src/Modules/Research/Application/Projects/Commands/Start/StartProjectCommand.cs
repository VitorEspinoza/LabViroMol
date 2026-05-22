namespace LabViroMol.Modules.Research.Application.Projects.Commands.Start;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record StartProjectCommand(Guid ProjectId, Guid ResearcherId) : ICommand<Result>;
