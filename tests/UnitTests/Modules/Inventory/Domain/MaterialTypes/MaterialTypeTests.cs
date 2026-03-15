using LabViroMol.Modules.Inventory.Domain.MaterialTypes;
using LabViroMol.Modules.Inventory.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.UnitTests.MaterialTypes;

public class MaterialTypeTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_ShouldInitializeCorrectly()
        {
            // Arrange
            var userId = Fakers.AnyUserId();

            // Act
            var type = MaterialType.Create(userId, "Reagente");

            // Assert
            Assert.Equal("Reagente", type.Name);
            Assert.Equal(userId,     type.CreatedBy);
            Assert.NotEqual(default, type.Id);
            Assert.True(type.Active);
        }
    }

    public class DeactivateTests
    {
        [Fact]
        public void Deactivate_WhenCalled_ShouldMarkAsInactive()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var type = Fakers.CreateMaterialType();

            // Act
            type.Deactivate(userId);

            // Assert
            Assert.False(type.Active);
            Assert.Equal(userId, type.DeactivatedBy);
            Assert.True(type.DeactivatedAt.HasValue);
            Assert.Equal(userId, type.UpdatedBy);
        }

        [Fact]
        public void Deactivate_CalledTwice_ShouldBeIdempotent()
        {
            // Arrange
            var firstUser  = Fakers.AnyUserId();
            var secondUser = Fakers.AnyUserId();
            var type = Fakers.CreateMaterialType();

            // Act
            type.Deactivate(firstUser);
            type.Deactivate(secondUser); 

            // Assert
            Assert.Equal<UserId?>(firstUser, type.DeactivatedBy);
        }
    }

    public class ActivateTests
    {
        [Fact]
        public void Activate_WhenCalled_ShouldRestoreActiveState()
        {
            // Arrange
            var activatedBy = Fakers.AnyUserId();
            var type = Fakers.CreateInactiveMaterialType();

            // Act
            type.Activate(activatedBy);

            // Assert
            Assert.True(type.Active);
            Assert.Null(type.DeactivatedBy);
            Assert.Null(type.DeactivatedAt);
            Assert.Equal(activatedBy, type.UpdatedBy);
            Assert.True(type.UpdatedAt.HasValue);
        }

        [Fact]
        public void Activate_CalledTwice_ShouldBeIdempotent()
        {
            // Arrange 
            var type = Fakers.CreateMaterialType();

            // Act 
            type.Activate(Fakers.AnyUserId());

            // Assert 
            Assert.True(type.Active);
            Assert.Null(type.DeactivatedBy);
            Assert.Null(type.UpdatedBy); 
        }
    }
}
