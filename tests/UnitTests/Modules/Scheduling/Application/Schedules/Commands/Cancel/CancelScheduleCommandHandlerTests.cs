using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Cancel;
using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using NSubstitute;
using Xunit;

namespace LabViroMol.Modules.Scheduling.Application.UnitTests.Schedules.Commands.Cancel;

public class CancelScheduleCommandHandlerTests
{
    private readonly IScheduleRepository _scheduleRepository = Substitute.For<IScheduleRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly CancelScheduleCommandHandler _handler;

    public CancelScheduleCommandHandlerTests()
    {
        _currentUser.Id.Returns(Fakers.AnyUserId());
        _handler = new CancelScheduleCommandHandler(_scheduleRepository, _unitOfWork, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenCancelSucceeds_ShouldPersistCancelEvent()
    {
        var schedule = Fakers.CreateSchedule();
        const string justification = "Agendador desistiu da reserva.";

        _scheduleRepository
            .GetByIdAsync(schedule.Id.Value, Arg.Any<CancellationToken>())
            .Returns(schedule);

        var result = await _handler.Handle(new CancelScheduleCommand(schedule.Id, justification), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ScheduleStatus.CANCELED, schedule.Status);

        _unitOfWork.Received(1)
            .AddPersistentEvent(Arg.Is<CancelSchedulePersistentEvent>(e =>
                e.SchedulerEmail == schedule.Scheduler.Email &&
                e.SchedulerName == schedule.Scheduler.Name &&
                e.ProjectTitle == schedule.ProjectTitle &&
                e.AdvisorProfessor == schedule.AdvisorProfessor &&
                e.Date == schedule.Scheduling.Date &&
                e.Start == schedule.Scheduling.StartDateHour &&
                e.End == schedule.Scheduling.EndDateHour &&
                e.Justification == justification));

        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }
}
