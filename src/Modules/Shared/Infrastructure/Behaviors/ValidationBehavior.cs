using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Mediator;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Shared.Infrastructure.Behaviors;


public sealed class ValidationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly IEnumerable<IValidator<TMessage>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TMessage>> validators)
    {
        _validators = validators;
    }
    private static TResponse CreateResultWithError(List<string> errors)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Validation(errors);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var errorMethod = responseType.GetMethod(
                "Validation",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy,
                null,
                new[] { typeof(List<string>) },
                null);

            if (errorMethod != null)
            {
                return (TResponse)errorMethod.Invoke(null, new object[] { errors })!;
            }
        }


        var validationFailures = errors.Select(e => new FluentValidation.Results.ValidationFailure(string.Empty, e));
        throw new ValidationException(validationFailures);
    }

    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(message, cancellationToken);
        }

        var context = new ValidationContext<TMessage>(message);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .Select(f => f.ErrorMessage)
            .Distinct() 
            .ToList();

        if (errors.Any())
        {
            return CreateResultWithError(errors);
        }

        return await next(message, cancellationToken);
    }
}