using System;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Shared;

public record SchedulingInput(DateOnly Date, DateTimeOffset Start, DateTimeOffset End);