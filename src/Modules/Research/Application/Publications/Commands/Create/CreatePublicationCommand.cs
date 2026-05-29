namespace LabViroMol.Modules.Research.Application.Publications.Commands.Create;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record CreatePublicationCommand(
    string Title,
    string Description,
    string Doi,
    DateOnly PublicationDate,
    string PublishedOn,
    string PublishUrl) : ICommand<Result>;
