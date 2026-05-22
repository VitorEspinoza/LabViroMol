using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Research.Domain.UnitTests.Partners;

public class PartnerTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_WithValidInputs_ShouldReturnSuccessWithCorrectProperties()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var name = "Instituto de Pesquisa";
            var description = "Parceiro estratégico de pesquisa";

            // Act
            var result = Partner.Create(userId, name, description);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(name, result.Data!.Name);
            Assert.Equal(description, result.Data!.Description);
            Assert.Equal(userId, result.Data!.CreatedBy);
            Assert.Null(result.Data!.UpdatedBy);
        }

        [Fact]
        public void Create_WithShortName_ShouldThrow()
        {
            // Arrange
            var userId = Fakers.AnyUserId();

            // Act & Assert
            Assert.Throws<DomainException>(() => Partner.Create(userId, "ab", null));
        }
    }

    public class UpdateTests
    {
        [Fact]
        public void Update_WithValidInputs_ShouldUpdatePropertiesAndAudit()
        {
            // Arrange
            var modifiedBy = Fakers.AnyUserId();
            var partner = Fakers.CreatePartner();
            var newName = "Novo Nome do Parceiro";
            var newDescription = "Nova descrição";

            // Act
            var result = partner.Update(newName, newDescription, modifiedBy);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(newName, partner.Name);
            Assert.Equal(newDescription, partner.Description);
            Assert.Equal(modifiedBy, partner.UpdatedBy);
        }

        [Fact]
        public void Update_WithShortName_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var partner = Fakers.CreatePartner();

            // Act
            var result = partner.Update("ab", null, Fakers.AnyUserId());

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("3 caracteres", result.Errors[0]);
        }

        [Fact]
        public void Update_WithNullDescription_ShouldClearDescription()
        {
            // Arrange
            var partner = Fakers.CreatePartner();
            var modifiedBy = Fakers.AnyUserId();

            // Act
            var result = partner.Update("Nome Válido do Parceiro", null, modifiedBy);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Null(partner.Description);
        }
    }
}
