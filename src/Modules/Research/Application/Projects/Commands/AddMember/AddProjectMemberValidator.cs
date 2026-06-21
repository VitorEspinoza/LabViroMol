namespace LabViroMol.Modules.Research.Application.Projects.Commands.AddMember;

using FluentValidation;
using LabViroMol.Modules.Research.Domain.Projects;

public class AddProjectMemberValidator : AbstractValidator<AddProjectMemberCommand>
{
    public AddProjectMemberValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ResearcherId).NotEmpty();
        RuleFor(x => x.RequestedById).NotEmpty();

        RuleFor(x => x.Role)
            .NotEmpty()
            .WithMessage("Role é obrigatório.")
            .Must(BeAValidProjectRole)
            .WithMessage(x => $"'{x.Role}' não é um ProjectRole válido. Opções: {string.Join(", ", Enum.GetNames<ProjectRole>())}");
    }

    private static bool BeAValidProjectRole(string role)
        => !string.IsNullOrWhiteSpace(role) && Enum.TryParse<ProjectRole>(role, out _);
}
