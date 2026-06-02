using FluentValidation;

namespace LabViroMol.Modules.Identity.Application.Users.RefreshToken;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("O token de atualização é obrigatório.");
    }
}
