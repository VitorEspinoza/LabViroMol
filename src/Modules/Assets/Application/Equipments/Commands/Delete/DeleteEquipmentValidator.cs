using FluentValidation;
using LabViroMol.Modules.Assets.Domain.Equipments;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Delete;

public class DeleteEquipmentValidator : AbstractValidator<DeleteEquipmentCommand>
{
    public DeleteEquipmentValidator()
    {
        RuleFor(x => x.EquipmentId)
            .NotEmpty().WithMessage("Id do equipamento é obrigatório");
    }
}