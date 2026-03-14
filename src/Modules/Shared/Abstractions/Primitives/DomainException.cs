namespace LabViroMol.Modules.Shared.Abstractions.Primitives;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
