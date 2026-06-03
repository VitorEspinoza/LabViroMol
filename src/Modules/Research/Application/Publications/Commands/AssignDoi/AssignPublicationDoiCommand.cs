using System;

namespace LabViroMol.Modules.Research.Application.Publications.Commands.AssignDoi;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record AssignPublicationDoiCommand(
    Guid PublicationId,
    string Doi) : ICommand<Result>;
