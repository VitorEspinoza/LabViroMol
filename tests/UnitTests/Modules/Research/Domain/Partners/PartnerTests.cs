using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.UnitTests.Partners;

public class PartnerTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_WithValidInputs_ShouldReturnSuccessWithCorrectProperties()
        {
            // Arrange
            var name = "Instituto de Pesquisa";
            var description = "Parceiro estratégico de pesquisa";

            // Act
            var result = Partner.Create(name, description);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(name, result.Data!.Name);
            Assert.Equal(description, result.Data!.Description);
        }

        [Fact]
        public void Create_WithShortName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<DomainException>(() => Partner.Create("ab", null));
        }
    }

    public class UpdateTests
    {
        [Fact]
        public void Update_WithValidInputs_ShouldUpdateProperties()
        {
            // Arrange
            var partner = Fakers.CreatePartner();
            var newName = "Novo Nome do Parceiro";
            var newDescription = "Nova descrição";

            // Act
            var result = partner.Update(newName, newDescription);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(newName, partner.Name);
            Assert.Equal(newDescription, partner.Description);
        }

        [Fact]
        public void Update_WithShortName_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var partner = Fakers.CreatePartner();

            // Act
            var result = partner.Update("ab", null);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("3 caracteres", result.Errors[0]);
        }

        [Fact]
        public void Update_WithNullDescription_ShouldClearDescription()
        {
            // Arrange
            var partner = Fakers.CreatePartner();

            // Act
            var result = partner.Update("Nome Válido do Parceiro", null);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(partner.Description);
        }
    }
}
