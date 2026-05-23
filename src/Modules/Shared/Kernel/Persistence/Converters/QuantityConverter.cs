using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kernel.Persistence.Converters;


public class QuantityConverter : ValueConverter<Quantity, decimal>
{
    public QuantityConverter() 
        : base(v => v.Value, v => new Quantity(v)) { }
}
