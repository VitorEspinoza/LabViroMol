using FluentValidation;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Create;

public class CreateMaintenanceValidator : AbstractValidator<CreateMaintenanceCommand>
{
    public CreateMaintenanceValidator()
    {
        RuleFor(x => x.EquipmentId)
            .NotEmpty().WithMessage("Id do equipamento é obrigatório");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .MaximumLength(1000).WithMessage("A descrição não pode exceder 1000 caracteres");

        RuleFor(x => x.ProblemDescription)
            .NotEmpty().WithMessage("Descrição do problema é obrigatória")
            .MaximumLength(1000).WithMessage("A descrição do problema não pode exceder 1000 caracteres");
    }
}