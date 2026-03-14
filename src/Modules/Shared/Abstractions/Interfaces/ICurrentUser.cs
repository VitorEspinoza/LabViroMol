using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Shared.Abstractions.Interfaces;

public interface ICurrentUser
{
    UserId Id { get; }
    string Name { get; }
}