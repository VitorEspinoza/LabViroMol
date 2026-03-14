using FluentValidation;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.RemoveStockException;

public class RemoveStockMaterialExceptionValidator : AbstractValidator<RemoveStockMaterialExceptionCommand>
{
    public RemoveStockMaterialExceptionValidator()
    {
        RuleFor(x => x.MaterialId)
            .NotEmpty().WithMessage("O ID do material é obrigatório.");

        RuleFor(x => x.Quantity.Value)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A remoção de estoque manual requer uma justificativa.")
            .MinimumLength(10).WithMessage("É obrigatório no mínimo 10 caracteres de justificativa.");
    }
}
