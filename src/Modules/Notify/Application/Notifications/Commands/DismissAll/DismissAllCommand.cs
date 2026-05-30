using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Notifications.Commands.DismissAll;

public record DismissAllCommand() : ICommand<Result>;