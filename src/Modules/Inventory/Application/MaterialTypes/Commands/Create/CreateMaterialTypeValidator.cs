using FluentValidation;
using LabViroMol.Modules.Inventory.Application.MaterialTypes.Commands.Create;

namespace LabViroMol.Modules.Inventory.Application.MaterialTypes.Create;

public class CreateMaterialTypeValidator : AbstractValidator<CreateMaterialTypeCommand>
{
    public CreateMaterialTypeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do tipo de material é obrigatório.")
            .MaximumLength(200).WithMessage("O nome não pode exceder 200 caracteres.");
    }
}
