using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.Researchers;

public record DegreeLevel : SmartEnum<DegreeLevel>
{
    public static readonly DegreeLevel Undergraduate = new("Undergraduate");
    public static readonly DegreeLevel Specialization = new("Specialization");
    public static readonly DegreeLevel Masters = new("Masters");
    public static readonly DegreeLevel Doctorate = new("Doctorate");
    public static readonly DegreeLevel PostDoctorate = new("PostDoctorate");

    private DegreeLevel(string value) : base(value) { }
}

