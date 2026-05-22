using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Research.Application.Projects.Integrations;

internal class ProjectIntegrationService(IProjectRepository projectRepository) : IProjectChecker
{
    public async Task<Result> IsEligibleForConsumptionAsync(Guid projectId, UserId projectMemberId, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(ProjectId.From(projectId), ct);

        if (project is null) return Result.InvalidReference("Projeto não encontrado");
        
        var projectElegible = project.Status == ProjectStatus.InProgress;
     
        if (!projectElegible)
            return Result.BusinessRule("Apenas projetos em andamento podem ser atribuídos a baixas no estoque");
        
        var consumerResearcher = project.Members.FirstOrDefault(x => x.Id == ProjectMemberId.From(projectMemberId.Value) && !x.IsDeleted);
       
        if (consumerResearcher is null)
            return Result.BusinessRule("Apenas membros do projeto podem dar baixa no estoque");

        return Result.Success();
    }

    public async Task<Result> IsEligibleForOrdersAsync(Guid projectId, CancellationToken ct)
    {
        var project = await projectRepository.GetByIdAsync(ProjectId.From(projectId), ct);

        if (project is null) return Result.InvalidReference("Projeto não encontrado");

        if (!project.Status.In(ProjectStatus.InProgress, ProjectStatus.Planned))
            return Result.BusinessRule("Apenas projetos planejados ou em andamento podem iniciar pedidos");

        return Result.Success();

    }
}