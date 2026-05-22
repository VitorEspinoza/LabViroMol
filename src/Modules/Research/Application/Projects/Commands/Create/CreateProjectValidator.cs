namespace LabViroMol.Modules.Research.Application.Projects.Commands.Create;

using FluentValidation;

public class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.PrincipalInvestigatorId)
            .NotEmpty().WithMessage("O Investigador Principal e obrigatorio.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MinimumLength(3).WithMessage("O título deve ter ao menos 3 caracteres.")
            .MaximumLength(200).WithMessage("O título não pode exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("A descrição não pode exceder 2000 caracteres.");

        RuleFor(x => x.PartnerId)
            .NotEmpty().WithMessage("O parceiro é obrigatório.");
    }
}
