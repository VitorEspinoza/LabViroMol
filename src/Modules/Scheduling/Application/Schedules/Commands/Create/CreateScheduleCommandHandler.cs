using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;

public class CreateScheduleCommandHandler : ICommandHandler<CreateScheduleCommand, Result>
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public CreateScheduleCommandHandler(
        IScheduleRepository scheduleRepository,
        ISchedulingUnitOfWork unitOfWork)
    {
        _scheduleRepository = scheduleRepository;
        _unitOfWork = unitOfWork;
    }
    
    public async ValueTask<Result> Handle(CreateScheduleCommand command, CancellationToken ct)
    {
        var equipmentIds = GetEquipmentIds(command);

        if (await HasConflictAsync(command, equipmentIds, ct))
            return ConflictResult();

        var schedulingResult = CreateScheduling(command);
        if (schedulingResult.IsFailure)
            return schedulingResult;

        var scheduleResult = CreateSchedule(command, schedulingResult.Data!);
        if (scheduleResult.IsFailure)
            return scheduleResult;

        await PersistAsync(scheduleResult.Data!, ct);
        
        return Result.Success();
    }
    
    private List<Guid> GetEquipmentIds(CreateScheduleCommand command) =>
        command.Equipments.Select(e => e.EquipmentId).ToList();
    
    private async Task<bool> HasConflictAsync(
        CreateScheduleCommand command,
        List<Guid> equipmentIds,
        CancellationToken ct)
    {
        var conflicts = await _scheduleRepository.GetSchedulesConflictAsync(
            command.Scheduling.Start,
            command.Scheduling.End,
            equipmentIds,
            ct);

        return conflicts.Count > 0;
    }
    
    private Result ConflictResult() =>
        Result.BusinessRule("Não é possível solicitar o agendamento, pois possui horário conflitante com outros agendamentos confirmados");
    
    private Result<Domain.Schedules.Scheduling> CreateScheduling(CreateScheduleCommand command) =>
        Domain.Schedules.Scheduling.Create(
            command.Scheduling.Date,
            command.Scheduling.Start.ToUniversalTime(),
            command.Scheduling.End.ToUniversalTime());
    
    private Result<Schedule> CreateSchedule(
        CreateScheduleCommand command,
        Domain.Schedules.Scheduling scheduling)
    {
        var equipments = command.Equipments
            .Select(e => new ScheduleEquipment(e.EquipmentId, e.Name))
            .ToList();

        return Schedule.Create(
            new Scheduler(
                command.Scheduler.Name,
                command.Scheduler.Course,
                command.Scheduler.Email),
            scheduling,
            command.AcceptTerm,
            command.AdvisorProfessor,
            command.ProjectTitle,
            command.Description,
            equipments);
    }
    
    private async Task PersistAsync(Schedule schedule, CancellationToken ct)
    {
        _unitOfWork.AddPersistentEvent(new CreateScheduleNotificationPersistentEvent(
            schedule.Id,
            schedule.Scheduler.Name,
            schedule.Scheduling.Date,
            schedule.Scheduling.StartDateHour,
            schedule.Scheduling.EndDateHour,
            schedule.Equipments));
        
        await _scheduleRepository.AddAsync(schedule, ct);
        await _unitOfWork.CompleteAsync(ct);
    }
}