namespace LabViroMol.Modules.Research.Application.Projects.Commands.ChangeMemberRole;

using FluentValidation;
using LabViroMol.Modules.Research.Domain.Projects;

public class ChangeProjectMemberRoleValidator : AbstractValidator<ChangeProjectMemberRoleCommand>
{
    public ChangeProjectMemberRoleValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ResearcherId).NotEmpty();
        RuleFor(x => x.RequestedById).NotEmpty();
        RuleFor(x => x.NewRole)
            .NotEmpty().WithMessage("O papel e obrigatorio.")
            .Must(BeAValidProjectRole)
            .WithMessage(x => $"'{x.NewRole}' nao e um ProjectRole valido. Opcoes: {string.Join(", ", Enum.GetNames<ProjectRole>())}");
    }

    private static bool BeAValidProjectRole(string role)
        => !string.IsNullOrWhiteSpace(role) && Enum.TryParse<ProjectRole>(role, out _);
}
