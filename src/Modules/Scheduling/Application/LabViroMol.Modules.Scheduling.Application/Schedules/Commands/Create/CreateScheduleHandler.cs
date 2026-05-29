using LabViroMol.Modules.Notify.Contracts;
using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;

public class CreateScheduleHandler : ICommandHandler<CreateScheduleCommand, Result>
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;
    private readonly ISendNotification _sendNotification;

    public CreateScheduleHandler(
        IScheduleRepository scheduleRepository,
        ISchedulingUnitOfWork unitOfWork,
        ISendNotification sendNotification)
    {
        _scheduleRepository = scheduleRepository;
        _unitOfWork = unitOfWork;
        _sendNotification = sendNotification;
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

        await SendNotificationAsync(command, ct);
        
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

    private async Task SendNotificationAsync(CreateScheduleCommand command, CancellationToken ct)
    {
        var equipments = string.Join(", ", 
            command.Equipments.Select(e => e.Name));

        var message = $"""
           Novo agendamento solicitado.

           Solicitante: {command.Scheduler.Name}

           Data: {command.Scheduling.Date:dd/MM/yyyy}
           Horário: {command.Scheduling.Start:HH:mm} às {command.Scheduling.End:HH:mm}

           Equipamentos: {equipments}
           """;

        await _sendNotification.SendNotification(
            "Agendamento solicitado",
            message,
            "f3a7c1d2-8b4e-4c91-a6f7-2d9e5b7f4a13",
            ct);
    }
}