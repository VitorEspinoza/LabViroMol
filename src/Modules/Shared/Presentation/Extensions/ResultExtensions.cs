using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Microsoft.AspNetCore.Http;

namespace LabViroMol.Modules.Shared.Presentation.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result, IResult onSuccess) =>
        result.IsSuccess ? onSuccess : result.ErrorType switch
        {
            ResultErrorType.NotFound   => Results.NotFound(new { Errors = result.Errors }),
            ResultErrorType.Conflict   => Results.Conflict(new { Errors = result.Errors }),
            ResultErrorType.Validation => Results.BadRequest(new { Errors = result.Errors }),
            _                          => Results.UnprocessableEntity(new { Errors = result.Errors })
        };
}
