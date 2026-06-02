using FluentValidation;

namespace LabViroMol.Modules.Identity.Application.Users.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage("O identificador do usuário é obrigatório.");

        RuleFor(x => x.UserData.FirstName)
            .NotEmpty().WithMessage("O primeiro nome é obrigatório.")
            .MinimumLength(2).WithMessage("O primeiro nome deve ter no mínimo 2 caracteres.");

        RuleFor(x => x.UserData.LastName)
            .NotEmpty().WithMessage("O sobrenome é obrigatório.")
            .MinimumLength(2).WithMessage("O sobrenome deve ter no mínimo 2 caracteres.");

        RuleFor(x => x.UserData.PhoneNumber)
            .Matches(@"^\+?\d+$").WithMessage("Telefone deve conter apenas dígitos (opcionalmente precedido por '+').")
            .When(x => !string.IsNullOrEmpty(x.UserData.PhoneNumber));

        RuleFor(x => x.UserData.EmergencyContactNumber)
            .Matches(@"^\+?\d+$").WithMessage("Contato de emergência deve conter apenas dígitos (opcionalmente precedido por '+').")
            .When(x => !string.IsNullOrEmpty(x.UserData.EmergencyContactNumber));
    }
}
