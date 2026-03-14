using FluentValidation;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Cancel;

public class CancelOrderCommandValidator : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(x => x.OrderId.Value)
            .NotEmpty().WithMessage("O id do pedido é obrigatório.");
    }
}
