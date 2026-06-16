using FluentValidation;

namespace LabViroMol.Modules.Identity.Application.Roles.DeleteRole;

public class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("O identificador do perfil é obrigatório.");
    }
}
