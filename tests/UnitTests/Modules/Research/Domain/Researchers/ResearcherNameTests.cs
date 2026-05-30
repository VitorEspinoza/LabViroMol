using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.UnitTests.Researchers;

public class ResearcherNameTests
{
    public class FullNameTests
    {
        [Fact]
        public void FullName_ShouldConcatenateFirstAndLastName()
        {
            // Arrange
            var name = new ResearcherName("Ana", "Lima", null, null);

            // Act & Assert
            Assert.Equal("Ana Lima", name.FullName);
        }
    }

    public class PublicDisplayNameTests
    {
        [Fact]
        public void PublicDisplayName_WhenDisplayNameIsNull_ShouldReturnFirstNameAndLastWordOfLastName()
        {
            // Arrange
            var name = new ResearcherName("Ana", "Lima Santos", null, null);

            // Act & Assert
            Assert.Equal("Ana Santos", name.PublicDisplayName);
        }
    }

    public class PublicCitationNameTests
    {
        [Fact]
        public void PublicCitationName_WhenCitationNameIsNull_ShouldFormatAsLastWordUpperCommaInitial()
        {
            // Arrange
            var name = new ResearcherName("Ana", "Lima Santos", null, null);

            // Act & Assert
            Assert.Equal("SANTOS, A.", name.PublicCitationName);
        }

        [Fact]
        public void PublicCitationName_WhenCitationNameIsSet_ShouldReturnItDirectly()
        {
            // Arrange
            var citationName = "Santos, A. L.";
            var name = new ResearcherName("Ana", "Lima Santos", citationName, null);

            // Act & Assert
            Assert.Equal(citationName, name.PublicCitationName);
        }
    }

    public class ConstructorTests
    {
        [Fact]
        public void Constructor_WithEmptyFirstName_ShouldThrow()
        {
            // Act & Assert
            Assert.Throws<DomainException>(() => new ResearcherName("", "Lima", null, null));
        }
    }
}
