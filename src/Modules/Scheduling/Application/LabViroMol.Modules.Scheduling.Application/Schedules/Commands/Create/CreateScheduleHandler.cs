using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
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
        var conflictingSchedules =
            await _scheduleRepository.GetSchedulesConflictByDatesAsync(command.Scheduling.Start, command.Scheduling.End, ct);

        if (conflictingSchedules.Count > 0)
            return Result.BusinessRule(
                $"Não é possível solicitar o agendamento, pois possui horário conflitante com outros agendamentos confirmados");

        var resultScheduling = Domain.Schedules.Scheduling.Create(
            command.Scheduling.Date,
            command.Scheduling.Start,
            command.Scheduling.End);
        
        if(resultScheduling.IsFailure)
            return resultScheduling;
        
        var result = Schedule.Create(
            new Scheduler(
                command.Scheduler.Name,
                command.Scheduler.Course,
                command.Scheduler.Email), 
            resultScheduling.Data!, 
            command.AcceptTerm, 
            command.AdvisorProfessor, 
            command.ProjectTitle, 
            command.Description);

        if (result.IsFailure)
            return result;
            
        await _scheduleRepository.AddAsync(result.Data!, ct);
        await _unitOfWork.CompleteAsync(ct);
        
        return Result.Success();
    }
}