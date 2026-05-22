namespace LabViroMol.Modules.Research.Application.Publications.Commands.Update;

using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

public record UpdatePublicationCommand(
    Guid PublicationId,
    string Title,
    string Description,
    string PublishedOn,
    string PublishUrl) : ICommand<Result>;
