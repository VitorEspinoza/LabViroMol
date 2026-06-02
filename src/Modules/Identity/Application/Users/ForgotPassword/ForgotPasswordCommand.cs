using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Identity.Application.Users.ForgotPassword;

public record ForgotPasswordCommand(string Email) : ICommand<Result<string>>;
