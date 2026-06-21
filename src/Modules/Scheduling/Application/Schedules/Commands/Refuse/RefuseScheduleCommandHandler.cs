using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Refuse;

public class RefuseScheduleCommandHandler : ICommandHandler<RefuseScheduleCommand, Result>
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RefuseScheduleCommandHandler(
        IScheduleRepository scheduleRepository,
        ISchedulingUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _scheduleRepository = scheduleRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async ValueTask<Result> Handle(RefuseScheduleCommand command, CancellationToken ct)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(command.ScheduleId.Value, ct);

        if (schedule is null)
            return Result.NotFound("Agendamento não encontrado.");

        var result = schedule.Refuse(_currentUser.Id, command.Justification);

        if (result.IsFailure)
            return result;

        _unitOfWork.AddPersistentEvent(new ReprovedSchedulePersistentEvent(
            schedule.Scheduler.Email,
            schedule.Scheduler.Name,
            schedule.ProjectTitle,
            schedule.AdvisorProfessor,
            schedule.Scheduling.Date,
            schedule.Scheduling.StartDateHour,
            schedule.Scheduling.EndDateHour,
            command.Justification));

        await _unitOfWork.CompleteAsync(ct);
        return result;
    }
}
