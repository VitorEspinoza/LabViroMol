using System;
using System.Collections.Generic;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.UpdateUser;

public record UpdateUserCommand(
    Guid TargetUserId,
    UserInfo UserData,
    List<Guid> RoleIds) : ICommand<Result>;
