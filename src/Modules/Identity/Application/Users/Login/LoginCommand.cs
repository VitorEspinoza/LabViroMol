using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.Login;

public record LoginCommand(string Email, string Password)
    : ICommand<Result<(string AccessToken, string RefreshToken)>>;
