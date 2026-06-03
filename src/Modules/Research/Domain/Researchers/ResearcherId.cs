using System;

namespace LabViroMol.Modules.Research.Domain.Researchers;

using LabViroMol.Modules.Shared.Kernel.Primitives;

public record struct ResearcherId(Guid Value) : IStrongId<ResearcherId>
{
    public static ResearcherId From(Guid value) => new(value);
    
    public static implicit operator Guid(ResearcherId id) => id.Value;
    
}
