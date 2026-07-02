using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Approve;

public sealed class ApproveScheduleCommandHandler : ICommandHandler<ApproveScheduleCommand, Result>
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;
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
            return Result.NotFound("Agendamento n�o encontrado.");

        var result = schedule.Approve(_currentUser.Id);

        if (result.IsFailure)
            return result;

        var refuseConflictsResult = await RefuseConflictingSchedules(schedule, ct);
        if (refuseConflictsResult.IsFailure)
            return refuseConflictsResult;

        AddApproveEmailEvent(schedule);
        await _unitOfWork.CompleteAsync(ct);

        return result;
    }

    private async Task<Result> RefuseConflictingSchedules(Schedule schedule, CancellationToken ct)
    {
        var equipmentIds = schedule.Equipments
            .Select(e => e.EquipmentId)
            .ToList();

        var conflicts = await _scheduleRepository.GetSchedulesConflictAsync(
            schedule.Scheduling.StartDateHour,
            schedule.Scheduling.EndDateHour,
            equipmentIds,
            ct);

        const string justification = "Outro agendamento com horário conflitante foi aprovado.";

        foreach (var conflict in conflicts.Where(s =>
                     s.Id != schedule.Id &&
                     s.Status == ScheduleStatus.PENDING))
        {
            var result = conflict.Refuse(_currentUser.Id, justification);
            if (result.IsFailure)
                return result;

            AddRefuseEmailEvent(conflict, justification);
        }

        return Result.Success();
    }

    private void AddApproveEmailEvent(Schedule schedule)
    {
        _unitOfWork.AddPersistentEvent(new ApproveSchedulePersistentEvent(
            schedule.Scheduler.Email,
            schedule.Scheduler.Name,
            schedule.ProjectTitle,
            schedule.AdvisorProfessor,
            schedule.Scheduling.Date,
            schedule.Scheduling.StartDateHour,
            schedule.Scheduling.EndDateHour));
    }

    private void AddRefuseEmailEvent(Schedule schedule, string justification)
    {
        _unitOfWork.AddPersistentEvent(new ReprovedSchedulePersistentEvent(
            schedule.Scheduler.Email,
            schedule.Scheduler.Name,
            schedule.ProjectTitle,
            schedule.AdvisorProfessor,
            schedule.Scheduling.Date,
            schedule.Scheduling.StartDateHour,
            schedule.Scheduling.EndDateHour,
            justification));
    }
}
