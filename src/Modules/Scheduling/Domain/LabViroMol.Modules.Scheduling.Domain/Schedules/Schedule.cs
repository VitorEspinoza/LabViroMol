using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using LabViroMol.Modules.Shared.Domain.Extension;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public class Schedule : AggregateRoot<ScheduleId>
{
    private Schedule() {}

    private Schedule(ScheduleId id, Scheduler scheduler, Scheduling scheduling, bool acceptTerm,
        string advisorProfessor, string projectTitle, string description, List<Equipment> equipments) : base(id)
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
    public string AdvisorProfessor  { get; private set; }
    public string ProjectTitle  { get; private set; }
    public string Description { get; private set; }
    public ScheduleStatus  Status { get; private set; }
    public List<Equipment> Equipments { get; private set; }
    public UserId? ApprovedBy { get; private set; }
    public UserId? RefusedBy { get; private set; }

    public static Result<Schedule> Create(Scheduler scheduler, Scheduling scheduling, bool acceptTerm, string advisorProfessor,
        string projectTitle, string description, List<Equipment> equipments)
    {
        var schedule = new Schedule(IdFactory.New<ScheduleId>(), scheduler, scheduling, acceptTerm, advisorProfessor, projectTitle, description, equipments);
        return Result<Schedule>.Success(schedule);
    }

    public void Approve(UserId userId)
    {
        EnsureCanBeApprovedOrRefused();
        
        Status = ScheduleStatus.SCHEDULED;
        ApprovedBy = userId;
        MarkAsUpdated(userId);
    }

    public void Refuse(UserId userId)
    {
        EnsureCanBeApprovedOrRefused();
        
        Status = ScheduleStatus.REFUSED;
        RefusedBy = userId;
        MarkAsUpdated(userId);
    }
    
    private void EnsureCanBeApprovedOrRefused()
    {
        if (!Status.Equals(ScheduleStatus.PENDING))
            Result.BusinessRule("Agendamento não está pendente.");

        if (Scheduling.Date.IsBefore(DateOnly.FromDateTime(DateTime.Now)))
            Result.BusinessRule("Não é possível alterar agendamento com data passada.");
    }
}