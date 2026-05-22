namespace LabViroMol.Modules.Research.Application.Publications.Commands.Update;

using FluentValidation;

public class UpdatePublicationValidator : AbstractValidator<UpdatePublicationCommand>
{
    public UpdatePublicationValidator()
    {
        RuleFor(x => x.PublicationId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O titulo da publicacao e obrigatorio.")
            .MinimumLength(3).WithMessage("O titulo deve ter ao menos 3 caracteres.")
            .MaximumLength(500).WithMessage("O titulo nao pode exceder 500 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("A descricao nao pode exceder 5000 caracteres.");

        RuleFor(x => x.PublishedOn)
            .NotEmpty().WithMessage("O local de publicacao e obrigatorio.")
            .MaximumLength(500).WithMessage("O local de publicacao nao pode exceder 500 caracteres.");

        RuleFor(x => x.PublishUrl)
            .MaximumLength(2000).WithMessage("A URL nao pode exceder 2000 caracteres.");
    }
}
