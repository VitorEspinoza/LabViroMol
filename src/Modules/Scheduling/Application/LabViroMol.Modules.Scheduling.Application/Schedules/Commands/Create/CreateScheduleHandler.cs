using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;

public class CreateScheduleHandler : ICommandHandler<CreateScheduleCommand, Result>
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;

    public CreateScheduleHandler(
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
            command.Scheduling.Start,
            command.Scheduling.End);
    
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
        await _scheduleRepository.AddAsync(schedule, ct);
        await _unitOfWork.CompleteAsync(ct);
    }
}