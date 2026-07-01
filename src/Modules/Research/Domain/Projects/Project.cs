using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Projects;

public class Project : AggregateRoot<ProjectId>, ICreationAuditable, IModificationAuditable, ITranslatable<ProjectTranslation>
{
    private Project() { }

    private Project(ProjectId id, string title, string description, PartnerId partnerId)
        : base(id)
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
    public Dictionary<string, ProjectTranslation> Translations { get; private set; } = new();

    private readonly List<ProjectMember> _members = new();
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    private bool IsResearchLead(ResearcherId id)
        => _members.Any(m => m.ResearcherId == id && m.IsActive && m.Role == ProjectRole.ResearchLead);

    private bool HasAdministrativePrivileges(ResearcherId id)
        => _members.Any(m => m.ResearcherId == id && m.IsActive &&
            (m.Role == ProjectRole.ResearchLead || m.Role == ProjectRole.Manager));

    public static Result<Project> Create(ResearcherId principalInvestigatorId,
        string title, string description, PartnerId partnerId)
    {
        var project = new Project(IdFactory.New<ProjectId>(), title, description, partnerId);
        project._members.Add(new ProjectMember(principalInvestigatorId, ProjectRole.ResearchLead));
        return Result<Project>.Success(project);
    }

    public Result Start(ResearcherId requestedBy)
    {
        if (!IsResearchLead(requestedBy))
            return Result.BusinessRule("Apenas o líder de pesquisa pode alterar o status do projeto.");
        if (Status != ProjectStatus.Planned)
            return Result.BusinessRule("O projeto só pode ser iniciado a partir do status Planejado.");

        Status = ProjectStatus.InProgress;
        return Result.Success();
    }

    public Result Complete(ResearcherId requestedBy)
    {
        if (!IsResearchLead(requestedBy))
            return Result.BusinessRule("Apenas o lider de pesquisa pode alterar o status do projeto.");
        if (Status != ProjectStatus.InProgress)
            return Result.BusinessRule("O projeto só pode ser concluído a partir do status Em Andamento.");

        Status = ProjectStatus.Completed;
        return Result.Success();
    }

    public Result Cancel(ResearcherId requestedBy)
    {
        if (!IsResearchLead(requestedBy))
            return Result.BusinessRule("Apenas o lider de pesquisa pode alterar o status do projeto.");
        if (Status is ProjectStatus.Completed or ProjectStatus.Canceled)
            return Result.BusinessRule("Projetos concluídos ou cancelados não podem ter seu status alterado.");

        Status = ProjectStatus.Canceled;
        return Result.Success();
    }

    public Result Update(string title, string description, ResearcherId requestedBy)
    {
        if (!HasAdministrativePrivileges(requestedBy))
            return Result.BusinessRule("Apenas Lider ou Gerente podem editar o projeto.");

        Title = Guard.AgainstMinLength(title, 3, "O titulo deve ter ao menos 3 caracteres.");
        Description = Guard.AgainstMinLength(description, 3, "A descrição deve ter ao menos 3 caracteres.");

        return Result.Success();
    }

    public Result AddMember(ResearcherId researcherId, ProjectRole role, ResearcherId requestedBy)
    {
        if (!HasAdministrativePrivileges(requestedBy))
            return Result.BusinessRule("Apenas Líder ou Gerente podem gerenciar membros.");

        if (_members.Any(m => m.ResearcherId == researcherId && m.IsActive))
            return Result.Conflict("O pesquisador já é um membro ativo deste projeto.");

        if (role == ProjectRole.ResearchLead && _members.Any(m => m.IsActive && m.Role == ProjectRole.ResearchLead))
            return Result.Conflict("O projeto já possui um líder de pesquisa ativo.");

        _members.Add(new ProjectMember(researcherId, role));
        return Result.Success();
    }

    public Result TransferLeadership(ResearcherId newLeadId, ResearcherId requestedBy)
    {
        if (!IsResearchLead(requestedBy))
            return Result.BusinessRule("Apenas o lider atual pode transferir a lideranca do projeto.");

        var newLead = _members.FirstOrDefault(m => m.ResearcherId == newLeadId && m.IsActive);
        if (newLead is null)
            return Result.NotFound("Membro nao encontrado no projeto.");

        if (newLead.Role == ProjectRole.ResearchLead)
            return Result.Success();

        var currentLead = _members.First(m => m.ResearcherId == requestedBy && m.IsActive);
        currentLead.UpdateRole(ProjectRole.Manager);
        newLead.UpdateRole(ProjectRole.ResearchLead);

        return Result.Success();
    }

    public Result ChangeMemberRole(ResearcherId researcherId, ProjectRole newRole, ResearcherId requestedBy)
    {
        if (!HasAdministrativePrivileges(requestedBy))
            return Result.BusinessRule("Apenas Lider ou Gerente podem gerenciar membros do projeto.");

        if (newRole == ProjectRole.ResearchLead)
            return Result.BusinessRule("Use TransferLeadership para promover um membro a lider de pesquisa.");

        var member = _members.FirstOrDefault(m => m.ResearcherId == researcherId && m.IsActive);
        if (member is null)
            return Result.NotFound("Membro nao encontrado no projeto.");

        if (member.Role == newRole)
            return Result.Success();

        if (member.Role == ProjectRole.ResearchLead)
            return Result.BusinessRule("Nao e possivel rebaixar o lider. Use TransferLeadership para transferir a lideranca primeiro.");

        member.UpdateRole(newRole);
        return Result.Success();
    }

    public Result RemoveMember(ResearcherId researcherId, ResearcherId requestedBy)
    {
        if (!HasAdministrativePrivileges(requestedBy))
            return Result.BusinessRule("Apenas Líder ou Gerente podem gerenciar membros do projeto.");

        var member = _members.FirstOrDefault(m => m.ResearcherId == researcherId && m.IsActive);
        if (member is null)
            return Result.NotFound("Membro não encontrado no projeto.");
        if (member.Role == ProjectRole.ResearchLead)
            return Result.BusinessRule("Não e possivel remover o lider de pesquisa sem substituí-lo.");

        member.RemoveFromProject();
        return Result.Success();
    }

    public void AddTranslation(string languageCode, string title, string description)
    {
        if (string.IsNullOrWhiteSpace(languageCode)) return;

        Translations[languageCode.ToLower()] = new ProjectTranslation(title, description);
    }

    public string GetTitle(string? language)
    {
        if (language == "en"
            && Translations.TryGetValue("en", out var translation)
            && !string.IsNullOrWhiteSpace(translation.Title))
        {
            return translation.Title;
        }

        return Title;
    }

    public string GetDescription(string? language)
    {
        if (language == "en"
            && Translations.TryGetValue("en", out var translation)
            && !string.IsNullOrWhiteSpace(translation.Description))
        {
            return translation.Description;
        }

        return Description;
    }
}
