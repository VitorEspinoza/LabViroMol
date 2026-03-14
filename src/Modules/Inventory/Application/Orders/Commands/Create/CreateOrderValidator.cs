using FluentValidation;

namespace LabViroMol.Modules.Inventory.Application.Orders.Commands.Create;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.MaterialId.Value)
            .NotEmpty().WithMessage("O material é obrigatório.");

        RuleFor(x => x.ProjectId.Value)
            .NotEmpty().WithMessage("O projeto é obrigatório.");

        RuleFor(x => x.Quantity.Value)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

        RuleFor(x => x.description)
            .NotEmpty().WithMessage("A descrição é obrigatória.")
            .MaximumLength(500).WithMessage("A descrição não pode exceder 500 caracteres.");
    }
}
