using FluentValidation;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.Create;

public class CreateEquipmentValidator : AbstractValidator<CreateEquipmentCommand>
{
    public CreateEquipmentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome do equipamento é obrigatório")
            .MaximumLength(100).WithMessage("O nome do equipamento não pode exceder 100 caracteres");

        RuleFor(x => x.Brand)
            .NotEmpty().WithMessage("Marca é obrigatória")
            .MaximumLength(100).WithMessage("A marca não pode exceder 100 caracteres");

        RuleFor(x => x.Model)
            .NotEmpty().WithMessage("Modelo é obrigatório")
            .MaximumLength(100).WithMessage("O modelo não pode exceder 100 caracteres");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Código é obrigatório")
            .MaximumLength(50).WithMessage("O código não pode exceder 50 caracteres");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .MaximumLength(1000).WithMessage("A descrição não pode exceder 1000 caracteres");
    }
}