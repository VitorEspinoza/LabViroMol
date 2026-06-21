using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Approve;
using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
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

        _unitOfWork.Received(1)
            .AddPersistentEvent(Arg.Is<ApproveSchedulePersistentEvent>(e =>
                e.SchedulerEmail == schedule.Scheduler.Email &&
                e.SchedulerName == schedule.Scheduler.Name &&
                e.ProjectTitle == schedule.ProjectTitle &&
                e.AdvisorProfessor == schedule.AdvisorProfessor &&
                e.Date == schedule.Scheduling.Date &&
                e.Start == schedule.Scheduling.StartDateHour &&
                e.End == schedule.Scheduling.EndDateHour));

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

    [Fact]
    public async Task Handle_WhenConflictRefuseFails_ShouldNotPersistAnything()
    {
        var schedule = Fakers.CreateSchedule();
        var pastDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));
        var pastScheduling = new Domain.Schedules.Scheduling(
            pastDate,
            new DateTimeOffset(pastDate.ToDateTime(new TimeOnly(9, 0))),
            new DateTimeOffset(pastDate.ToDateTime(new TimeOnly(10, 0))));

        var conflict = Fakers.CreateSchedule(scheduling: pastScheduling);

        _scheduleRepository
            .GetByIdAsync(schedule.Id.Value, Arg.Any<CancellationToken>())
            .Returns(schedule);

        _scheduleRepository
            .GetSchedulesConflictAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<List<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Schedule> { conflict });

        var command = new ApproveScheduleCommand(schedule.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ScheduleStatus.PENDING, conflict.Status);

        _unitOfWork.DidNotReceive().AddPersistentEvent(Arg.Any<ApproveSchedulePersistentEvent>());
        _unitOfWork.DidNotReceive().AddPersistentEvent(Arg.Any<ReprovedSchedulePersistentEvent>());
        await _unitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenConflictRefuseSucceeds_ShouldPersistApproveAndRefuseEvents()
    {
        var schedule = Fakers.CreateSchedule();
        var conflict = Fakers.CreateSchedule();

        _scheduleRepository
            .GetByIdAsync(schedule.Id.Value, Arg.Any<CancellationToken>())
            .Returns(schedule);

        _scheduleRepository
            .GetSchedulesConflictAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<List<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Schedule> { schedule, conflict });

        var command = new ApproveScheduleCommand(schedule.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ScheduleStatus.REFUSED, conflict.Status);

        _unitOfWork.Received(1)
            .AddPersistentEvent(Arg.Is<ApproveSchedulePersistentEvent>(e => e.SchedulerEmail == schedule.Scheduler.Email));

        _unitOfWork.Received(1)
            .AddPersistentEvent(Arg.Is<ReprovedSchedulePersistentEvent>(e =>
                e.SchedulerEmail == conflict.Scheduler.Email &&
                e.Justification == "Outro agendamento com horário conflitante foi aprovado."));

        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }
}
