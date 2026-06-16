using LabViroMol.Modules.Scheduling.Domain.Schedules.Events;
using LabViroMol.Modules.Shared.Kernel.Extensions;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public class Schedule : AggregateRoot<ScheduleId>, IModificationAuditable
{
    private Schedule()
    {
    }

    private Schedule(ScheduleId id, Scheduler scheduler, Scheduling scheduling, bool acceptTerm,
        string advisorProfessor, string projectTitle, string description, List<ScheduleEquipment> equipments) : base(id)
    {
        Scheduler = scheduler;
        Scheduling = scheduling;
        AcceptTerm = acceptTerm;
        AdvisorProfessor = advisorProfessor;
        ProjectTitle = projectTitle;
        Description = description;
        Status = ScheduleStatus.PENDING;
        Equipments = equipments;
        AddEvent(new NewScheduleDomainEvent(this));
    }

    public Scheduler Scheduler { get; private set; }
    public Scheduling Scheduling { get; private set; }
    public bool AcceptTerm { get; private set; }
    public string AdvisorProfessor { get; private set; }
    public string ProjectTitle { get; private set; }
    public string Description { get; private set; }
    public ScheduleStatus Status { get; private set; }
    public List<ScheduleEquipment> Equipments { get; private set; }
    public UserId? ApprovedBy { get; private set; }
    public UserId? RefusedBy { get; private set; }
    public string TermUrl { get; private set; }
    public string RefuseJustification { get; private set; }

public static Result<Schedule> Create(Scheduler scheduler, Scheduling scheduling, bool acceptTerm, string advisorProfessor,
        string projectTitle, string description, List<ScheduleEquipment> equipments)
    {
        if (equipments == null || equipments.Count < 1)
            return Result<Schedule>.BusinessRule("É necessário informar ao menos um equipamento");

        if (equipments.Select(e => e.EquipmentId).Distinct().Count() != equipments.Count)
            return Result<Schedule>.BusinessRule("Não é permitido equipamentos duplicados");

        var schedule = new Schedule(IdFactory.New<ScheduleId>(), scheduler, scheduling, acceptTerm, advisorProfessor, projectTitle, description, equipments);
        return Result<Schedule>.Success(schedule);
    }

    public Result Approve(UserId userId)
    {
        var valid = EnsureCanBeApprovedOrRefused();

        if(valid.IsFailure)
            return valid;

        Status = ScheduleStatus.SCHEDULED;
        ApprovedBy = userId;
        AddEvent(new ApprovedScheduleDomainEvent(this));
        return Result.Success();
    }

    public Result Refuse(UserId userId, string justification)
    {
        var valid = EnsureCanBeApprovedOrRefused();

        if(valid.IsFailure)
            return valid;

        Status = ScheduleStatus.REFUSED;
        RefusedBy = userId;
        RefuseJustification = justification;
        AddEvent(new ReprovedScheduleDomainEvent(this, justification));
        return Result.Success();
    }

    private Result EnsureCanBeApprovedOrRefused()
    {
        if (!Status.Equals(ScheduleStatus.PENDING))
            return Result.BusinessRule("Agendamento não está pendente.");

        if (Scheduling.Date.IsBefore(DateOnly.FromDateTime(DateTime.Now)))
            return Result.BusinessRule("Não é possível alterar agendamento com data passada.");

        return Result.Success();
    }

    public Result Cancel(string justification, UserId userId)
    {
        if (Status.Equals(ScheduleStatus.CANCELED))
            return Result.BusinessRule("Agendamento já cancelado.");
        
        if (Status.Equals(ScheduleStatus.REFUSED))
            return Result.BusinessRule("Agendamento reprovado não pode ser cancelado.");
        
        
        Status = ScheduleStatus.CANCELED;
        RefusedBy = userId;
        RefuseJustification =  justification;
        AddEvent(new CanceledScheduleDomainEvent(this,  justification));
        return  Result.Success();
    }
    
    public void AttachTermUrl(string url)
    {
        TermUrl = url;
    }
}