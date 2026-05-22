namespace LabViroMol.Modules.Research.Application.Publications.Commands.AssignDoi;

using FluentValidation;

public class AssignPublicationDoiValidator : AbstractValidator<AssignPublicationDoiCommand>
{
    public AssignPublicationDoiValidator()
    {
        RuleFor(x => x.PublicationId).NotEmpty();
        RuleFor(x => x.Doi)
            .NotEmpty().WithMessage("O DOI e obrigatorio.")
            .MaximumLength(200).WithMessage("O DOI nao pode exceder 200 caracteres.");
    }
}
