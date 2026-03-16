using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public record Scheduling
{
    public DateOnly Date { get; private set; }
    public DateTimeOffset StartDateHour { get; private set; }
    public DateTimeOffset EndDateHour { get; private set; }

    public Scheduling(DateOnly date, DateTimeOffset startDateHour, DateTimeOffset endDateHour)
    {
        if (date < DateOnly.FromDateTime(DateTime.Today))
            throw new DomainException("A data não pode ser no passado");

        if (startDateHour > endDateHour)
            throw new DomainException("O horário de início deve ser anterior ao horario de término");
        
        Date = date;
        StartDateHour = startDateHour;
        EndDateHour = endDateHour;
    }
}