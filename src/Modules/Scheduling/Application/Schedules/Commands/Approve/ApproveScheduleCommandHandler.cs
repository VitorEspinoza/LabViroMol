using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Approve;

public class ApproveScheduleCommandHandler : ICommandHandler<ApproveScheduleCommand, Result>
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ISchedulingUnitOfWork  _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ApproveScheduleCommandHandler(
        IScheduleRepository scheduleRepository,
        ISchedulingUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _scheduleRepository = scheduleRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }
    
    public async ValueTask<Result> Handle(ApproveScheduleCommand command, CancellationToken ct)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(command.ScheduleId.Value, ct);
        
        if (schedule is null)
            return Result.NotFound("Agendamento não encontrado.");
        
        var result = schedule.Approve(_currentUser.Id);

        if (result.IsFailure)
            return result;

        await RefuseConflictingSchedules(schedule, ct);
         
        await _unitOfWork.CompleteAsync(ct);
        
        return result;
    }

    private async Task RefuseConflictingSchedules(Schedule schedule, CancellationToken ct)
    {
        var equipmentIds = schedule.Equipments
            .Select(e => e.EquipmentId)
            .ToList();

        var conflicts = await _scheduleRepository.GetSchedulesConflictAsync(
            schedule.Scheduling.StartDateHour,
            schedule.Scheduling.EndDateHour,
            equipmentIds,
            ct);

        foreach (var conflict in conflicts.Where(s => 
                     s.Id != schedule.Id && 
                     s.Status == ScheduleStatus.PENDING))
        {
            conflict.Refuse(_currentUser.Id, "Outro agendamento com horário conflitante foi aprovado.");
        }
    }
}