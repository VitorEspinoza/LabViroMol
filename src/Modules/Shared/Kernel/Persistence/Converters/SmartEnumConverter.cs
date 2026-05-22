using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kernel.Persistence.Converters;

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