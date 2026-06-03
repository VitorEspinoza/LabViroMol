using System;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.DeactivateUser;

public record DeactivateUserCommand(Guid TargetUserId) : ICommand<Result>;
