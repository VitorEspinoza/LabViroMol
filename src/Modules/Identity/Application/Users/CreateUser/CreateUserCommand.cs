using System;
using System.Collections.Generic;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.CreateUser;

public record CreateUserCommand(
    UserInfo UserData,
    string Email,
    List<Guid> RoleIds) : ICommand<Result<string>>;
