using FluentValidation;

namespace LabViroMol.Modules.Assets.Application.MaintenanceRequests.Commands.Start;

public class StartMaintenanceRequestValidator : AbstractValidator<StartMaintenanceRequestCommand>
{
    public StartMaintenanceRequestValidator()
    {
        RuleFor(x => x.MaintenanceRequestId)
            .NotEmpty().WithMessage("Id da solicitação é obrigatório");
    }
}