using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Cancel;

public class CancelScheduleCommandHandler : ICommandHandler<CancelScheduleCommand, Result>
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public CancelScheduleCommandHandler(
        IScheduleRepository scheduleRepository,
        ISchedulingUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IServiceScopeFactory scopeFactory)
    {
        _scheduleRepository = scheduleRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _scopeFactory = scopeFactory;
    }
    
    
    public async ValueTask<Result> Handle(CancelScheduleCommand command, CancellationToken ct)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(command.ScheduleId.Value, ct);
        if (schedule == null)
            return Result.NotFound("Agendamento não encontrado");

        var result = schedule.Cancel(command.Justification, _currentUser.Id);

        if (result.IsFailure)
            return result;
        
        _unitOfWork.AddPersistentEvent(new CancelSchedulePersistentEvent(
            schedule.Scheduler.Email,
            schedule.Scheduler.Name,
            schedule.ProjectTitle,
            schedule.AdvisorProfessor,
            schedule.Scheduling.Date,
            schedule.Scheduling.StartDateHour,
            schedule.Scheduling.EndDateHour,
            command.Justification));
        
        await _unitOfWork.CompleteAsync(ct);
        
        return Result.Success();
    }
}