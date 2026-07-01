using LabViroMol.Modules.Inventory.Domain.Kits;
using LabViroMol.Modules.Inventory.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Inventory.Domain.UnitTests.Kits;

public class KitTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_ShouldInitializeCorrectly()
        {
            // Arrange
            var items = Fakers.AnyKitItems(count: 3);

            // Act
            var kit = Kit.Create("Kit Alpha", "descrição do kit", items);

            // Assert
            Assert.Equal("Kit Alpha", kit.Name);
            Assert.Equal("descrição do kit", kit.Description);
            Assert.Equal(3, kit.Materials.Count);
        }

        [Fact]
        public void Create_WhenItemsIsNull_ShouldThrowArgumentException()
        {
            Assert.Throws<DomainException>(() =>
                Kit.Create("Kit Alpha", "descrição", null!));
        }

        [Fact]
        public void Create_WhenItemsIsEmpty_ShouldThrowArgumentException()
        {
            Assert.Throws<DomainException>(() =>
                Kit.Create("Kit Alpha", "descrição", new List<KitItem>()));
        }
    }

    public class UpdateMetadataTests
    {
        [Fact]
        public void UpdateMetadata_WhenCalled_ShouldUpdateProperties()
        {
            // Arrange
            var kit = Fakers.CreateKit();

            // Act
            kit.UpdateMetadata("Novo Nome", "Nova Descrição");

            // Assert
            Assert.Equal("Novo Nome", kit.Name);
            Assert.Equal("Nova Descrição", kit.Description);
        }
    }

    public class DefineMaterialsTests
    {
        [Fact]
        public void DefineMaterials_WhenNewItemAdded_ShouldExpandCollection()
        {
            // Arrange
            var existingItems = Fakers.AnyKitItems(count: 2);
            var kit = Fakers.CreateKit(items: existingItems);

            // Act
            kit.DefineMaterials([.. existingItems, Fakers.AnyKitItem()]);

            // Assert
            Assert.Equal(3, kit.Materials.Count);
        }

        [Fact]
        public void DefineMaterials_WhenItemRemoved_ShouldShrinkCollection()
        {
            // Arrange
            var itemA = Fakers.AnyKitItem();
            var itemB = Fakers.AnyKitItem();
            var kit = Fakers.CreateKit(items: [itemA, itemB]);

            // Act
            kit.DefineMaterials([itemA]);

            // Assert
            Assert.Single(kit.Materials);
            Assert.DoesNotContain(kit.Materials, m => m.MaterialId == itemB.MaterialId);
        }

        [Fact]
        public void DefineMaterials_WhenQuantityChanged_ShouldUpdateItem()
        {
            // Arrange
            var materialId = Fakers.AnyMaterialId();
            var kit = Fakers.CreateKit(items: [Fakers.AnyKitItem(materialId: materialId, quantity: Fakers.QuantityOf(5m))]);

            // Act
            kit.DefineMaterials([Fakers.AnyKitItem(materialId: materialId, quantity: Fakers.QuantityOf(10m))]);

            // Assert
            var found = kit.Materials.Single(m => m.MaterialId == materialId);
            Assert.Equal(Fakers.QuantityOf(10m), found.Quantity);
        }

        [Fact]
        public void DefineMaterials_WhenNothingChanged_ShouldBeIdempotent()
        {
            // Arrange
            var kit = Fakers.CreateKit();
            var materialsBefore = kit.Materials.ToList();

            // Act
            kit.DefineMaterials(kit.Materials.ToList());

            // Assert
            Assert.Equal(materialsBefore.Count, kit.Materials.Count);
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
            kit.DefineMaterials([itemC, itemD]);

            // Assert
            Assert.DoesNotContain(kit.Materials, m => m.MaterialId == itemA.MaterialId);
            Assert.DoesNotContain(kit.Materials, m => m.MaterialId == itemB.MaterialId);
            Assert.Contains(kit.Materials, m => m.MaterialId == itemC.MaterialId);
            Assert.Contains(kit.Materials, m => m.MaterialId == itemD.MaterialId);
        }
    }
}
