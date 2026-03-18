using System.Data;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using LabViroMol.Modules.Shared.Presentation.Extensions;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public record Scheduling
{
    public DateOnly Date { get; private set; }
    public DateTimeOffset StartDateHour { get; private set; }
    public DateTimeOffset EndDateHour { get; private set; }

    public Scheduling(DateOnly date, DateTimeOffset startDateHour, DateTimeOffset endDateHour)
    {
        if (date.IsBefore(DateOnly.FromDateTime(DateTime.Today)))
            throw new DomainException("A data não pode ser no passado.");

        if (endDateHour.IsBefore(startDateHour))
            throw new DomainException("O horário de início deve ser anterior ao horário de término.");

        if (!date.IsBusinessDay())
            Result.BusinessRule("A data não representa um dia útil.");
        
        Date = date;
        StartDateHour = startDateHour;
        EndDateHour = endDateHour;
    }
}