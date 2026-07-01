using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Roles.DeleteRole;

public sealed class DeleteRoleCommandHandler(IIdentityService identityService)
    : ICommandHandler<DeleteRoleCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteRoleCommand command, CancellationToken ct)
        => await identityService.DeleteRoleAsync(command.RoleId, ct);
}
