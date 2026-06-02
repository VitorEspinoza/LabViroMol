using FluentValidation;

namespace LabViroMol.Modules.Identity.Application.Users.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("Formato de e-mail inválido.");

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

        When(x => x.UserData.ResearchData is not null, () =>
        {
            RuleFor(x => x.UserData.ResearchData!.PositionId)
                .NotEmpty().WithMessage("O cargo é obrigatório.");

            RuleFor(x => x.UserData.ResearchData!.DegreeLevel)
                .NotEmpty().WithMessage("O nível de formação é obrigatório.");

            RuleFor(x => x.UserData.ResearchData!.FieldOfStudy)
                .NotEmpty().WithMessage("A área de estudo é obrigatória.");
        });
    }
}
