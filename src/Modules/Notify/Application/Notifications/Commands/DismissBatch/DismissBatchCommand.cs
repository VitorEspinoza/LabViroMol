using System.Collections.Generic;
using LabViroMol.Modules.Notify.Domain.Notifications;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Notify.Application.Notifications.Commands.DismissBatch;

public record DismissBatchCommand(List<NotificationId> NotificationIds) : ICommand<Result>;