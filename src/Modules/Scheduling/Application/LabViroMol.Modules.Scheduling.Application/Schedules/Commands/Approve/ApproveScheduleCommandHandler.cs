using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
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
        
        schedule.Approve(_currentUser.Id);

        await _unitOfWork.CompleteAsync(ct);
        
        return Result.Success();
    }
}