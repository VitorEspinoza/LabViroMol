using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.RefreshToken;

public class RefreshTokenCommandHandler
    : ICommandHandler<RefreshTokenCommand, Result<(string AccessToken, string RefreshToken)>>
{
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async ValueTask<Result<(string AccessToken, string RefreshToken)>> Handle(
        RefreshTokenCommand command, CancellationToken ct)
    {
        return await _identityService.RefreshTokenAsync(command.RefreshToken, ct);
    }
}
