using Mediator;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LabViroMol.ArchitectureTests;

public sealed class CqrsConventionTests
{
    [Fact]
    public void Commands_should_be_named_with_command_suffix()
    {
        Classes().That().ImplementInterface(typeof(ICommand<>))
            .Should().HaveNameEndingWith("Command")
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Queries_should_be_named_with_query_suffix()
    {
        var queryTypes = TestTypeCatalog.AllTypes()
            .Where(type => type.IsClass && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>)))
            .ToArray();

        if (queryTypes.Length == 0)
        {
            return;
        }

        Classes().That().ImplementInterface(typeof(IQuery<>))
            .Should().HaveNameEndingWith("Query")
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Command_and_query_handlers_should_be_named_handler_and_live_in_application()
    {
        Classes().That().ImplementInterface(typeof(ICommandHandler<,>))
            .Should().HaveNameEndingWith("Handler")
            .AndShould().HaveFullNameContaining(".Application.")
            .Check(ArchitectureModel.Architecture);

        var queryHandlers = TestTypeCatalog.AllTypes()
            .Where(type => type.IsClass && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
            .ToArray();

        if (queryHandlers.Length > 0)
        {
            Classes().That().ImplementInterface(typeof(IQueryHandler<,>))
                .Should().HaveNameEndingWith("Handler")
                .AndShould().HaveFullNameContaining(".Application.")
                .Check(ArchitectureModel.Architecture);
        }
    }

    [Fact]
    public void Commands_and_queries_should_be_declared_as_records()
    {
        var messages = TestTypeCatalog.AllTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces().Any(i =>
                i.IsGenericType &&
                (i.GetGenericTypeDefinition() == typeof(ICommand<>) || i.GetGenericTypeDefinition() == typeof(IQuery<>))))
            .ToArray();

        Assert.All(messages, type => Assert.True(SourceCodeInspector.IsDeclaredAsRecord(type), $"{type.FullName} should be declared as record."));
    }

    [Fact]
    public void Presentation_and_application_should_not_depend_directly_on_handlers()
    {
        var handlers = Classes().That().HaveNameEndingWith("Handler");

        Types().That().Are(ArchitectureModel.Presentation)
            .Should().NotDependOnAny(handlers)
            .Check(ArchitectureModel.Architecture);

        Classes().That().Are(ArchitectureModel.Application)
            .And().DoNotHaveNameEndingWith("Handler")
            .Should().NotDependOnAny(handlers)
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Handlers_should_be_sealed()
    {
        var handlers = TestTypeCatalog.AllTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type =>
                type.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))))
            .ToArray();

        Assert.All(handlers, type => Assert.True(type.IsSealed, $"{type.FullName} should be sealed."));
    }
}
