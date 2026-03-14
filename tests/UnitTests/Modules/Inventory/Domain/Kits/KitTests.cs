using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.UnitTests.Kits;

public class KitTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_ShouldInitializeCorrectly()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var items = Fakers.AnyKitItems(count: 3);

            // Act
            var kit = Kit.Create(userId, "Kit Alpha", "descrição do kit", items);

            // Assert
            Assert.Equal("Kit Alpha",        kit.Name);
            Assert.Equal("descrição do kit", kit.Description);
            Assert.Equal(userId,             kit.CreatedBy);
            Assert.Equal(3,                  kit.Materials.Count);
            Assert.Equal(userId,             kit.UpdatedBy); 
        }

        [Fact]
        public void Create_WhenItemsIsNull_ShouldThrowArgumentException()
        {
            Assert.Throws<DomainException>(() =>
                Kit.Create(Fakers.AnyUserId(), "Kit Alpha", "descrição", null!));
        }

        [Fact]
        public void Create_WhenItemsIsEmpty_ShouldThrowArgumentException()
        {
            Assert.Throws<DomainException>(() =>
                Kit.Create(Fakers.AnyUserId(), "Kit Alpha", "descrição", new List<KitItem>()));
        }
    }

    public class UpdateMetadataTests
    {
        [Fact]
        public void UpdateMetadata_WhenCalled_ShouldUpdatePropertiesAndAudit()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var kit = Fakers.CreateKit();

            // Act
            kit.UpdateMetadata("Novo Nome", "Nova Descrição", userId);

            // Assert
            Assert.Equal("Novo Nome",      kit.Name);
            Assert.Equal("Nova Descrição", kit.Description);
            Assert.Equal(userId,           kit.UpdatedBy);
            Assert.True(kit.UpdatedAt.HasValue);
        }
    }

    public class DefineMaterialsTests
    {
        [Fact]
        public void DefineMaterials_WhenNewItemAdded_ShouldExpandCollectionAndMarkUpdated()
        {
            // Arrange
            var existingItems = Fakers.AnyKitItems(count: 2);
            var kit = Fakers.CreateKit(items: existingItems);
            var userId = Fakers.AnyUserId();

            // Act
            kit.DefineMaterials([..existingItems, Fakers.AnyKitItem()], userId);

            // Assert
            Assert.Equal(3, kit.Materials.Count);
            Assert.Equal(userId, kit.UpdatedBy);
        }

        [Fact]
        public void DefineMaterials_WhenItemRemoved_ShouldShrinkCollectionAndMarkUpdated()
        {
            // Arrange
            var itemA = Fakers.AnyKitItem();
            var itemB = Fakers.AnyKitItem();
            var kit = Fakers.CreateKit(items: [itemA, itemB]);
            var userId = Fakers.AnyUserId();

            // Act
            kit.DefineMaterials([itemA], userId);

            // Assert
            Assert.Single(kit.Materials);
            Assert.DoesNotContain(kit.Materials, m => m.MaterialId == itemB.MaterialId);
            Assert.Equal(userId, kit.UpdatedBy);
        }

        [Fact]
        public void DefineMaterials_WhenQuantityChanged_ShouldUpdateItemAndMarkUpdated()
        {
            // Arrange
            var materialId = Fakers.AnyMaterialId();
            var kit = Fakers.CreateKit(items: [Fakers.AnyKitItem(materialId: materialId, quantity: Fakers.QuantityOf(5m))]);
            var userId = Fakers.AnyUserId();

            // Act
            kit.DefineMaterials([Fakers.AnyKitItem(materialId: materialId, quantity: Fakers.QuantityOf(10m))], userId);

            // Assert
            var found = kit.Materials.Single(m => m.MaterialId == materialId);
            Assert.Equal(Fakers.QuantityOf(10m), found.Quantity);
            Assert.Equal(userId, kit.UpdatedBy);
        }

        [Fact]
        public void DefineMaterials_WhenNothingChanged_ShouldBeIdempotent()
        {
            // Arrange
            var kit = Fakers.CreateKit();
            var updatedByBefore = kit.UpdatedBy;
            var updatedOnBefore = kit.UpdatedAt;

            // Act
            kit.DefineMaterials(kit.Materials.ToList(), Fakers.AnyUserId());

            // Assert 
            Assert.Equal(updatedByBefore, kit.UpdatedBy);
            Assert.Equal(updatedOnBefore, kit.UpdatedAt);
        }

        [Fact]
        public void DefineMaterials_WhenCompleteReplacement_ShouldContainOnlyNewItems()
        {
            // Arrange
            var itemA = Fakers.AnyKitItem();
            var itemB = Fakers.AnyKitItem();
            var itemC = Fakers.AnyKitItem();
            var itemD = Fakers.AnyKitItem();
            var kit = Fakers.CreateKit(items: [itemA, itemB]);

            // Act
            kit.DefineMaterials([itemC, itemD], Fakers.AnyUserId());

            // Assert
            Assert.DoesNotContain(kit.Materials, m => m.MaterialId == itemA.MaterialId);
            Assert.DoesNotContain(kit.Materials, m => m.MaterialId == itemB.MaterialId);
            Assert.Contains(kit.Materials, m => m.MaterialId == itemC.MaterialId);
            Assert.Contains(kit.Materials, m => m.MaterialId == itemD.MaterialId);
        }
    }
}
