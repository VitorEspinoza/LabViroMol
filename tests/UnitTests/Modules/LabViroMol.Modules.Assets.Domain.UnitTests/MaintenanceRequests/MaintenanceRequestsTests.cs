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
            var equipmentId = Fakers.AnyEquipmentId();
            var description = "Substituição de peça";
            var problemDesc = "Equipamento apresentando ruído anormal";

            var result = MaintenanceRequest.Create(description, problemDesc, equipmentId);
            var request = result.Data!;

            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.Requested, request.Status);
            Assert.Equal(description, request.Description);
            Assert.Equal(problemDesc, request.ProblemDescription);
            Assert.Equal(equipmentId, request.EquipmentId.Value);
        }
    }

    public class StartTests
    {
        [Fact]
        public void Start_WhenRequested_ShouldSetInProgress()
        {
            var request = Fakers.CreateMaintenanceRequest();

            var result = request.Start();

            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.InProgress, request.Status);
        }

        [Fact]
        public void Start_WhenInProgress_ShouldFail()
        {
            var request = Fakers.CreateInProgressMaintenanceRequest();

            var result = request.Start();

            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.InProgress, request.Status);
        }

        [Fact]
        public void Start_WhenDone_ShouldFail()
        {
            var request = Fakers.CreateDoneMaintenanceRequest();

            var result = request.Start();

            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Done, request.Status);
        }

        [Fact]
        public void Start_WhenCancelled_ShouldFail()
        {
            var request = Fakers.CreateCancelledMaintenanceRequest();

            var result = request.Start();

            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Cancelled, request.Status);
        }
    }

    public class DoneTests
    {
        [Fact]
        public void Done_WhenInProgress_ShouldSetDone()
        {
            var request = Fakers.CreateInProgressMaintenanceRequest();

            var result = request.Done();

            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.Done, request.Status);
        }

        [Fact]
        public void Done_WhenRequested_ShouldFail()
        {
            var request = Fakers.CreateMaintenanceRequest();

            var result = request.Done();

            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Requested, request.Status);
        }

        [Fact]
        public void Done_WhenAlreadyDone_ShouldFail()
        {
            var request = Fakers.CreateDoneMaintenanceRequest();

            var result = request.Done();

            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Done, request.Status);
        }

        [Fact]
        public void Done_WhenCancelled_ShouldFail()
        {
            var request = Fakers.CreateCancelledMaintenanceRequest();

            var result = request.Done();

            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Cancelled, request.Status);
        }
    }

    public class CancelTests
    {
        [Fact]
        public void Cancel_WhenRequested_ShouldSetCancelled()
        {
            var request = Fakers.CreateMaintenanceRequest();

            var result = request.Cancel();

            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.Cancelled, request.Status);
        }

        [Fact]
        public void Cancel_WhenInProgress_ShouldSetCancelled()
        {
            var request = Fakers.CreateInProgressMaintenanceRequest();

            var result = request.Cancel();

            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.Cancelled, request.Status);
        }

        [Fact]
        public void Cancel_WhenDone_ShouldFail()
        {
            var request = Fakers.CreateDoneMaintenanceRequest();

            var result = request.Cancel();

            Assert.True(result.IsFailure);
            Assert.Equal(MaintenanceRequestStatus.Done, request.Status);
        }

        [Fact]
        public void Cancel_WhenAlreadyCancelled_ShouldSetCancelled()
        {
            var request = Fakers.CreateCancelledMaintenanceRequest();

            var result = request.Cancel();

            Assert.True(result.IsSuccess);
            Assert.Equal(MaintenanceRequestStatus.Cancelled, request.Status);
        }
    }
}
