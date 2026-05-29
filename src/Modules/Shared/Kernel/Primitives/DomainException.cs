namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
