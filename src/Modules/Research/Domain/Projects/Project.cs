using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Projects;

public class Project : AggregateRoot<ProjectId>
{
    private Project() { }

    private Project(ProjectId id, UserId createdBy, string title, string description, PartnerId partnerId)
        : base(id, createdBy)
    {
        Title = Guard.AgainstMinLength(title, 3, "O titulo deve ter ao menos 3 caracteres.");
        Description = Guard.AgainstMinLength(description, 3, "A descrição deve ter ao menos 3 caracteres.");
        PartnerId = partnerId;
        Status = ProjectStatus.Planned;
    }

    public string Title { get; private set; }
    public string Description { get; private set; }
    public ProjectStatus Status { get; private set; }
    public PartnerId PartnerId { get; private set; }

    private readonly List<ProjectMember> _members = new();
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    private bool IsResearchLead(ResearcherId id)
        => _members.Any(m => m.Id == id && m.Role == ProjectRole.ResearchLead);

    private bool HasAdministrativePrivileges(ResearcherId id)
        => _members.Any(m => m.Id == id &&
            m.Role.In(ProjectRole.ResearchLead, ProjectRole.Manager));

    public static Result<Project> Create(UserId createdBy, ResearcherId principalInvestigatorId,
        string title, string description, PartnerId partnerId)
    {
        var project = new Project(IdFactory.New<ProjectId>(), createdBy, title, description, partnerId);
        project._members.Add(new ProjectMember(principalInvestigatorId, ProjectRole.ResearchLead, createdBy));
        return Result<Project>.Success(project);
    }

    public Result Start(ResearcherId requestedBy)
    {
        if (!IsResearchLead(requestedBy))
            return Result.BusinessRule("Apenas o líder de pesquisa pode alterar o status do projeto.");
        if (Status != ProjectStatus.Planned)
            return Result.BusinessRule("O projeto só pode ser iniciado a partir do status Planejado.");

        Status = ProjectStatus.InProgress;
        MarkAsUpdated(UserId.From(requestedBy));
        return Result.Success();
    }

    public Result Complete(ResearcherId requestedBy)
    {
        if (!IsResearchLead(requestedBy))
            return Result.BusinessRule("Apenas o lider de pesquisa pode alterar o status do projeto.");
        if (Status != ProjectStatus.InProgress)
            return Result.BusinessRule("O projeto só pode ser concluído a partir do status Em Andamento.");

        Status = ProjectStatus.Completed;
        MarkAsUpdated(UserId.From(requestedBy));
        return Result.Success();
    }

    public Result Cancel(ResearcherId requestedBy)
    {
        if (!IsResearchLead(requestedBy))
            return Result.BusinessRule("Apenas o lider de pesquisa pode alterar o status do projeto.");
        if (Status.In(ProjectStatus.Completed, ProjectStatus.Canceled))
            return Result.BusinessRule("Projetos concluídos ou cancelados não podem ter seu status alterado.");

        Status = ProjectStatus.Canceled;
        MarkAsUpdated(UserId.From(requestedBy));
        return Result.Success();
    }

    public Result Update(string title, string description, ResearcherId requestedBy)
    {
        if (!HasAdministrativePrivileges(requestedBy))
            return Result.BusinessRule("Apenas Lider ou Gerente podem editar o projeto.");
       
        Title = Guard.AgainstMinLength(title, 3, "O titulo deve ter ao menos 3 caracteres.");
        Description = Guard.AgainstMinLength(description, 3, "A descrição deve ter ao menos 3 caracteres.");

        MarkAsUpdated(UserId.From(requestedBy));
        return Result.Success();
    }

    public Result AddMember(ResearcherId researcherId, ProjectRole role, ResearcherId requestedBy)
    {
        if (!HasAdministrativePrivileges(requestedBy))
            return Result.BusinessRule("Apenas Líder ou Gerente podem gerenciar membros.");

        var existingMember = _members.FirstOrDefault(m => m.Id == researcherId);

        if (existingMember is not null)
        {
            if (!existingMember.IsDeleted)
                return Result.Conflict("O pesquisador já é um membro ativo deste projeto.");

            if (role == ProjectRole.ResearchLead && _members.Any(m => !m.IsDeleted && m.Role == ProjectRole.ResearchLead))
                return Result.Conflict("O projeto já possui um líder de pesquisa ativo.");

            existingMember.UndoRemove(UserId.From(requestedBy.Value));
            existingMember.UpdateRole(role, UserId.From(requestedBy.Value));
            return Result.Success();
        }

        if (role == ProjectRole.ResearchLead && _members.Any(m => !m.IsDeleted && m.Role == ProjectRole.ResearchLead))
            return Result.Conflict("O projeto já possui um líder de pesquisa.");

        _members.Add(new ProjectMember(researcherId, role, UserId.From(requestedBy.Value)));
        MarkAsUpdated(UserId.From(requestedBy));
        return Result.Success();
    }
    public Result TransferLeadership(ResearcherId newLeadId, ResearcherId requestedBy)
    {
        if (!IsResearchLead(requestedBy))
            return Result.BusinessRule("Apenas o lider atual pode transferir a lideranca do projeto.");

        var newLead = _members.FirstOrDefault(m => m.Id == newLeadId && !m.IsDeleted);
        if (newLead is null)
            return Result.NotFound("Membro nao encontrado no projeto.");

        if (newLead.Role == ProjectRole.ResearchLead)
            return Result.Success();

        var currentLead = _members.First(m => m.Id == requestedBy && !m.IsDeleted);
        currentLead.UpdateRole(ProjectRole.Manager, UserId.From(requestedBy));
        newLead.UpdateRole(ProjectRole.ResearchLead, UserId.From(requestedBy));

        MarkAsUpdated(UserId.From(requestedBy));
        return Result.Success();
    }

    public Result ChangeMemberRole(ResearcherId researcherId, ProjectRole newRole, ResearcherId requestedBy)
    {
        if (!HasAdministrativePrivileges(requestedBy))
            return Result.BusinessRule("Apenas Lider ou Gerente podem gerenciar membros do projeto.");

        if (newRole == ProjectRole.ResearchLead)
            return Result.BusinessRule("Use TransferLeadership para promover um membro a lider de pesquisa.");

        var member = _members.FirstOrDefault(m => m.Id == researcherId && !m.IsDeleted);
        if (member is null)
            return Result.NotFound("Membro nao encontrado no projeto.");

        if (member.Role == newRole)
            return Result.Success();

        if (member.Role == ProjectRole.ResearchLead)
            return Result.BusinessRule("Nao e possivel rebaixar o lider. Use TransferLeadership para transferir a lideranca primeiro.");

        member.UpdateRole(newRole, UserId.From(requestedBy.Value));
        MarkAsUpdated(UserId.From(requestedBy));
        return Result.Success();
    }

    public Result RemoveMember(ResearcherId researcherId, ResearcherId requestedBy)
    {
        if (!HasAdministrativePrivileges(requestedBy))
            return Result.BusinessRule("Apenas Líder ou Gerente podem gerenciar membros do projeto.");

        var member = _members.FirstOrDefault(m => m.Id == researcherId);
        if (member is null)
            return Result.NotFound("Membro não encontrado no projeto.");
        if (member.Role == ProjectRole.ResearchLead)
            return Result.BusinessRule("Não e possivel remover o lider de pesquisa sem substituí-lo.");
        
        member.MarkAsRemoved(UserId.From((requestedBy)));
        MarkAsUpdated(UserId.From(requestedBy));
        return Result.Success();
    }
    
}
