namespace LabViroMol.Modules.Research.Application.Publications.Commands.AddResearcher;

using FluentValidation;

public class AddPublicationResearcherValidator : AbstractValidator<AddPublicationResearcherCommand>
{
    public AddPublicationResearcherValidator()
    {
        RuleFor(x => x.PublicationId).NotEmpty();
        RuleFor(x => x.ResearcherId).NotEmpty();
    }
}
