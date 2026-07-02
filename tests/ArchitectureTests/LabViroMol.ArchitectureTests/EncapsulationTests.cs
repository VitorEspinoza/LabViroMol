namespace LabViroMol.ArchitectureTests;

public sealed class EncapsulationTests
{
    [Fact]
    public void Interfaces_should_start_with_i()
    {
        var interfaces = TestTypeCatalog.AllTypes().Where(type => type.IsInterface).ToArray();
        Assert.All(interfaces, type => Assert.StartsWith("I", type.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void Types_should_live_under_their_assembly_root_namespace()
    {
        var offenders = TestTypeCatalog.AllTypes()
            .Where(type => type.Assembly.GetName().Name is not null)
            .Where(type => type.Namespace is not null)
            .Where(type => !type.Namespace!.StartsWith("Coverlet.", StringComparison.Ordinal))
            .Where(type => !type.Namespace!.StartsWith(type.Assembly.GetName().Name!, StringComparison.Ordinal))
            .Select(type => type.FullName!)
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Custom_exceptions_should_end_with_exception()
    {
        Assert.All(TestTypeCatalog.CustomExceptions(), type => Assert.EndsWith("Exception", type.Name, StringComparison.Ordinal));
    }

    [Fact]
    public void Repository_and_queries_implementations_should_be_internal()
    {
        var repositoryImplementations = TestTypeCatalog.AllTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Name.EndsWith("Repository", StringComparison.Ordinal))
            .Where(type => type.GetInterfaces().Any(i => i.Name.EndsWith("Repository", StringComparison.Ordinal)))
            .ToArray();

        var queryImplementations = TestTypeCatalog.AllTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces().Any(i => i.Name.EndsWith("Queries", StringComparison.Ordinal)))
            .ToArray();

        var concreteTypes = repositoryImplementations
            .Concat(queryImplementations)
            .Distinct()
            .ToArray();

        Assert.All(
            concreteTypes,
            type => Assert.True(SourceCodeInspector.IsDeclaredAsInternal(type), $"{type.FullName} should be internal."));
    }
}
