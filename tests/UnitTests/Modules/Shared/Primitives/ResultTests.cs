using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Shared.Infrastructure.UnitTests.Primitives;

public class ResultTests
{
    [Fact]
    public void Constructor_WhenSuccessContainsErrors_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _ = new TestableResult(true, null, ["boom"]));
    }

    [Fact]
    public void Constructor_WhenFailureContainsNoErrors_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _ = new TestableResult(false, ResultErrorType.Validation, []));
    }

    [Theory]
    [MemberData(nameof(FromErrorCases))]
    public void FromError_MapsErrorTypeAndErrors(Result source, ResultErrorType expectedType, string[] expectedErrors)
    {
        var result = Result<string>.FromError(source);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedType, result.ErrorType);
        Assert.Equal(expectedErrors, result.Errors);
        Assert.Null(result.Data);
    }

    [Fact]
    public void ImplicitConversion_TransformsValueIntoSuccessResult()
    {
        Result<string> result = "payload";

        Assert.True(result.IsSuccess);
        Assert.Equal("payload", result.Data);
        Assert.Empty(result.Errors);
    }

    public static IEnumerable<object[]> FromErrorCases()
    {
        yield return [Result.NotFound("missing"), ResultErrorType.NotFound, new[] { "missing" }];
        yield return [Result.Conflict("conflict"), ResultErrorType.Conflict, new[] { "conflict" }];
        yield return [Result.BusinessRule("rule"), ResultErrorType.BusinessRule, new[] { "rule" }];
        yield return [Result.Validation(["a", "b"]), ResultErrorType.Validation, new[] { "a", "b" }];
        yield return [Result.InvalidReference("reference"), ResultErrorType.InvalidReference, new[] { "reference" }];
    }

    private sealed class TestableResult(bool isSuccess, ResultErrorType? errorType, List<string> errors)
        : Result(isSuccess, errorType, errors);
}
