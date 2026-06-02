using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.RefreshToken;

public record RefreshTokenCommand(string RefreshToken)
    : ICommand<Result<(string AccessToken, string RefreshToken)>>;
