namespace LabViroMol.Modules.Research.Application.Projects.Commands.Update;

using FluentValidation;

public class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.RequestedById).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O titulo e obrigatorio.")
            .MinimumLength(3).WithMessage("O titulo deve ter ao menos 3 caracteres.")
            .MaximumLength(200).WithMessage("O titulo nao pode exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("A descricao nao pode exceder 2000 caracteres.");
    }
}
