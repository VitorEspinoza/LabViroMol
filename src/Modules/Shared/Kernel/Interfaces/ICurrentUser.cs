using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Shared.Kernel.Interfaces;

public interface ICurrentUser
{
    UserId Id { get; }
    string Name { get; }
}