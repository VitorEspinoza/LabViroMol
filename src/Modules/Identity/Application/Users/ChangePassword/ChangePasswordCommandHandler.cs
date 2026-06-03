using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.ChangePassword;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Result>
{
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async ValueTask<Result> Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        return await _identityService.ChangePasswordAsync(
            command.UserId, command.CurrentPassword, command.NewPassword, ct);
    }
}
