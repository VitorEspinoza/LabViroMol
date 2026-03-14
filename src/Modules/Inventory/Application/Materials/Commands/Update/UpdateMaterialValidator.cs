using FluentValidation;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.Update;

public class UpdateMaterialValidator : AbstractValidator<UpdateMaterialCommand>
{
    public UpdateMaterialValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do material é obrigatório.")
            .MaximumLength(200).WithMessage("O nome não pode exceder 200 caracteres.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("A localização é obrigatória.")
            .MaximumLength(200).WithMessage("A localização não pode exceder 200 caracteres.");

        RuleFor(x => x.MinStock.Value)
            .GreaterThanOrEqualTo(0).WithMessage("O estoque mínimo não pode ser negativo.");
    }
}
