using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Identity.Application.Users.Emails;
using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.ForgotPassword;

public class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly ISendEmail _emailSender;

    public ForgotPasswordCommandHandler(IIdentityService identityService, ISendEmail emailSender)
    {
        _identityService = identityService;
        _emailSender = emailSender;
    }

    public async ValueTask<Result> Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var result = await _identityService.GeneratePasswordResetTokenAsync(command.Email, ct);

        if (result.IsFailure)
            return Result.Success();

        var (resetLink, firstName) = result.Data!;

        var (subject, body) = PasswordEmailTemplates.BuildPasswordResetEmail(firstName, resetLink);
        await _emailSender.SendEmail(command.Email, subject, body, ct);

        return Result.Success();
    }
}
