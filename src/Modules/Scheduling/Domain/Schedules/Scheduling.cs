using LabViroMol.Modules.Scheduling.Domain.Schedules.Policies;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public record Scheduling
{
    public DateOnly Date { get; private set; }
    public DateTimeOffset StartDateHour { get; private set; }
    public DateTimeOffset EndDateHour { get; private set; }

    public Scheduling(DateOnly date, DateTimeOffset startDateHour, DateTimeOffset endDateHour)
    {
        Date = date;
        StartDateHour = startDateHour;
        EndDateHour = endDateHour;
    }

    public static Result<Scheduling> Create(DateOnly date, DateTimeOffset start, DateTimeOffset end)
    {
        var result = BusinessTimePolicies.Validate(date, start, end);

        if (result.IsFailure)
            return Result<Scheduling>.Validation(result.Errors);

        return Result<Scheduling>.Success(new Scheduling(date, start, end));
    }
}