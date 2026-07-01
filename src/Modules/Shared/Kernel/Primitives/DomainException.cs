namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException() : base()
    {
    }

    public DomainException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
