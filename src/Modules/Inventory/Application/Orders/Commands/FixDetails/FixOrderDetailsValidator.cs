using FluentValidation;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.FixDetails;

public class FixOrderDetailsValidator : AbstractValidator<FixOrderDetailsCommand>
{
    public FixOrderDetailsValidator()
    {
        RuleFor(x => x.OrderId.Value)
            .NotEmpty().WithMessage("O pedido é obrigatório.");

        RuleFor(x => x.NewProjectId.Value)
            .NotEmpty().WithMessage("O projeto é obrigatório.");

        RuleFor(x => x.NewQuantity.Value)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(500).WithMessage("A descrição não pode exceder 500 caracteres.");

    }
}
