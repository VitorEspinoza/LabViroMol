using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Interfaces;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Shared.Infrastructure;

public class CurrentUserMock : ICurrentUser
{
    public UserId Id { get; } =  IdFactory.New<UserId>();
    public string Name { get; } = "Mock User";
}