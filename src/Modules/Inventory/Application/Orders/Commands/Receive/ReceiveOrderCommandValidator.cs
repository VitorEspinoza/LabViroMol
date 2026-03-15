using FluentValidation;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Receive;

public class ReceiveOrderCommandValidator : AbstractValidator<ReceiveOrderCommand>
{
    public ReceiveOrderCommandValidator()
    {
        RuleFor(x => x.OrderId.Value)
            .NotEmpty().WithMessage("O id do pedido é obrigatório.");

        RuleFor(x => x.QuantityReceived.Value)
            .GreaterThan(0).WithMessage("A quantidade recebida deve ser maior que zero.");
    }
}
