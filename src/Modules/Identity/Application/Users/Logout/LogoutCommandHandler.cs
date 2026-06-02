using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.Logout;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand, Result>
{
    private readonly IIdentityService _identityService;

    public LogoutCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async ValueTask<Result> Handle(LogoutCommand command, CancellationToken ct)
    {
        return await _identityService.LogoutAsync(command.UserId, ct);
    }
}
