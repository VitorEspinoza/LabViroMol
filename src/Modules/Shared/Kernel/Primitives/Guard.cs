namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public static class Guard
{
    public static string AgainstEmpty(string? value, string exceptionMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(exceptionMessage);

        return value;
    }

    public static string AgainstMinLength(string? value, int minLength, string exceptionMessage)
    {
        var safeValue = AgainstEmpty(value, exceptionMessage);

        if (safeValue.Length < minLength)
            throw new DomainException(exceptionMessage);

        return safeValue;
    }
}
