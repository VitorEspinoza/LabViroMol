using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Roles.DeleteRole;

public record DeleteRoleCommand(Guid RoleId) : ICommand<Result>;
