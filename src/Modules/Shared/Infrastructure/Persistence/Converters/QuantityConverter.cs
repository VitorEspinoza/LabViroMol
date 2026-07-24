using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Converters;


public class QuantityConverter : ValueConverter<Quantity, decimal>
{
    public QuantityConverter()
        : base(v => v.Value, v => new Quantity(v)) { }
}
