using FluentValidation;

namespace LabViroMol.Modules.Identity.Application.Users.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("O identificador do usuário é obrigatório.");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("A senha atual é obrigatória.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("A nova senha é obrigatória.")
            .MinimumLength(8).WithMessage("A nova senha deve ter no mínimo 8 caracteres.");
    }
}
