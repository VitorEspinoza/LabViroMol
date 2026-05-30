using FluentValidation;

namespace LabViroMol.Modules.Identity.Application.Users.ReactivateUser;

public class ReactivateUserCommandValidator : AbstractValidator<ReactivateUserCommand>
{
    public ReactivateUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("O identificador do usuário é obrigatório.");
    }
}
