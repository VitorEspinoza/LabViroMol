using FluentValidation;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Update;

public class UpdateEquipmentValidator : AbstractValidator<UpdateEquipmentCommand>
{
    public UpdateEquipmentValidator()
    {
        RuleFor(x => x.EquipmentId)
            .NotEmpty().WithMessage("Id do equipamento é obrigatório");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MaximumLength(100).WithMessage("Nome deve ter no máximo 100 caracteres");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Modelo é obrigatório")
            .MaximumLength(100).WithMessage("Modelo deve ter no máximo 100 caracteres");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Marca é obrigatória")
            .MaximumLength(100).WithMessage("Marca deve ter no máximo 100 caracteres");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatório")
            .MaximumLength(500).WithMessage("Descrição deve ter no máximo 500 caracteres");
    }
}