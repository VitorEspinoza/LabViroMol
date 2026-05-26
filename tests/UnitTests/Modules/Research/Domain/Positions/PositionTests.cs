using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.UnitTests.Positions;

public class PositionTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_WithValidInputs_ShouldReturnSuccessWithCorrectProperties()
        {
            // Arrange
            var name = "Pesquisador Sênior";
            var description = "Cargo de pesquisador com experiência avançada";

            // Act
            var result = Position.Create(name, description);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(name, result.Data!.Name);
            Assert.Equal(description, result.Data!.Description);
        }

        [Fact]
        public void Create_WithShortName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<DomainException>(() => Position.Create("ab", "descricao valida"));
        }
    }
}
