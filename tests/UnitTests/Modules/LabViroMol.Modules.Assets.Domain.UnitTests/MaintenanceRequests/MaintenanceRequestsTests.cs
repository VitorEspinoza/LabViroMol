using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Assets.Domain.UnitTests.Common;
using Xunit;

namespace LabViroMol.Modules.Assets.Domain.UnitTests.MaintenanceRequests;

public class MaintenanceRequestTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_ShouldInitializeCorrectly()
        {
            // Arrange
            var createdBy     = Fakers.AnyUserId();
            var equipmentId   = Fakers.AnyEquipmentId();
            var description   = "Substituição de peça";
            var problemDesc   = "Equipamento apresentando ruído anormal";

            // Act
            var result  = MaintenanceRequest.Create(createdBy, description, problemDesc, equipmentId);
            var request = result.Data!;

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.Requested, request.Status);
            Assert.Equal(description,  request.Description);
            Assert.Equal(problemDesc,  request.ProblemDescription);
            Assert.Equal(equipmentId,  request.EquipmentId.Value);
        }
    }

    public class StartTests
    {
        [Fact]
        public void Start_WhenRequested_ShouldSetInProgress()
        {
            // Arrange
            var request = Fakers.CreateMaintenanceRequest();
            var userId  = Fakers.AnyUserId();

            // Act
            var result = request.Start(userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.InProgress, request.Status);
        }

        [Fact]
        public void Start_WhenInProgress_ShouldFail()
        {
            // Arrange
            var request = Fakers.CreateInProgressMaintenanceRequest();

            // Act
            var result = request.Start(Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.InProgress, request.Status);
        }

        [Fact]
        public void Start_WhenDone_ShouldFail()
        {
            // Arrange
            var request = Fakers.CreateDoneMaintenanceRequest();

            // Act
            var result = request.Start(Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Done, request.Status);
        }

        [Fact]
        public void Start_WhenCancelled_ShouldFail()
        {
            // Arrange
            var request = Fakers.CreateCancelledMaintenanceRequest();

            // Act
            var result = request.Start(Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Cancelled, request.Status);
        }
    }

    public class DoneTests
    {
        [Fact]
        public void Done_WhenInProgress_ShouldSetDone()
        {
            // Arrange
            var request = Fakers.CreateInProgressMaintenanceRequest();
            var userId  = Fakers.AnyUserId();

            // Act
            var result = request.Done(userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.Done, request.Status);
        }

        [Fact]
        public void Done_WhenRequested_ShouldFail()
        {
            // Arrange
            var request = Fakers.CreateMaintenanceRequest();

            // Act
            var result = request.Done(Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Requested, request.Status);
        }

        [Fact]
        public void Done_WhenAlreadyDone_ShouldFail()
        {
            // Arrange
            var request = Fakers.CreateDoneMaintenanceRequest();

            // Act
            var result = request.Done(Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Done, request.Status);
        }

        [Fact]
        public void Done_WhenCancelled_ShouldFail()
        {
            // Arrange
            var request = Fakers.CreateCancelledMaintenanceRequest();

            // Act
            var result = request.Done(Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Cancelled, request.Status);
        }
    }

    public class CancelTests
    {
        [Fact]
        public void Cancel_WhenRequested_ShouldSetCancelled()
        {
            // Arrange
            var request = Fakers.CreateMaintenanceRequest();
            var userId  = Fakers.AnyUserId();

            // Act
            var result = request.Cancel(userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.Cancelled, request.Status);
        }

        [Fact]
        public void Cancel_WhenInProgress_ShouldSetCancelled()
        {
            // Arrange
            var request = Fakers.CreateInProgressMaintenanceRequest();

            // Act
            var result = request.Cancel(Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.Cancelled, request.Status);
        }

        [Fact]
        public void Cancel_WhenDone_ShouldFail()
        {
            // Arrange
            var request = Fakers.CreateDoneMaintenanceRequest();

            // Act
            var result = request.Cancel(Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Done, request.Status);
        }

        [Fact]
        public void Cancel_WhenAlreadyCancelled_ShouldSetCancelled()
        {
            // Arrange
            var request = Fakers.CreateCancelledMaintenanceRequest();

            // Act
            var result = request.Cancel(Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.Cancelled, request.Status);
        }
    }
}