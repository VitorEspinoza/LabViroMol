using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public class Schedule : AggregateRoot<ScheduleId>
{
    private Schedule() {}

    private Schedule(ScheduleId id, Scheduler scheduler, Scheduling scheduling, bool acceptTerm,
        string advisorProfessor, string projectTitle, string description)
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
        return new Schedule(ScheduleId.New(), scheduler, scheduling, acceptTerm, advisorProfessor, projectTitle, description);
    }

    public void Approve(UserId approvedBy)
    {
        Status = ScheduleStatus.SCHEDULED;
    }

    public void Reject(UserId rejectedBy)
    {
        Status = ScheduleStatus.REFUSED;
    }
}