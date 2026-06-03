using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.Login;

public class LoginCommandHandler
    : ICommandHandler<LoginCommand, Result<(string AccessToken, string RefreshToken)>>
{
    private readonly IIdentityService _identityService;

    public LoginCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async ValueTask<Result<(string AccessToken, string RefreshToken)>> Handle(
        LoginCommand command, CancellationToken ct)
    {
        return await _identityService.LoginAsync(command.Email, command.Password, ct);
    }
}
