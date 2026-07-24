using LabViroMol.Modules.Shared.Kernel.Pagination;

namespace LabViroMol.Modules.Shared.Infrastructure.UnitTests.Pagination;

public class QueryableSearchExtensionsTests
{
    [Fact]
    public void WhereSearch_WhenSearchIsNull_ReturnsOriginalSource()
    {
        var source = CreateItems().AsQueryable();

        var result = source.WhereSearch(null, x => x.Name).ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void WhereSearch_WhenNoFieldsAreProvided_ReturnsOriginalSource()
    {
        var source = CreateItems().AsQueryable();

        var result = source.WhereSearch("virus").ToList();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void WhereSearch_FiltersByAnyConfiguredField_AndIgnoresNullValues()
    {
        var source = CreateItems().AsQueryable();

        var result = source
            .WhereSearch("Virus", x => x.Name, x => x.Description)
            .Select(x => x.Name)
            .ToList();

        Assert.Equal(["Virus A", "Culture B"], result);
    }

    [Fact]
    public void WhereSearch_WhenNoFieldMatches_ReturnsEmptySequence()
    {
        var source = CreateItems().AsQueryable();

        var result = source.WhereSearch("missing", x => x.Name, x => x.Description).ToList();

        Assert.Empty(result);
    }

    private static List<SearchItem> CreateItems() =>
    [
        new("Virus A", "Alpha sample"),
        new("Culture B", "Virus repository"),
        new(null, "Control material"),
    ];

    private sealed record SearchItem(string? Name, string? Description);
}
