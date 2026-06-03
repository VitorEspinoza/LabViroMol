using System;

namespace LabViroMol.Modules.Research.Application.Publications.Commands.Delete;

using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

public record DeletePublicationCommand(Guid PublicationId) : ICommand<Result>;
