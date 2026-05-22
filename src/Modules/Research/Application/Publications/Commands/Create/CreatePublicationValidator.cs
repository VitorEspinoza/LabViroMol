namespace LabViroMol.Modules.Research.Application.Publications.Commands.Create;

using FluentValidation;

public class CreatePublicationValidator : AbstractValidator<CreatePublicationCommand>
{
    public CreatePublicationValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O titulo da publicacao e obrigatorio.")
            .MinimumLength(3).WithMessage("O titulo deve ter ao menos 3 caracteres.")
            .MaximumLength(500).WithMessage("O titulo nao pode exceder 500 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("A descricao nao pode exceder 5000 caracteres.");

        RuleFor(x => x.Doi)
            .MaximumLength(200).WithMessage("O DOI nao pode exceder 200 caracteres.");

        RuleFor(x => x.PublishedOn)
            .NotEmpty().WithMessage("O local de publicacao e obrigatorio.")
            .MaximumLength(500).WithMessage("O local de publicacao nao pode exceder 500 caracteres.");

        RuleFor(x => x.PublishUrl)
            .MaximumLength(2000).WithMessage("A URL nao pode exceder 2000 caracteres.");

        RuleFor(x => x.PublicationDate)
            .NotEmpty().WithMessage("A data de publicacao e obrigatoria.");
    }
}
