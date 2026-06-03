using System.IO;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.UploadTerm;

public record UploadTermCommand(ScheduleId ScheduleId, Stream Stream, string FileName) : ICommand<Result>;