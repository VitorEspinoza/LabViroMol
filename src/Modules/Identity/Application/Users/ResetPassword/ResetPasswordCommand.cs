using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.ResetPassword;

public record ResetPasswordCommand(string Email, string Token, string NewPassword)
    : ICommand<Result>;
