using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Roles.UpdateRolePermissions;

public sealed class UpdateRolePermissionsCommandHandler(IIdentityService identityService)
    : ICommandHandler<UpdateRolePermissionsCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateRolePermissionsCommand command, CancellationToken ct)
    {
        return await identityService.UpdateRolePermissionsAsync(command.RoleId, command.Permissions, ct);
    }
}
