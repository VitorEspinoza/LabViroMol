using FluentValidation;
using LabViroMol.Modules.Inventory.Application.Materials.Commands.AddStockException;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.AddStock;

public class AddStockMaterialExceptionValidator : AbstractValidator<AddStockMaterialExceptionCommand>
{
    public AddStockMaterialExceptionValidator()
    {
        RuleFor(x => x.MaterialId)
            .NotEmpty().WithMessage("O ID do material é obrigatório.");

        RuleFor(x => x.Quantity.Value)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A adição de estoque manual requer uma justificativa")
            .MinimumLength(10).WithMessage("É obrigatório no mínimo 10 caracteres de justificativa");
    }
}
