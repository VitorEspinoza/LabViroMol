using System.Reflection;

namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public abstract record SmartEnum<T>(string Value) : IComparable<SmartEnum<T>> 
    where T : SmartEnum<T>
{
    private static readonly Lazy<Dictionary<string, T>> _values = new(() =>
        typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(T))
            .Select(f => (T)f.GetValue(null)!)
            .ToDictionary(x => x.Value.ToLowerInvariant()));

    public static T FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("O valor não pode ser vazio.", nameof(value));

        if (!_values.Value.TryGetValue(value.ToLowerInvariant(), out var result))
        {
            var validOptions = string.Join(", ", _values.Value.Values.Select(x => x.Value));
            throw new DomainException($"'{value}' não é um {typeof(T).Name} válido. Opções: {validOptions}");
        }

        return result;
    }

    public static IEnumerable<T> List() => _values.Value.Values;

    public bool In(params T[] statuses) => statuses.Contains((T)this);
    
    public override string ToString() => Value;

    public int CompareTo(SmartEnum<T>? other) => 
        string.Compare(Value, other?.Value, StringComparison.Ordinal);
}