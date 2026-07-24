using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Roles.CreateRole;

public sealed class CreateRoleCommandHandler(IIdentityService identityService)
    : ICommandHandler<CreateRoleCommand, Result>
{
    public async ValueTask<Result> Handle(CreateRoleCommand command, CancellationToken ct)
    {
        return await identityService.CreateRoleAsync(command.Name, command.Permissions, ct);
    }
}
