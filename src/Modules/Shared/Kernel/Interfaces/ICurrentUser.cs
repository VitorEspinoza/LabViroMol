using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Shared.Kernel.Interfaces;

public interface ICurrentUser
{
    UserId Id { get; }
    string FirstName { get; } 
    string LastName { get; } 
    string FullName { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }      
    IReadOnlyList<string> Permissions { get; }
}