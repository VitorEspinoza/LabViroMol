namespace LabViroMol.Modules.Research.Application.Partners.Commands.Update;

using FluentValidation;

public class UpdatePartnerValidator : AbstractValidator<UpdatePartnerCommand>
{
    public UpdatePartnerValidator()
    {
        RuleFor(x => x.PartnerId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("O nome do parceiro e obrigatorio.")
            .MinimumLength(3).WithMessage("O nome do parceiro deve ter ao menos 3 caracteres.")
            .MaximumLength(200).WithMessage("O nome do parceiro nao pode exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("A descricao nao pode exceder 2000 caracteres.");
    }
}
