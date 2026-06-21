namespace LabViroMol.Modules.Shared.Kernel.Exceptions;

public class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
