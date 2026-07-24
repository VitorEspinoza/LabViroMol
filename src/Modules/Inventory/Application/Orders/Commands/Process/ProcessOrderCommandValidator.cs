using FluentValidation;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Process;

public class ProcessOrderCommandValidator : AbstractValidator<ProcessOrderCommand>
{
    public ProcessOrderCommandValidator()
    {
        RuleFor(x => x.OrderId.Value)
            .NotEmpty().WithMessage("O id do pedido é obrigatório.");

    }
}
