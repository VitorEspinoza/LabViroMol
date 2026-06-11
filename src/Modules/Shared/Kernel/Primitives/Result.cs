namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public enum ResultErrorType { Validation, NotFound, Conflict, BusinessRule, InvalidReference }

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public List<string> Errors { get; }
    public ResultErrorType? ErrorType { get; }

    protected Result(bool isSuccess, ResultErrorType? errorType, List<string> errors)
    {
        if (isSuccess && errors.Any())
            throw new InvalidOperationException("Um resultado de sucesso não pode conter erros.");

        if (!isSuccess && !errors.Any())
            throw new InvalidOperationException("Um resultado de falha precisa de ao menos um erro.");

        IsSuccess = isSuccess;
        ErrorType = errorType;
        Errors = errors;
    }

    public static Result Success()
        => new(true, null, new List<string>());

    public static Result NotFound(string error)
        => new(false, ResultErrorType.NotFound, new List<string> { error });

    public static Result BusinessRule(string error)
        => new(false, ResultErrorType.BusinessRule, new List<string> { error });

    public static Result Conflict(string error)
        => new(false, ResultErrorType.Conflict, new List<string> { error });

    public static Result Validation(List<string> errors)
        => new(false, ResultErrorType.Validation, errors);
    
    public static Result InvalidReference(string error)
        => new(false, ResultErrorType.InvalidReference, new List<string> { error });
}

public class Result<T> : Result
{
    public T? Data { get; }

    protected Result(T? data, bool isSuccess, ResultErrorType? errorType, List<string> errors)
        : base(isSuccess, errorType, errors)
    {
        Data = data;
    }

    public static Result<T> Success(T data)
        => new(data, true, null, new List<string>());

    public new static Result<T> NotFound(string error)
        => new(default, false, ResultErrorType.NotFound, new List<string> { error });

    public new static Result<T> BusinessRule(string error)
        => new(default, false, ResultErrorType.BusinessRule, new List<string> { error });

    public new static Result<T> Conflict(string error)
        => new(default, false, ResultErrorType.Conflict, new List<string> { error });

    public new static Result<T> Validation(List<string> errors)
        => new(default, false, ResultErrorType.Validation, errors);
    
    public new static Result<T> InvalidReference(List<string> errors)
        => new(default, false, ResultErrorType.InvalidReference, errors);

    public static Result<T> FromError(Result source) => source.ErrorType switch
    {
        ResultErrorType.NotFound         => NotFound(source.Errors[0]),
        ResultErrorType.Conflict         => Conflict(source.Errors[0]),
        ResultErrorType.BusinessRule     => BusinessRule(source.Errors[0]),
        ResultErrorType.InvalidReference => InvalidReference(source.Errors),
        _                                => Validation(source.Errors),
    };

    public static implicit operator Result<T>(T value) => Success(value);
}
