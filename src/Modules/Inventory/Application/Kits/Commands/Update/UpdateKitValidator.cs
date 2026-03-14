using FluentValidation;

namespace LabViroMol.Modules.Inventory.Application.Kits.Commands.Update;

public class UpdateKitValidator : AbstractValidator<UpdateKitCommand>
{
    public UpdateKitValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do kit é obrigatório.")
            .MaximumLength(100).WithMessage("O nome não pode exceder 100 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("A descrição é muito longa.");

        RuleFor(x => x.Materials)
            .NotEmpty().WithMessage("Um kit precisa ter pelo menos um material.")
            
            .Must(items => items.Select(i => i.Id).Distinct().Count() == items.Count)
                .When(x => x.Materials is { Count: > 0 })
                .WithMessage("O kit não pode ter materiais duplicados.");

        RuleForEach(x => x.Materials).ChildRules(item =>
        {
            item.RuleFor(m => m.Id)
                .NotEmpty().WithMessage("O ID do material é obrigatório.");

            item.RuleFor(m => m.Quantity)
                .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");
        });
    }
}
