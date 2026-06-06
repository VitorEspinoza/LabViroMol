using FluentValidation;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Refuse;

public class RefuseScheduleValidator : AbstractValidator<RefuseScheduleCommand>
{
    public RefuseScheduleValidator()
    {
        RuleFor(x => x.Justification)
            .NotEmpty().WithMessage("Justificativa é obrigatória para reprovação")
            .MinimumLength(20).WithMessage("Justificativa deve conter no mínimo 20 caracteres");
    }
}