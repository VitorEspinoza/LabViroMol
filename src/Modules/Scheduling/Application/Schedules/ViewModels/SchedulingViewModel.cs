using System;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.ViewModels;

public record SchedulingViewModel(DateOnly Date, DateTimeOffset StartDateHour, DateTimeOffset EndDateHour);