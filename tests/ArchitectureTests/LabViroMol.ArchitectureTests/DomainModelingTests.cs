using System.Reflection;
using LabViroMol.Modules.Shared.Kernel.Messaging;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.ArchitectureTests;

public sealed class DomainModelingTests
{
    [Fact]
    public void Aggregate_roots_should_live_in_domain_and_not_be_records()
    {
        var aggregateRoots = TestTypeCatalog.AllTypes()
            .Where(type => type.BaseType is not null)
            .Where(type => type.BaseType!.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            .ToArray();

        Assert.All(aggregateRoots, type =>
        {
            Assert.Contains(".Domain.", type.FullName);
            Assert.False(SourceCodeInspector.IsDeclaredAsRecord(type), $"{type.FullName} should be a class, not a record.");
        });
    }

    [Fact]
    public void Strong_ids_should_be_structs_named_id_and_live_in_domain()
    {
        var strongIds = TestTypeCatalog.AllTypes()
            .Where(type => type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IStrongId<>)))
            .ToArray();

        Assert.All(strongIds, type =>
        {
            Assert.True(type.IsValueType, $"{type.FullName} should be a struct.");
            Assert.EndsWith("Id", type.Name, StringComparison.Ordinal);
            Assert.True(
                type.FullName!.Contains(".Domain.", StringComparison.Ordinal) ||
                type.FullName.Contains("LabViroMol.Modules.Shared.Kernel.Identity.", StringComparison.Ordinal),
                $"{type.FullName} should live in a domain layer or shared kernel identity namespace.");
        });
    }

    [Fact]
    public void Repository_interfaces_should_live_in_domain_and_implementations_in_infrastructure()
    {
        var repositoryInterfaces = TestTypeCatalog.AllTypes()
            .Where(type => type.IsInterface && type.Name.StartsWith("I", StringComparison.Ordinal) && type.Name.EndsWith("Repository", StringComparison.Ordinal))
            .ToArray();

        Assert.All(repositoryInterfaces, type => Assert.Contains(".Domain.", type.FullName));

        var repositoryImplementations = TestTypeCatalog.AllTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.Name.EndsWith("Repository", StringComparison.Ordinal))
            .Where(type => type.GetInterfaces().Any(i => i.Name.EndsWith("Repository", StringComparison.Ordinal)))
            .ToArray();

        Assert.All(repositoryImplementations, type => Assert.Contains(".Infrastructure.", type.FullName));
    }

    [Fact]
    public void Query_interfaces_should_live_in_application_and_implementations_in_infrastructure()
    {
        var queryInterfaces = TestTypeCatalog.AllTypes()
            .Where(type => type.IsInterface && type.Name.StartsWith("I", StringComparison.Ordinal) && type.Name.EndsWith("Queries", StringComparison.Ordinal))
            .ToArray();

        Assert.All(queryInterfaces, type => Assert.Contains(".Application.", type.FullName));

        var queryImplementations = TestTypeCatalog.AllTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces().Any(i => i.Name.EndsWith("Queries", StringComparison.Ordinal)))
            .ToArray();

        Assert.All(queryImplementations, type => Assert.Contains(".Infrastructure.", type.FullName));
    }

    [Fact]
    public void View_models_should_live_in_application()
    {
        var viewModels = TestTypeCatalog.AllTypes()
            .Where(type => type.Name.EndsWith("ViewModel", StringComparison.Ordinal))
            .ToArray();

        Assert.All(viewModels, type => Assert.Contains(".Application.", type.FullName));
    }

    [Fact]
    public void Domain_events_should_live_in_domain_and_be_records()
    {
        var events = TestTypeCatalog.AllTypes()
            .Where(type => !type.IsInterface && typeof(IDomainEvent).IsAssignableFrom(type))
            .ToArray();

        Assert.All(events, type =>
        {
            Assert.Contains(".Domain.", type.FullName);
            Assert.True(
                type.Name.EndsWith("DomainEvent", StringComparison.Ordinal) ||
                type.Name.EndsWith("Event", StringComparison.Ordinal),
                $"{type.FullName} should end with DomainEvent or Event.");
            Assert.True(SourceCodeInspector.IsDeclaredAsRecord(type), $"{type.FullName} should be declared as record.");
        });
    }

    [Fact]
    public void Aggregates_should_not_expose_public_setters()
    {
        var aggregateRoots = TestTypeCatalog.AllTypes()
            .Where(type => type.BaseType is not null)
            .Where(type => type.BaseType!.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            .ToArray();

        var offenders = aggregateRoots
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(property => property.SetMethod?.IsPublic == true)
                .Select(property => $"{type.FullName}.{property.Name}"))
            .ToArray();

        Assert.Empty(offenders);
    }
}
