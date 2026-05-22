namespace LabViroMol.Modules.Research.Application.Positions.Commands.Create;

using FluentValidation;

public class CreatePositionValidator : AbstractValidator<CreatePositionCommand>
{
    public CreatePositionValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do cargo é obrigatório.")
            .MinimumLength(3).WithMessage("O nome deve ter ao menos 3 caracteres.")
            .MaximumLength(200).WithMessage("O nome não pode exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("A descrição não pode exceder 2000 caracteres.");
    }
}
