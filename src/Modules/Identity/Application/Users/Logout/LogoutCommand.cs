using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.Logout;

public record LogoutCommand(Guid UserId) : ICommand<Result>;
