using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace Kernel;

public class CurrentUserMock : ICurrentUser
{
    public UserId Id { get; } =  IdFactory.New<UserId>();
    public string Name { get; } = "Mock User";
}