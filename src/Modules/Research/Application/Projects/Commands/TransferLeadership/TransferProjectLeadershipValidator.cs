namespace LabViroMol.Modules.Research.Application.Projects.Commands.TransferLeadership;

using FluentValidation;

public class TransferProjectLeadershipValidator : AbstractValidator<TransferProjectLeadershipCommand>
{
    public TransferProjectLeadershipValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.NewLeadResearcherId).NotEmpty();
        RuleFor(x => x.RequestedById).NotEmpty();
    }
}
