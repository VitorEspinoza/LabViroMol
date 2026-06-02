using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.ForgotPassword;

public class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, Result<string>>
{
    private readonly IIdentityService _identityService;

    public ForgotPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async ValueTask<Result<string>> Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        return await _identityService.GeneratePasswordResetTokenAsync(command.Email, ct);
    }
}
