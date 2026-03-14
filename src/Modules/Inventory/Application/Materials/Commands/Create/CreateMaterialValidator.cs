using FluentValidation;
using LabViroMol.Modules.Inventory.Domain.Materials;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.Create;

public class CreateMaterialValidator : AbstractValidator<CreateMaterialCommand>
{
    public CreateMaterialValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do material é obrigatório.")
            .MaximumLength(200).WithMessage("O nome não pode exceder 200 caracteres.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("A localização é obrigatória.")
            .MaximumLength(200).WithMessage("A localização não pode exceder 200 caracteres.");

        RuleFor(x => x.TypeId.Value)
            .NotEmpty().WithMessage("O tipo de material é obrigatório.");

        RuleFor(x => x.Unit)
            .IsInEnum().WithMessage("Unidade de medida inválida.");

        RuleFor(x => x.MinStock.Value)
            .GreaterThanOrEqualTo(0).WithMessage("O estoque mínimo não pode ser negativo.");

        RuleFor(x => x.StockQuantity.Value)
            .GreaterThanOrEqualTo(0).WithMessage("A quantidade em estoque não pode ser negativa.");
    }
}
