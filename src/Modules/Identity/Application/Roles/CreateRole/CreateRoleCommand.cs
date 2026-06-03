using System.Collections.Generic;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Roles.CreateRole;

public record CreateRoleCommand(string Name, List<string> Permissions) : ICommand<Result>;
