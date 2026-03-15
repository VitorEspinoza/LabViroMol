using LabViroMol.Modules.Inventory.Domain.Materials;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LabViroMol.Modules.Inventory.Infrastructure.Persistence.Converters;


internal class QuantityConverter : ValueConverter<Quantity, decimal>
{
    public QuantityConverter() 
        : base(v => v.Value, v => new Quantity(v)) { }
}
