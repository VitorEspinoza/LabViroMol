using FluentValidation;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LabViroMol.ArchitectureTests;

public sealed class ValidationConventionTests
{
    [Fact]
    public void Validators_should_be_named_validator_and_live_in_application()
    {
        Classes().That().AreAssignableTo(typeof(AbstractValidator<>))
            .Should().HaveNameEndingWith("Validator")
            .AndShould().HaveFullNameContaining(".Application.")
            .Check(ArchitectureModel.Architecture);
    }

    [Fact]
    public void Validators_should_be_public()
    {
        var validators = TestTypeCatalog.AllTypes()
            .Where(type => !type.IsAbstract)
            .Where(type => type.BaseType?.IsGenericType == true && type.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>))
            .ToArray();

        Assert.All(validators, type => Assert.True(SourceCodeInspector.IsDeclaredAsPublic(type), $"{type.FullName} should be public."));
    }
}
