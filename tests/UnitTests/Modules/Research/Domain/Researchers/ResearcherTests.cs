using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Research.Domain.UnitTests.Common;

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
            var createdBy = Fakers.AnyUserId();
            var name = Fakers.AnyResearcherName();
            var background = Fakers.AnyAcademicBackground();
            var positionId = Fakers.AnyPositionId();

            // Act
            var researcher = Researcher.Create(id, createdBy, name, null, background, positionId);

            // Assert
            Assert.Equal(id, researcher.Id);
            Assert.Equal(name.FullName, researcher.Name.FullName);
            Assert.Null(researcher.LattesUrl);
            Assert.Equal(background.DegreeLevel, researcher.AcademicBackground.DegreeLevel);
            Assert.Equal(background.FieldOfStudy, researcher.AcademicBackground.FieldOfStudy);
            Assert.Equal(positionId, researcher.PositionId);
            Assert.Equal(createdBy, researcher.CreatedBy);
            Assert.Null(researcher.UpdatedBy);
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
            researcher.Update(newDegreeLevel, newFieldOfStudy, newPositionId, Fakers.AnyUserId());

            // Assert
            Assert.Equal(newDegreeLevel, researcher.AcademicBackground.DegreeLevel);
            Assert.Equal(newFieldOfStudy, researcher.AcademicBackground.FieldOfStudy);
            Assert.Equal(newPositionId, researcher.PositionId);
        }

        [Fact]
        public void Update_ShouldSetAuditFields()
        {
            // Arrange
            var researcher = Fakers.CreateResearcher();
            var modifiedBy = Fakers.AnyUserId();
            var before = DateTimeOffset.UtcNow;

            // Act
            researcher.Update(DegreeLevel.PostDoctorate, "Epidemiologia", Fakers.AnyPositionId(), modifiedBy);

            // Assert
            Assert.Equal(modifiedBy, researcher.UpdatedBy);
            Assert.True(researcher.UpdatedAt >= before);
        }

        [Fact]
        public void Update_WhenCalledTwice_ShouldReflectLatestValues()
        {
            // Arrange
            var researcher = Fakers.CreateResearcher();
            var firstPositionId = Fakers.AnyPositionId();
            var secondPositionId = Fakers.AnyPositionId();

            // Act
            researcher.Update(DegreeLevel.Masters, "Campo A", firstPositionId, Fakers.AnyUserId());
            researcher.Update(DegreeLevel.Doctorate, "Campo B", secondPositionId, Fakers.AnyUserId());

            // Assert
            Assert.Equal(DegreeLevel.Doctorate, researcher.AcademicBackground.DegreeLevel);
            Assert.Equal("Campo B", researcher.AcademicBackground.FieldOfStudy);
            Assert.Equal(secondPositionId, researcher.PositionId);
        }
    }
}
