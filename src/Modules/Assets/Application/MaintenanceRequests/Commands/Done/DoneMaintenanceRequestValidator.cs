using FluentValidation;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Done;

public class DoneMaintenanceRequestValidator : AbstractValidator<DoneMaintenanceRequestCommand>
{
    public DoneMaintenanceRequestValidator()
    {
        RuleFor(x => x.MaintenanceRequestId)
            .NotEmpty().WithMessage("Id da solicitação é obrigatório");
    }
}