using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.UnitTests.Common;

namespace LabViroMol.Modules.Inventory.Domain.UnitTests.Materials;

public class MaterialTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_WhenTypeIsActive_ShouldReturnSuccessWithCorrectProperties()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var activeType = Fakers.CreateActiveMaterialType();

            // Act
            var result = Material.Create(userId, "Etanol", "Prateleira A",
                Fakers.QuantityOf(5m), Fakers.QuantityOf(75m), Unit.Milliliter, activeType);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Etanol",        result.Data!.Name);
            Assert.Equal("Prateleira A",  result.Data.Location);
            Assert.Equal(Unit.Milliliter, result.Data.Unit);
            Assert.Equal(activeType.Id,   result.Data.TypeId);
            Assert.Equal(75m, (decimal)result.Data.StockQuantity);
        }

        [Fact]
        public void Create_WhenTypeIsInactive_ShouldReturnFailureWithError()
        {
            // Arrange
            var inactiveType = Fakers.CreateInactiveMaterialType();

            // Act
            var result = Material.Create(Fakers.AnyUserId(), "Etanol", "Prateleira A",
                Fakers.QuantityOf(5m), Fakers.QuantityOf(50m), Unit.Milliliter, inactiveType);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("inativo", result.Errors[0]);
        }

        [Fact]
        public void Create_WithInitialStock_ShouldRegisterExceptionInTransaction()
        {
            // Arrange
            var activeType = Fakers.CreateActiveMaterialType();

            // Act
            var result = Material.Create(Fakers.AnyUserId(), "Etanol", "Prateleira A",
                Fakers.QuantityOf(5m), Fakers.QuantityOf(75m), Unit.Milliliter, activeType);

            // Assert
            var transaction = result.Data!.Transactions.Single();
            Assert.Equal(TransactionType.ExceptionIn, transaction.Type);
            Assert.Equal(75m, (decimal)transaction.Quantity);
        }

        [Fact]
        public void Create_WithZeroInitialStock_ShouldNotRegisterAnyTransaction()
        {
            // Arrange
            var activeType = Fakers.CreateActiveMaterialType();

            // Act
            var result = Material.Create(Fakers.AnyUserId(), "Etanol", "Prateleira A",
                Fakers.QuantityOf(5m), Fakers.QuantityOf(0m), Unit.Milliliter, activeType);

            // Assert
            Assert.Empty(result.Data!.Transactions);
        }
    }

    public class UpdateTests
    {
        [Fact]
        public void Update_WhenCalled_ShouldUpdatePropertiesAndAudit()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var newMin = Fakers.QuantityOf(20m);
            var material = Fakers.CreateMaterial();

            // Act
            material.Update("Novo Nome", newMin, "Nova Localização", userId);

            // Assert
            Assert.Equal("Novo Nome",        material.Name);
            Assert.Equal(newMin,             material.MinStock);
            Assert.Equal("Nova Localização", material.Location);
            Assert.Equal(userId,             material.UpdatedBy);
            Assert.True(material.UpdatedAt.HasValue);
        }
    }

    public class AddStockExceptionTests
    {
        [Fact]
        public void AddStockException_WhenCalled_ShouldIncreaseStockAndAudit()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var material = Fakers.CreateMaterial(stockQuantity: Fakers.QuantityOf(50m));

            // Act
            material.AddStockException(Fakers.QuantityOf(30m), "reposição mensal do estoque", userId);

            // Assert
            Assert.Equal(80m, (decimal)material.StockQuantity);
            Assert.Equal(userId, material.UpdatedBy);
        }

        [Fact]
        public void AddStockException_ShouldAccumulateOnMultipleCalls()
        {
            // Arrange
            var material = Fakers.CreateMaterial(stockQuantity: Fakers.QuantityOf(20m));

            // Act
            material.AddStockException(Fakers.QuantityOf(50m), "reposição mensal do estoque", Fakers.AnyUserId());
            material.AddStockException(Fakers.QuantityOf(30m), "reposição mensal do estoque", Fakers.AnyUserId());

            // Assert — 20 + 50 + 30 = 100
            Assert.Equal(100m, (decimal)material.StockQuantity);
        }

        [Fact]
        public void AddStockException_ShouldRegisterExceptionInTransaction()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var material = Fakers.CreateMaterial(stockQuantity: Fakers.QuantityOf(0m));

            // Act
            material.AddStockException(Fakers.QuantityOf(30m), "reposição mensal do estoque", userId);

            // Assert
            var transaction = material.Transactions.Single();
            Assert.Equal(TransactionType.ExceptionIn, transaction.Type);
            Assert.Equal(30m, (decimal)transaction.Quantity);
            Assert.Equal(material.Id, transaction.MaterialId);
            Assert.Equal(userId, transaction.TransactedByUserId);
        }
    }

    public class RemoveStockExceptionTests
    {
        [Fact]
        public void RemoveStockException_WhenSufficientAndAboveMin_ShouldDecreaseStockReturnSuccessAndAudit()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var material = Fakers.CreateMaterialWithStock(stock: 100, min: 10);

            // Act
            var result = material.RemoveStockException(Fakers.QuantityOf(30m), "baixa de estoque manual", userId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(70m, (decimal)material.StockQuantity);
            Assert.Equal(userId, material.UpdatedBy);
            Assert.DoesNotContain(material.Events, e => e is LowStockDomainEvent);
        }

        [Fact]
        public void RemoveStockException_WhenResultBelowMin_ShouldRaiseLowStockEventWithCorrectData()
        {
            // Arrange
            var material = Fakers.CreateMaterialWithStock(stock: 20, min: 15);

            // Act
            material.RemoveStockException(Fakers.QuantityOf(10m), "baixa de estoque manual", Fakers.AnyUserId());

            // Assert
            var evt = material.Events.OfType<LowStockDomainEvent>().Single();
            Assert.Equal(material.Id, evt.MaterialId);
            Assert.Equal((decimal)material.StockQuantity, evt.CurrentQuantity);
        }

        [Fact]
        public void RemoveStockException_WhenResultEqualsMin_ShouldRaiseLowStockEvent()
        {
            // Arrange
            var material = Fakers.CreateMaterialWithStock(stock: 20, min: 10);

            // Act
            material.RemoveStockException(Fakers.QuantityOf(10m), "baixa de estoque manual", Fakers.AnyUserId());

            // Assert
            Assert.Contains(material.Events, e => e is LowStockDomainEvent);
        }

        [Fact]
        public void RemoveStockException_WhenQuantityExceedsStock_ShouldReturnFailureAndNotChangeStock()
        {
            // Arrange
            var material = Fakers.CreateMaterialWithStock(stock: 50, min: 10);

            // Act
            var result = material.RemoveStockException(Fakers.QuantityOf(60m), "baixa de estoque manual", Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("insuficiente", result.Errors[0]);
            Assert.Equal(50m, (decimal)material.StockQuantity);
        }

        [Fact]
        public void RemoveStockException_WhenRemovingEntireStock_ShouldReturnSuccessAndStockBeZero()
        {
            // Arrange
            var material = Fakers.CreateMaterialWithStock(stock: 50, min: 10);

            // Act
            var result = material.RemoveStockException(Fakers.QuantityOf(50m), "baixa de estoque manual", Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(0m, (decimal)material.StockQuantity);
        }

        [Fact]
        public void RemoveStockException_ShouldRegisterExceptionOutTransaction()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var material = Fakers.CreateMaterial(stockQuantity: Fakers.QuantityOf(50m));
            var initialCount = material.Transactions.Count;

            // Act
            material.RemoveStockException(Fakers.QuantityOf(20m), "baixa de estoque manual", userId);

            // Assert
            Assert.Equal(initialCount + 1, material.Transactions.Count);
            var transaction = material.Transactions.Last();
            Assert.Equal(TransactionType.ExceptionOut, transaction.Type);
            Assert.Equal(20m, (decimal)transaction.Quantity);
            Assert.Equal(material.Id, transaction.MaterialId);
            Assert.Equal(userId, transaction.TransactedByUserId);
        }
    }
}
