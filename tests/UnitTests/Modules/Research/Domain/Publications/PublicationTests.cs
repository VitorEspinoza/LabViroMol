using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Domain.UnitTests.Common;

namespace LabViroMol.Modules.Research.Domain.UnitTests.Publications;

public class PublicationTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_ShouldInitializeWithCorrectProperties()
        {
            // Arrange
            var title = "Analise Virologica em Amostras Clinicas";
            var description = "Descricao detalhada do estudo";
            var doi = "10.1234/test";
            var publicationDate = new DateOnly(2024, 6, 15);
            var publishedOn = "Nature Virology";
            var publishUrl = "https://example.com/pub";

            // Act
            var result = Publication.Create(title, description, doi, publicationDate, publishedOn, publishUrl);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(title, result.Data!.Title);
            Assert.Equal(doi, result.Data!.Doi);
            Assert.Equal(publicationDate, result.Data!.PublicationDate);
            Assert.Equal(publishedOn, result.Data!.PublishedOn);
            Assert.Equal(publishUrl, result.Data!.PublishUrl);
            Assert.Empty(result.Data!.Researchers);
        }
    }

    public class UpdateTests
    {
        [Fact]
        public void Update_WithValidInputs_ShouldUpdateProperties()
        {
            // Arrange
            var publication = Fakers.CreatePublication();
            var newTitle = "Novo Titulo da Publicacao";
            var newPublishedOn = "Science";

            // Act
            var result = publication.Update(newTitle, "nova descricao", newPublishedOn, "https://science.org");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(newTitle, publication.Title);
            Assert.Equal(newPublishedOn, publication.PublishedOn);
        }

        [Fact]
        public void Update_WithShortTitle_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var publication = Fakers.CreatePublication();

            // Act
            var result = publication.Update("ab", "descricao", "Nature", "https://example.com");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("3 caracteres", result.Errors[0]);
        }

        [Fact]
        public void Update_WithEmptyPublishedOn_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var publication = Fakers.CreatePublication();

            // Act
            var result = publication.Update("Titulo Valido da Publicacao", "desc", "", "https://example.com");

            // Assert
            Assert.True(result.IsFailure);
        }
    }

    public class AssignDoiTests
    {
        [Fact]
        public void AssignDoi_WithValidDoi_ShouldSetDoi()
        {
            // Arrange
            var publication = Fakers.CreatePublication();
            var doi = "10.5678/new-doi";

            // Act
            var result = publication.AssignDoi(doi);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(doi, publication.Doi);
        }

        [Fact]
        public void AssignDoi_WithEmptyDoi_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var publication = Fakers.CreatePublication();

            // Act
            var result = publication.AssignDoi("");

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("DOI", result.Errors[0]);
        }

        [Fact]
        public void AssignDoi_WithWhitespaceDoi_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var publication = Fakers.CreatePublication();

            // Act
            var result = publication.AssignDoi("   ");

            // Assert
            Assert.True(result.IsFailure);
        }
    }

    public class AddResearcherTests
    {
        [Fact]
        public void AddResearcher_ShouldAppendWithCorrectOrder()
        {
            // Arrange
            var publication = Fakers.CreatePublication();
            var researcherId = Fakers.AnyResearcherId();

            // Act
            var result = publication.AddResearcher(researcherId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(publication.Researchers);
            Assert.Equal(1, publication.Researchers.First().Order);
            Assert.Equal(researcherId, publication.Researchers.First().ResearcherId);
        }

        [Fact]
        public void AddResearcher_MultipleTimes_ShouldIncrementOrder()
        {
            // Arrange
            var publication = Fakers.CreatePublication();
            var r1 = Fakers.AnyResearcherId();
            var r2 = Fakers.AnyResearcherId();
            var r3 = Fakers.AnyResearcherId();

            // Act
            publication.AddResearcher(r1);
            publication.AddResearcher(r2);
            publication.AddResearcher(r3);

            // Assert
            Assert.Equal(3, publication.Researchers.Count);
            var ordered = publication.Researchers.OrderBy(r => r.Order).ToList();
            Assert.Equal(1, ordered[0].Order);
            Assert.Equal(2, ordered[1].Order);
            Assert.Equal(3, ordered[2].Order);
        }

        [Fact]
        public void AddResearcher_Duplicate_ShouldReturnConflictError()
        {
            // Arrange
            var publication = Fakers.CreatePublication();
            var researcherId = Fakers.AnyResearcherId();
            publication.AddResearcher(researcherId);

            // Act
            var result = publication.AddResearcher(researcherId);

            // Assert
            Assert.True(result.IsFailure);
        }
    }

    public class RemoveResearcherTests
    {
        [Fact]
        public void RemoveResearcher_ShouldRemoveAndRecompactOrder()
        {
            // Arrange
            var publication = Fakers.CreatePublication();
            var r1 = Fakers.AnyResearcherId();
            var r2 = Fakers.AnyResearcherId();
            var r3 = Fakers.AnyResearcherId();
            publication.AddResearcher(r1);
            publication.AddResearcher(r2);
            publication.AddResearcher(r3);

            // Act
            var result = publication.RemoveResearcher(r2);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, publication.Researchers.Count);
            var ordered = publication.Researchers.OrderBy(r => r.Order).ToList();
            Assert.Equal(1, ordered[0].Order);
            Assert.Equal(2, ordered[1].Order);
        }

        [Fact]
        public void RemoveResearcher_NotFound_ShouldReturnNotFoundError()
        {
            // Arrange
            var publication = Fakers.CreatePublication();

            // Act
            var result = publication.RemoveResearcher(Fakers.AnyResearcherId());

            // Assert
            Assert.True(result.IsFailure);
        }
    }

    public class ReorderResearchersTests
    {
        [Fact]
        public void ReorderResearchers_ShouldReassignOrderCorrectly()
        {
            // Arrange
            var publication = Fakers.CreatePublication();
            var r1 = Fakers.AnyResearcherId();
            var r2 = Fakers.AnyResearcherId();
            var r3 = Fakers.AnyResearcherId();
            publication.AddResearcher(r1);
            publication.AddResearcher(r2);
            publication.AddResearcher(r3);

            // Act — reverse order
            var result = publication.ReorderResearchers([r3, r2, r1]);

            // Assert
            Assert.True(result.IsSuccess);
            var ordered = publication.Researchers.OrderBy(r => r.Order).ToList();
            Assert.Equal(r3, ordered[0].ResearcherId);
            Assert.Equal(r2, ordered[1].ResearcherId);
            Assert.Equal(r1, ordered[2].ResearcherId);
        }

        [Fact]
        public void ReorderResearchers_WithDuplicateIds_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var publication = Fakers.CreatePublication();
            var r1 = Fakers.AnyResearcherId();
            publication.AddResearcher(r1);

            // Act
            var result = publication.ReorderResearchers([r1, r1]);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void ReorderResearchers_WithMissingId_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var publication = Fakers.CreatePublication();
            var r1 = Fakers.AnyResearcherId();
            publication.AddResearcher(r1);

            // Act
            var result = publication.ReorderResearchers([Fakers.AnyResearcherId()]);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void ReorderResearchers_WithIncompleteList_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var publication = Fakers.CreatePublication();
            var r1 = Fakers.AnyResearcherId();
            var r2 = Fakers.AnyResearcherId();
            publication.AddResearcher(r1);
            publication.AddResearcher(r2);

            // Act — only include one of two
            var result = publication.ReorderResearchers([r1]);

            // Assert
            Assert.True(result.IsFailure);
        }
    }
}
