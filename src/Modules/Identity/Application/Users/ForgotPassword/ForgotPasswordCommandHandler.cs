using LabViroMol.Modules.Identity.Application.Users.Abstractions;
using LabViroMol.Modules.Identity.Application.Users.Emails;
using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.ForgotPassword;

public class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IIdentityUnitOfWork _unitOfWork;

    public ForgotPasswordCommandHandler(IIdentityService identityService, IIdentityUnitOfWork unitOfWork)
    {
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<Result> Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var result = await _identityService.GeneratePasswordResetTokenAsync(command.Email, ct);

        if (result.IsFailure)
            return Result.Success();

        var (resetLink, firstName) = result.Data!;

        var (subject, body) = PasswordEmailTemplates.BuildPasswordResetEmail(firstName, resetLink);
        
        _unitOfWork.AddPersistentEvent(new ForgotPasswordPersistentEvent(
            command.Email,
            subject,
            body));
        
        await _unitOfWork.CompleteAsync(ct);
        
        return Result.Success();
    }
}
