using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;
using LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;
using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Contracts;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using NSubstitute;
using Xunit;

namespace LabViroMol.Modules.Scheduling.Application.UnitTests.Schedules.Commands.Create;

public class CreateScheduleCommandHandlerTests
{
    private readonly IScheduleRepository _scheduleRepository = Substitute.For<IScheduleRepository>();
    private readonly ISchedulingUnitOfWork _unitOfWork = Substitute.For<ISchedulingUnitOfWork>();
    private readonly CreateScheduleCommandHandler _handler;

    public CreateScheduleCommandHandlerTests()
    {
        _handler = new CreateScheduleCommandHandler(_scheduleRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenCreateSucceeds_ShouldPersistEmailAndNotificationEvents()
    {
        var date = DateOnly.FromDateTime(DateTime.Now.AddDays(2));
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(9, 0)));
        var end = new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)));
        var equipmentId = Guid.NewGuid();

        _scheduleRepository
            .GetSchedulesConflictAsync(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<List<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<Schedule>());

        var command = new CreateScheduleCommand(
            new SchedulerInput("Maria Silva", "Biomedicina", "maria.silva@test.com"),
            new SchedulingInput(date, start, end),
            true,
            "Prof. João",
            "Projeto X",
            "Descrição do projeto",
            new List<ScheduleEquipmentInput> { new(equipmentId, "Microscópio") });

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _unitOfWork.Received(1)
            .AddPersistentEvent(Arg.Is<CreateScheduleEmailPersistentEvent>(e =>
                e.SchedulerEmail == command.Scheduler.Email &&
                e.SchedulerName == command.Scheduler.Name &&
                e.ProjectTitle == command.ProjectTitle &&
                e.Date == date &&
                e.Start == start &&
                e.End == end));

        _unitOfWork.Received(1)
            .AddPersistentEvent(Arg.Is<CreateScheduleNotificationPersistentEvent>(e =>
                e.SchedulerName == command.Scheduler.Name &&
                e.Date == date &&
                e.Start == start &&
                e.End == end &&
                e.Equipments.Count == 1 &&
                e.Equipments.Single().EquipmentId == equipmentId));

        await _scheduleRepository.Received(1).AddAsync(Arg.Any<Schedule>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }
}
