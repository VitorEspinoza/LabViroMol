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

        [Fact]
        public void AddTranslation_WithBlankLanguage_ShouldIgnoreTranslation()
        {
            var position = Position.Create("Pesquisador", "Descricao").Data!;

            position.AddTranslation("   ", "Researcher", "Description");

            Assert.Empty(position.Translations);
        }

        [Fact]
        public void AddTranslation_WithEnglishLanguage_ShouldStoreLowerCaseKey()
        {
            var position = Position.Create("Pesquisador", "Descricao").Data!;

            position.AddTranslation("EN", "Researcher", "Description");

            Assert.True(position.Translations.ContainsKey("en"));
        }

        [Fact]
        public void GetName_WhenEnglishTranslationExists_ReturnsTranslatedName()
        {
            var position = Position.Create("Pesquisador", "Descricao").Data!;
            position.AddTranslation("en", "Researcher", "Description");

            var result = position.GetName("en");

            Assert.Equal("Researcher", result);
        }

        [Fact]
        public void GetDescription_WhenEnglishTranslationIsMissing_FallsBackToDefaultDescription()
        {
            var position = Position.Create("Pesquisador", "Descricao original").Data!;
            position.AddTranslation("en", "Researcher", "");

            var result = position.GetDescription("en");

            Assert.Equal("Descricao original", result);
        }
    }
}
