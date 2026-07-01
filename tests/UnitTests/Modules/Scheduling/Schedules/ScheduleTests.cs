using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Scheduling.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Xunit;

namespace LabViroMol.Modules.Scheduling.Domain.UnitTests.Schedules;

public class ScheduleTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_ShouldInitializeCorrectly()
        {
            // Arrange
            var scheduler = Fakers.CreateScheduler();
            var scheduling = Fakers.CreateScheduling();
            var equipments = Fakers.CreateScheduleEquipments();

            // Act
            var result = Schedule.Create(
                scheduler,
                scheduling,
                true,
                "Prof. João",
                "Projeto X",
                "Descrição do projeto",
                equipments);

            var schedule = result.Data!;

            // Assert
            Assert.Equal(scheduler, schedule.Scheduler);
            Assert.Equal(scheduling, schedule.Scheduling);
            Assert.True(schedule.AcceptTerm);
            Assert.Equal("Prof. João", schedule.AdvisorProfessor);
            Assert.Equal("Projeto X", schedule.ProjectTitle);
            Assert.Equal("Descrição do projeto", schedule.Description);
            Assert.Equal(ScheduleStatus.PENDING, schedule.Status);

            // 🔥 novo
            Assert.Equal(equipments.Count, schedule.Equipments.Count);
        }
    }

    public class ApproveTests
    {
        [Fact]
        public void Approve_WhenPendingAndFutureDate_ShouldApprove()
        {
            // Arrange
            var schedule = Fakers.CreateSchedule();
            var userId = Fakers.AnyUserId();

            // Act
            schedule.Approve(userId);

            // Assert
            Assert.Equal(ScheduleStatus.SCHEDULED, schedule.Status);
            Assert.Equal(userId, schedule.ApprovedBy);
        }

        [Fact]
        public void Approve_WhenNotPending_ShouldThrow()
        {
            // Arrange
            var schedule = Fakers.CreateSchedule();
            schedule.Approve(Fakers.AnyUserId());

            var result = schedule.Approve(Fakers.AnyUserId());

            // Act & Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Approve_WhenDateIsPast_ShouldThrow()
        {
            // Arrange
            var pastDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-1));

            // Act & Assert
            Assert.Throws<DomainException>(() =>
                Domain.Schedules.Scheduling.Create(
                    pastDate,
                    DateTimeOffset.Now.AddHours(-2),
                    DateTimeOffset.Now.AddHours(-1)
                ));
        }
    }

    public class RefuseTests
    {
        [Fact]
        public void Refuse_WhenPendingAndFutureDate_ShouldRefuse()
        {
            // Arrange
            var schedule = Fakers.CreateSchedule();
            var userId = Fakers.AnyUserId();
            var justification = "test";

            // Act
            schedule.Refuse(userId, justification);

            // Assert
            Assert.Equal(ScheduleStatus.REFUSED, schedule.Status);
            Assert.Equal(userId, schedule.RefusedBy);
        }

        [Fact]
        public void Refuse_WhenNotPending_ShouldThrow()
        {
            // Arrange
            var schedule = Fakers.CreateSchedule();
            var justification = "test";
            schedule.Refuse(Fakers.AnyUserId(), justification);


            // Act & Assert
            var result = schedule.Refuse(Fakers.AnyUserId(), justification);
            Assert.True(result.IsFailure);
        }
    }

    public class SchedulingTests
    {
        [Fact]
        public void CreateScheduling_WithValidData_ShouldSucceed()
        {
            // Arrange
            var date = Fakers.NextWorkday();
            var start = date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)));
            var end = date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)));

            // Act
            var result = Domain.Schedules.Scheduling.Create(date, start, end);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(date, result.Data!.Date);
            Assert.Equal(start, result.Data.StartDateHour);
            Assert.Equal(end, result.Data.EndDateHour);
        }

        [Fact]
        public void CreateScheduling_WithInvalidData_ShouldFail()
        {
            // Arrange (fim antes do início)
            var date = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            var start = DateTimeOffset.Now.AddHours(2);
            var end = DateTimeOffset.Now.AddHours(1);

            // Act
            var result = Domain.Schedules.Scheduling.Create(date, start, end);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Create_WithoutEquipments_ShouldFail()
        {
            // Arrange
            var scheduler = Fakers.CreateScheduler();
            var scheduling = Fakers.CreateScheduling();

            // Act
            var result = Schedule.Create(
                scheduler,
                scheduling,
                true,
                "Prof",
                "Projeto",
                "Desc",
                new List<ScheduleEquipment>());

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Create_WithDuplicatedEquipments_ShouldFail()
        {
            // Arrange
            var scheduler = Fakers.CreateScheduler();
            var scheduling = Fakers.CreateScheduling();

            var equipmentId = Guid.NewGuid();

            var equipments = new List<ScheduleEquipment>
            {
                new ScheduleEquipment(equipmentId, "Microscópio"),
                new ScheduleEquipment(equipmentId, "Microscópio")
            };

            // Act
            var result = Schedule.Create(
                scheduler,
                scheduling,
                true,
                "Prof",
                "Projeto",
                "Desc",
                equipments);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Create_WithEquipments_ShouldPersistCorrectly()
        {
            // Arrange
            var equipments = Fakers.CreateScheduleEquipments(3);

            // Act
            var schedule = Fakers.CreateSchedule(equipments: equipments);

            // Assert
            Assert.Equal(3, schedule.Equipments.Count);
            Assert.All(schedule.Equipments, e =>
                Assert.Contains(e.EquipmentId, equipments.Select(x => x.EquipmentId)));
        }
    }
}