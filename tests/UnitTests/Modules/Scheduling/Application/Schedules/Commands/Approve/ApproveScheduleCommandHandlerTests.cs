using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Approve;
using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using NSubstitute;
using Xunit;

namespace LabViroMol.Modules.Scheduling.Application.UnitTests.Schedules.Commands.Approve;

public class ApproveScheduleCommandHandlerTests
{
    private readonly IScheduleRepository _scheduleRepository = Substitute.For<IScheduleRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ApproveScheduleCommandHandler _handler;

    public ApproveScheduleCommandHandlerTests()
    {
        _currentUser.Id.Returns(Fakers.AnyUserId());
        _handler = new ApproveScheduleCommandHandler(_scheduleRepository, _unitOfWork, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenApproveFails_ShouldNotRefuseConflictsNorPersist()
    {
        // Arrange: schedule já aprovado anteriormente, então Approve() falha (não está PENDING)
        var schedule = Fakers.CreateSchedule();
        schedule.Approve(Fakers.AnyUserId());

        _scheduleRepository
            .GetByIdAsync(schedule.Id.Value, Arg.Any<CancellationToken>())
            .Returns(schedule);

        var command = new ApproveScheduleCommand(schedule.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);

        await _scheduleRepository
            .DidNotReceive()
            .GetSchedulesConflictAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<List<Guid>>(),
                Arg.Any<CancellationToken>());

        await _unitOfWork
            .DidNotReceive()
            .CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenApproveSucceeds_ShouldRefuseConflictsAndPersist()
    {
        // Arrange: schedule recém-criado, ainda PENDING e com data futura -> Approve() sucede
        var schedule = Fakers.CreateSchedule();

        _scheduleRepository
            .GetByIdAsync(schedule.Id.Value, Arg.Any<CancellationToken>())
            .Returns(schedule);

        _scheduleRepository
            .GetSchedulesConflictAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<List<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Schedule>());

        var command = new ApproveScheduleCommand(schedule.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(ScheduleStatus.SCHEDULED, schedule.Status);

        await _scheduleRepository
            .Received(1)
            .GetSchedulesConflictAsync(
                schedule.Scheduling.StartDateHour,
                schedule.Scheduling.EndDateHour,
                Arg.Any<List<Guid>>(),
                Arg.Any<CancellationToken>());

        await _unitOfWork
            .Received(1)
            .CompleteAsync(Arg.Any<CancellationToken>());
    }
}
