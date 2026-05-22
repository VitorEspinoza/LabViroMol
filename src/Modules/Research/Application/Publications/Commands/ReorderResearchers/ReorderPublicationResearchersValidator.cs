namespace LabViroMol.Modules.Research.Application.Publications.Commands.ReorderResearchers;

using FluentValidation;

public class ReorderPublicationResearchersValidator : AbstractValidator<ReorderPublicationResearchersCommand>
{
    public ReorderPublicationResearchersValidator()
    {
        RuleFor(x => x.PublicationId).NotEmpty();
        RuleFor(x => x.ResearcherIds).NotNull();
    }
}
