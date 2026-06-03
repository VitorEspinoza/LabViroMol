using System;
using System.Linq;
using LabViroMol.Modules.Inventory.Domain.Orders;
using LabViroMol.Modules.Inventory.Domain.UnitTests.Common;
using Xunit;

namespace LabViroMol.Modules.Inventory.Domain.UnitTests.Orders;

public class OrderTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_ShouldInitializeWithCorrectProperties()
        {
            // Arrange
            var materialId  = Fakers.AnyMaterialId();
            var projectId   = Fakers.AnyProjectId();
            var quantity    = Fakers.QuantityOf(25m);

            // Act
            var order = Fakers.CreateOrder(materialId, projectId, quantity, "Pedido de teste");

            // Assert
            Assert.Equal(OrderStatus.Pending, order.Status);
            Assert.Equal(materialId,          order.MaterialId);
            Assert.Equal(projectId,           order.ProjectId);
            Assert.Equal(quantity,            order.RequestedQuantity);
            Assert.Equal("Pedido de teste",   order.Description);
        }
    }

    public class FixDetailsTests
    {
        [Fact]
        public void FixDetails_WhenPending_ShouldUpdateDetails()
        {
            // Arrange
            var newProject = Fakers.AnyProjectId();
            var newQty     = Fakers.QuantityOf(99m);
            var order      = Fakers.CreateOrder();

            // Act
            var result = order.FixDetails(newProject, newQty, "nova descrição");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(newProject,      order.ProjectId);
            Assert.Equal(newQty,          order.RequestedQuantity);
            Assert.Equal("nova descrição", order.Description);
        }

        [Fact]
        public void FixDetails_WhenProcessing_ShouldReturnFailureWithError()
        {
            var order = Fakers.CreateOrder();
            order.Process(Fakers.AnyUserId(), "Nome", null);

            var result = order.FixDetails(Fakers.AnyProjectId(), Fakers.AnyQuantity(), "desc");

            Assert.True(result.IsFailure);
            Assert.Contains("pendentes", result.Errors[0]);
        }

        [Fact]
        public void FixDetails_WhenCompleted_ShouldReturnFailureWithError()
        {
            var order = Fakers.CreateOrder();
            order.Process(Fakers.AnyUserId(), "Nome", null);
            order.Receive(Fakers.AnyUserId(), "Nome", Fakers.AnyQuantity(), null);

            var result = order.FixDetails(Fakers.AnyProjectId(), Fakers.AnyQuantity(), "desc");

            Assert.True(result.IsFailure);
            Assert.Contains("pendentes", result.Errors[0]);
        }

        [Fact]
        public void FixDetails_WhenCanceled_ShouldReturnFailureWithError()
        {
            var order = Fakers.CreateOrder();
            order.Cancel();

            var result = order.FixDetails(Fakers.AnyProjectId(), Fakers.AnyQuantity(), "desc");

            Assert.True(result.IsFailure);
            Assert.Contains("pendentes", result.Errors[0]);
        }
    }

    public class ProcessTests
    {
        [Fact]
        public void Process_WhenPending_ShouldSetProcessingState()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var before = DateTimeOffset.UtcNow;
            var order  = Fakers.CreateOrder();

            // Act
            var result = order.Process(userId, "Nome Processador", null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(OrderStatus.Processing, order.Status);
            Assert.Equal(userId, order.Processing!.ProcessedBy);
            Assert.True(order.Processing!.ProcessedAt >= before);
        }

        [Fact]
        public void Process_WhenAlreadyProcessing_ShouldReturnFailureWithError()
        {
            var order = Fakers.CreateOrder();
            order.Process(Fakers.AnyUserId(), "Nome", null);

            var result = order.Process(Fakers.AnyUserId(), "Nome", null);

            Assert.True(result.IsFailure);
            Assert.Contains("pendentes", result.Errors[0]);
        }

        [Fact]
        public void Process_WhenCompleted_ShouldReturnFailureWithError()
        {
            var order = Fakers.CreateOrder();
            order.Process(Fakers.AnyUserId(), "Nome", null);
            order.Receive(Fakers.AnyUserId(), "Nome", Fakers.AnyQuantity(), null);

            var result = order.Process(Fakers.AnyUserId(), "Nome", null);

            Assert.True(result.IsFailure);
            Assert.Contains("pendentes", result.Errors[0]);
        }

        [Fact]
        public void Process_WhenCanceled_ShouldReturnFailureWithError()
        {
            var order = Fakers.CreateOrder();
            order.Cancel();

            var result = order.Process(Fakers.AnyUserId(), "Nome", null);

            Assert.True(result.IsFailure);
            Assert.Contains("pendentes", result.Errors[0]);
        }
    }

    public class ReceiveTests
    {
        [Fact]
        public void Receive_WhenProcessing_ShouldCompleteOrderAndRaiseEvent()
        {
            // Arrange
            var userId     = Fakers.AnyUserId();
            var materialId = Fakers.AnyMaterialId();
            var quantity   = Fakers.QuantityOf(50m);
            var before     = DateTimeOffset.UtcNow;
            var order      = Fakers.CreateOrder(materialId: materialId, quantity: quantity);
            order.Process(Fakers.AnyUserId(), "Nome", null);

            // Act
            var result = order.Receive(userId, "Nome Recebedor", quantity, null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(OrderStatus.Completed, order.Status);
            Assert.Equal(userId, order.Receipt!.ReceivedBy);
            Assert.True(order.Receipt!.ReceivedAt >= before);

            var evt = order.Events.OfType<OrderReceivedDomainEvent>().Single();
            Assert.Equal(order.Id,         evt.OrderId);
            Assert.Equal(materialId,       evt.MaterialId);
            Assert.Equal((decimal)quantity, evt.QuantityReceived);
            Assert.Equal(userId,           evt.ReceivedBy);
        }

        [Fact]
        public void Receive_WhenPending_ShouldReturnFailureWithError()
        {
            var order = Fakers.CreateOrder();

            var result = order.Receive(Fakers.AnyUserId(), "Nome", Fakers.AnyQuantity(), null);

            Assert.True(result.IsFailure);
            Assert.Contains("processamento", result.Errors[0]);
        }

        [Fact]
        public void Receive_WhenCompleted_ShouldReturnFailureWithError()
        {
            var order = Fakers.CreateOrder();
            order.Process(Fakers.AnyUserId(), "Nome", null);
            order.Receive(Fakers.AnyUserId(), "Nome", Fakers.AnyQuantity(), null);

            var result = order.Receive(Fakers.AnyUserId(), "Nome", Fakers.AnyQuantity(), null);

            Assert.True(result.IsFailure);
            Assert.Contains("processamento", result.Errors[0]);
        }

        [Fact]
        public void Receive_WhenCanceled_ShouldReturnFailureWithError()
        {
            var order = Fakers.CreateOrder();
            order.Cancel();

            var result = order.Receive(Fakers.AnyUserId(), "Nome", Fakers.AnyQuantity(), null);

            Assert.True(result.IsFailure);
            Assert.Contains("processamento", result.Errors[0]);
        }
    }

    public class CancelTests
    {
        [Fact]
        public void Cancel_WhenPending_ShouldMarkAsCanceled()
        {
            // Arrange
            var order  = Fakers.CreateOrder();

            // Act
            var result = order.Cancel();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(OrderStatus.Canceled, order.Status);
        }

        [Fact]
        public void Cancel_WhenAlreadyCanceled_ShouldBeIdempotentAndReturnFailure()
        {
            // Arrange
            var order = Fakers.CreateOrder();
            order.Cancel();

            // Act
            var result = order.Cancel();

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal(OrderStatus.Canceled, order.Status);
            Assert.Contains("pendentes", result.Errors[0]);
        }

        [Fact]
        public void Cancel_WhenProcessing_ShouldReturnFailureWithError()
        {
            var order = Fakers.CreateOrder();
            order.Process(Fakers.AnyUserId(), "Nome", null);

            var result = order.Cancel();

            Assert.True(result.IsFailure);
            Assert.Contains("pendentes", result.Errors[0]);
        }

        [Fact]
        public void Cancel_WhenCompleted_ShouldReturnFailureWithError()
        {
            var order = Fakers.CreateOrder();
            order.Process(Fakers.AnyUserId(), "Nome", null);
            order.Receive(Fakers.AnyUserId(), "Nome", Fakers.AnyQuantity(), null);

            var result = order.Cancel();

            Assert.True(result.IsFailure);
            Assert.Contains("pendentes", result.Errors[0]);
        }
    }
}
