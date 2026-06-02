using FluentValidation;

namespace LabViroMol.Modules.Identity.Application.Users.DeactivateUser;

public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("O identificador do usuário é obrigatório.");
    }
}
