namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public record Quantity
{
    public Quantity(decimal value)
    {
        if (value < 0)
            throw new DomainException("A quantidade não pode ser negativa.");
        
        Value = value;
    }

    public decimal Value { get; init; }
 
    public static implicit operator decimal(Quantity quantity) => quantity.Value;

    public static explicit operator Quantity(decimal value) => new(value);

    public static Quantity operator +(Quantity a, Quantity b) => new(a.Value + b.Value);

    public static Quantity operator -(Quantity a, Quantity b) => new(a.Value - b.Value);
}