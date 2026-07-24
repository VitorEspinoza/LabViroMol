using FluentValidation;
using LabViroMol.Modules.Shared.Infrastructure.Behaviors;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Shared.Infrastructure.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithoutValidators_InvokesNext()
    {
        var behavior = new ValidationBehavior<ValidationResultCommand, Result>([]);
        var nextCalled = false;

        var response = await behavior.Handle(
            new ValidationResultCommand("ok"),
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.FromResult(Result.Success());
            },
            CancellationToken.None);

        Assert.True(nextCalled);
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public async Task Handle_WithValidationErrorsForResult_ReturnsValidationResultWithoutCallingNext()
    {
        var validators = new IValidator<ValidationResultCommand>[]
        {
            new EmptyNameValidator(),
            new DuplicateFailureValidator(),
            new DuplicateFailureValidator(),
        };

        var behavior = new ValidationBehavior<ValidationResultCommand, Result>(validators);
        var nextCalled = false;

        var response = await behavior.Handle(
            new ValidationResultCommand(""),
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.FromResult(Result.Success());
            },
            CancellationToken.None);

        Assert.False(nextCalled);
        Assert.True(response.IsFailure);
        Assert.Equal(ResultErrorType.Validation, response.ErrorType);
        Assert.Equal(["name is required", "duplicate failure"], response.Errors);
    }

    [Fact]
    public async Task Handle_WithValidationErrorsForGenericResult_ReturnsValidationResultWithoutCallingNext()
    {
        var behavior = new ValidationBehavior<ValidationGenericResultCommand, Result<string>>(
            [new GenericResultValidator()]);

        var nextCalled = false;

        var response = await behavior.Handle(
            new ValidationGenericResultCommand("blocked"),
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.FromResult(Result<string>.Success("ok"));
            },
            CancellationToken.None);

        Assert.False(nextCalled);
        Assert.True(response.IsFailure);
        Assert.Equal(ResultErrorType.Validation, response.ErrorType);
        Assert.Equal(["generic failure"], response.Errors);
        Assert.Null(response.Data);
    }

    [Fact]
    public async Task Handle_WithValidationErrorsForNonResult_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<ValidationTextCommand, string>(
            [new TextCommandValidator()]);

        await Assert.ThrowsAsync<ValidationException>(async () =>
        {
            await behavior.Handle(
                new ValidationTextCommand("forbidden"),
                (_, _) => ValueTask.FromResult("ok"),
                CancellationToken.None);
        });
    }

    [Fact]
    public async Task Handle_WithValidMessage_InvokesNext()
    {
        var behavior = new ValidationBehavior<ValidationGenericResultCommand, Result<string>>(
            [new GenericResultValidator()]);

        var nextCalled = false;

        var response = await behavior.Handle(
            new ValidationGenericResultCommand("allowed"),
            (_, _) =>
            {
                nextCalled = true;
                return ValueTask.FromResult(Result<string>.Success("ok"));
            },
            CancellationToken.None);

        Assert.True(nextCalled);
        Assert.True(response.IsSuccess);
        Assert.Equal("ok", response.Data);
    }

    public sealed record ValidationResultCommand(string Name) : ICommand<Result>;

    public sealed record ValidationGenericResultCommand(string Name) : ICommand<Result<string>>;

    public sealed record ValidationTextCommand(string Name) : ICommand<string>;

    private sealed class EmptyNameValidator : AbstractValidator<ValidationResultCommand>
    {
        public EmptyNameValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("name is required");
        }
    }

    private sealed class DuplicateFailureValidator : AbstractValidator<ValidationResultCommand>
    {
        public DuplicateFailureValidator()
        {
            RuleFor(x => x.Name)
                .Must(_ => false)
                .WithMessage("duplicate failure");
        }
    }

    private sealed class GenericResultValidator : AbstractValidator<ValidationGenericResultCommand>
    {
        public GenericResultValidator()
        {
            RuleFor(x => x.Name)
                .NotEqual("blocked")
                .WithMessage("generic failure");
        }
    }

    private sealed class TextCommandValidator : AbstractValidator<ValidationTextCommand>
    {
        public TextCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEqual("forbidden")
                .WithMessage("plain text failure");
        }
    }
}
