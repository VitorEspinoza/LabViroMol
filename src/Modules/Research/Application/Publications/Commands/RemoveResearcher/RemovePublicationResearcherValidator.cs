namespace LabViroMol.Modules.Research.Application.Publications.Commands.RemoveResearcher;

using FluentValidation;

public class RemovePublicationResearcherValidator : AbstractValidator<RemovePublicationResearcherCommand>
{
    public RemovePublicationResearcherValidator()
    {
        RuleFor(x => x.PublicationId).NotEmpty();
        RuleFor(x => x.ResearcherId).NotEmpty();
    }
}
