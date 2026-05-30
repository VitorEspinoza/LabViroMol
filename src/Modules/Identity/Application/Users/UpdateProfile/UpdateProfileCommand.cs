using LabViroMol.Modules.Identity.Contracts;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.UpdateProfile;

public record UpdateProfileCommand(Guid UserId, UserInfo UserData) : ICommand<Result>;
