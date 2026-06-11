using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.AspNetCore.Http;

namespace LabViroMol.Modules.Shared.Infrastructure.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result, IResult onSuccess) =>
        result.IsSuccess ? onSuccess : result.ErrorType switch
        {
            ResultErrorType.NotFound         => Results.NotFound(new { Errors = result.Errors }),
            ResultErrorType.Conflict         => Results.Conflict(new { Errors = result.Errors }),
            ResultErrorType.Validation       => Results.BadRequest(new { Errors = result.Errors }),
            ResultErrorType.InvalidReference => Results.UnprocessableEntity(new { Errors = result.Errors }),
            ResultErrorType.BusinessRule     => Results.UnprocessableEntity(new { Errors = result.Errors }),
            _                                => Results.InternalServerError(new { Errors = result.Errors })
        };

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Data!) : result.ErrorType switch
        {
            ResultErrorType.NotFound         => Results.NotFound(new { Errors = result.Errors }),
            ResultErrorType.Conflict         => Results.Conflict(new { Errors = result.Errors }),
            ResultErrorType.Validation       => Results.BadRequest(new { Errors = result.Errors }),
            ResultErrorType.InvalidReference => Results.UnprocessableEntity(new { Errors = result.Errors }),
            ResultErrorType.BusinessRule     => Results.UnprocessableEntity(new { Errors = result.Errors }),
            _                                => Results.InternalServerError(new { Errors = result.Errors })
        };
}
