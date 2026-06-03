using System;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.ReactivateUser;

public record ReactivateUserCommand(Guid TargetUserId) : ICommand<Result>;
