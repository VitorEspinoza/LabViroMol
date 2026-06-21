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
    }

    public Scheduler Scheduler { get; private set; }
    public Scheduling Scheduling { get; private set; }
    public bool AcceptTerm { get; private set; }
    public string AdvisorProfessor { get; private set; }
    public string ProjectTitle { get; private set; }
    public string Description { get; private set; }
    public ScheduleStatus Status { get; private set; }
    public List<ScheduleEquipment> Equipments { get; private set; } = new();
    public UserId? ApprovedBy { get; private set; }
    public UserId? RefusedBy { get; private set; }
    public string TermUrl { get; private set; }
    public string RefuseJustification { get; private set; }

    public static Result<Schedule> Create(Scheduler scheduler, Scheduling scheduling, bool acceptTerm, string advisorProfessor,
        string projectTitle, string description, List<ScheduleEquipment> equipments)
    {
        if (equipments == null || equipments.Count < 1)
            return Result<Schedule>.BusinessRule("\u00C9 necess\u00E1rio informar ao menos um equipamento");

        if (equipments.Select(e => e.EquipmentId).Distinct().Count() != equipments.Count)
            return Result<Schedule>.BusinessRule("N\u00E3o \u00E9 permitido equipamentos duplicados");

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
        return Result.Success();
    }

    private Result EnsureCanBeApprovedOrRefused()
    {
        if (!Status.Equals(ScheduleStatus.PENDING))
            return Result.BusinessRule("Agendamento n\u00E3o est\u00E1 pendente.");

        if (Scheduling.Date.IsBefore(DateOnly.FromDateTime(DateTimeOffset.Now.DateTime)))
            return Result.BusinessRule("N\u00E3o \u00E9 poss\u00EDvel alterar agendamento com data passada.");

        return Result.Success();
    }

    public Result Cancel(string justification, UserId userId)
    {
        if (Status.Equals(ScheduleStatus.CANCELED))
            return Result.BusinessRule("Agendamento j\u00E1 cancelado.");

        if (Status.Equals(ScheduleStatus.REFUSED))
            return Result.BusinessRule("Agendamento reprovado n\u00E3o pode ser cancelado.");

        Status = ScheduleStatus.CANCELED;
        RefusedBy = userId;
        RefuseJustification =  justification;
        return  Result.Success();
    }

    public void AttachTermUrl(string url)
    {
        TermUrl = url;
    }
}
