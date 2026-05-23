using FluentValidation;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Cancel;

public class CancelMaintenanceRequestValidator : AbstractValidator<CancelMaintenanceRequestCommand>
{
    public CancelMaintenanceRequestValidator()
    {
        RuleFor(x => x.MaintenanceRequestId)
            .NotEmpty().WithMessage("Id da solicitação é obrigatório");
    }
}