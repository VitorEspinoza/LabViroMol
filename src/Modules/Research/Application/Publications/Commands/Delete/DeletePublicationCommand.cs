namespace LabViroMol.Modules.Research.Application.Publications.Commands.Delete;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record DeletePublicationCommand(Guid PublicationId) : ICommand<Result>;
