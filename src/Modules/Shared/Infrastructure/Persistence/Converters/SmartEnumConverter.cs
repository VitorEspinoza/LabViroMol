using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LabViroMol.Modules.Shared.Infrastructure.Persistence.Converters;

public class SmartEnumConverter<TEnum> : ValueConverter<TEnum, string>
    where TEnum : SmartEnum<TEnum>
{
    public SmartEnumConverter()
        : base(
            v => v.Value,
            v => SmartEnum<TEnum>.FromString(v))
    {
    }
}