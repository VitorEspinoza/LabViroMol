using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Research.Domain.UnitTests.Positions;

public class PositionTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_WithValidInputs_ShouldReturnSuccessWithCorrectProperties()
        {
            // Arrange
            var userId = Fakers.AnyUserId();
            var name = "Pesquisador Sênior";
            var description = "Cargo de pesquisador com experiência avançada";

            // Act
            var result = Position.Create(userId, name, description);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(name, result.Data!.Name);
            Assert.Equal(description, result.Data!.Description);
            Assert.Equal(userId, result.Data!.CreatedBy);
        }

        [Fact]
        public void Create_WithShortName_ShouldThrow()
        {
            // Arrange
            var userId = Fakers.AnyUserId();

            // Act & Assert
            Assert.Throws<DomainException>(() => Position.Create(userId, "ab", "descricao valida"));
        }
    }
}
