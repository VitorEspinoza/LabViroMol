namespace LabViroMol.Modules.Research.Infrastructure.Projects;

using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

internal class ProjectIntegrationService(IProjectRepository projectRepository) : IProjectChecker
{
    public async Task<Result> IsEligibleForConsumptionAsync(Guid projectId, UserId projectMemberId, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(ProjectId.From(projectId), ct);

        if (project is null) return Result.InvalidReference("Projeto não encontrado");

        var projectElegible = project.Status == ProjectStatus.InProgress;

        if (!projectElegible)
            return Result.BusinessRule("Apenas projetos em andamento podem ser atribuídos a baixas no estoque");

        var consumerResearcher = project.Members.FirstOrDefault(x => x.ResearcherId == ResearcherId.From(projectMemberId.Value) && x.IsActive);

        if (consumerResearcher is null)
            return Result.BusinessRule("Apenas membros do projeto podem dar baixa no estoque");

        return Result.Success();
    }

    public async Task<Result> IsEligibleForOrdersAsync(Guid projectId, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(ProjectId.From(projectId), ct);

        if (project is null) return Result.InvalidReference("Projeto não encontrado");

        if (project.Status is not (ProjectStatus.InProgress or ProjectStatus.Planned))
            return Result.BusinessRule("Apenas projetos planejados ou em andamento podem iniciar pedidos");

        return Result.Success();

    }
}
