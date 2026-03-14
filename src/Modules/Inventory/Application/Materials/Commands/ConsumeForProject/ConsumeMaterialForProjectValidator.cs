using FluentValidation;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.ConsumeForProject;

public class ConsumeMaterialForProjectValidator : AbstractValidator<ConsumeMaterialForProjectCommand>
{
    public ConsumeMaterialForProjectValidator()
    {
        RuleFor(x => x.MaterialId)
            .NotEmpty().WithMessage("O ID do material é obrigatório.");

        RuleFor(x => x.MaterialId)
            .NotEmpty().WithMessage("O ID do projeto é obrigatório.");
        
        RuleFor(x => x.Quantity.Value)
            .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");
    }
}
