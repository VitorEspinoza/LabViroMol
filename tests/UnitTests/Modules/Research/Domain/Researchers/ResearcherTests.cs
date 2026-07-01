using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.UnitTests.Researchers;

public class ResearcherTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_ShouldInitializeWithCorrectProperties()
        {
            // Arrange
            var id = Fakers.AnyResearcherId();
            var name = Fakers.AnyResearcherName();
            var background = Fakers.AnyAcademicBackground();
            var positionId = Fakers.AnyPositionId();

            // Act
            var researcher = Researcher.Create(id, name, null, background, positionId);

            // Assert
            Assert.Equal(id, researcher.Id);
            Assert.Equal(name.FullName, researcher.Name.FullName);
            Assert.Null(researcher.LattesUrl);
            Assert.Equal(background.DegreeLevel, researcher.AcademicBackground.DegreeLevel);
            Assert.Equal(background.FieldOfStudy, researcher.AcademicBackground.FieldOfStudy);
            Assert.Equal(positionId, researcher.PositionId);
        }
    }

    public class UpdateTests
    {
        [Fact]
        public void Update_ShouldChangeAcademicBackgroundAndPosition()
        {
            // Arrange
            var researcher = Fakers.CreateResearcher();
            var newDegreeLevel = DegreeLevel.Doctorate;
            var newFieldOfStudy = "Virologia Molecular";
            var newPositionId = Fakers.AnyPositionId();

            // Act
            researcher.Update(newDegreeLevel, newFieldOfStudy, newPositionId);

            // Assert
            Assert.Equal(newDegreeLevel, researcher.AcademicBackground.DegreeLevel);
            Assert.Equal(newFieldOfStudy, researcher.AcademicBackground.FieldOfStudy);
            Assert.Equal(newPositionId, researcher.PositionId);
        }

        [Fact]
        public void Update_WhenCalledTwice_ShouldReflectLatestValues()
        {
            // Arrange
            var researcher = Fakers.CreateResearcher();
            var firstPositionId = Fakers.AnyPositionId();
            var secondPositionId = Fakers.AnyPositionId();

            // Act
            researcher.Update(DegreeLevel.Masters, "Campo A", firstPositionId);
            researcher.Update(DegreeLevel.Doctorate, "Campo B", secondPositionId);

            // Assert
            Assert.Equal(DegreeLevel.Doctorate, researcher.AcademicBackground.DegreeLevel);
            Assert.Equal("Campo B", researcher.AcademicBackground.FieldOfStudy);
            Assert.Equal(secondPositionId, researcher.PositionId);
        }
    }

    public class ActivationTests
    {
        [Fact]
        public void Deactivate_WhenResearcherIsActive_ShouldMarkItInactive()
        {
            var researcher = Fakers.CreateResearcher();

            researcher.Deactivate();

            Assert.False(researcher.IsActive);
            Assert.NotNull(researcher.DeactivatedAt);
        }

        [Fact]
        public void Deactivate_WhenResearcherIsAlreadyInactive_ShouldThrow()
        {
            var researcher = Fakers.CreateResearcher();
            researcher.Deactivate();

            Assert.Throws<DomainException>(() => researcher.Deactivate());
        }

        [Fact]
        public void Reactivate_WhenResearcherIsInactive_ShouldClearDeactivatedAt()
        {
            var researcher = Fakers.CreateResearcher();
            researcher.Deactivate();

            researcher.Reactivate();

            Assert.True(researcher.IsActive);
            Assert.Null(researcher.DeactivatedAt);
        }

        [Fact]
        public void Reactivate_WhenResearcherIsAlreadyActive_ShouldThrow()
        {
            var researcher = Fakers.CreateResearcher();

            Assert.Throws<DomainException>(() => researcher.Reactivate());
        }
    }
}
