using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Roles.UpdateRolePermissions;

public record UpdateRolePermissionsCommand(Guid RoleId, List<string> Permissions) : ICommand<Result>;
