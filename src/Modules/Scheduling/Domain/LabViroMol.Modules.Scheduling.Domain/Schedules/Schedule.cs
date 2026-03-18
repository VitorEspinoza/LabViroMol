using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using LabViroMol.Modules.Shared.Presentation.Extensions;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public class Schedule : AggregateRoot<ScheduleId>
{
    private Schedule() {}

    private Schedule(ScheduleId id, Scheduler scheduler, Scheduling scheduling, bool acceptTerm,
        string advisorProfessor, string projectTitle, string description) : base(id)
    {
        Scheduler = scheduler;
        Scheduling = scheduling;
        AcceptTerm = acceptTerm;
        AdvisorProfessor = advisorProfessor;
        ProjectTitle = projectTitle;
        Description = description;
        Status = ScheduleStatus.PENDING;
    }

    public Scheduler Scheduler { get; private set; }
    public Scheduling Scheduling { get; private set; }
    public bool AcceptTerm { get; private set; }
    public string AdvisorProfessor  { get; private set; }
    public string ProjectTitle  { get; private set; }
    public string Description { get; private set; }
    public ScheduleStatus  Status { get; private set; }
    public UserId? ApprovedBy { get; private set; }
    public UserId? RejectedBy { get; private set; }

    public static Schedule Create(Scheduler scheduler, Scheduling scheduling, bool acceptTerm, string advisorProfessor,
        string projectTitle, string description)
    {
        return new Schedule(IdFactory.New<ScheduleId>(), scheduler, scheduling, acceptTerm, advisorProfessor, projectTitle, description);
    }

    public void Approve(UserId userId)
    {
        if (!Status.Equals(ScheduleStatus.PENDING))
            Result.BusinessRule("Agendamento não está pendente.");

        if (Scheduling.Date.IsBefore(DateOnly.FromDateTime(DateTime.Now)))
            Result.BusinessRule("Não é possível aprovar agendamento com data passada.");
        
        Status = ScheduleStatus.SCHEDULED;
        ApprovedBy = userId;
        MarkAsUpdated(userId);
    }

    public void Refuse(UserId userId)
    {
        if (!Status.Equals(ScheduleStatus.PENDING))
            Result.BusinessRule("Agendamento não está pendente.");

        if (Scheduling.Date.IsBefore(DateOnly.FromDateTime(DateTime.Now)))
            Result.BusinessRule("Não é possível recusar agendamento com data passada.");
        
        Status = ScheduleStatus.REFUSED;
        ApprovedBy = userId;
        MarkAsUpdated(userId);
    }
}