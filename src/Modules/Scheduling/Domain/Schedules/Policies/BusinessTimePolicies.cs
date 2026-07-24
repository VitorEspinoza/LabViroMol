using LabViroMol.Modules.Shared.Kernel.Primitives;
using LabViroMol.Modules.Shared.Kernel.Extensions;

namespace LabViroMol.Modules.Scheduling.Domain.Schedules.Policies;

public static class BusinessTimePolicies
{
    public static Result Validate(DateOnly date, DateTimeOffset start, DateTimeOffset end)
    {
        if (date.IsBefore(DateOnly.FromDateTime(DateTime.Today)))
            throw new DomainException("A data não pode ser no passado.");

        if (!date.IsBusinessDay())
            return Result.BusinessRule("A data deve ser um dia útil.");

        if (!start.IsBusinessHour())
            return Result.BusinessRule("O horário inicial está fora do horário comercial.");

        if (!end.IsBusinessHour())
            return Result.BusinessRule("O horário final está fora do horário comercial.");

        if (end.IsBefore(start))
            return Result.BusinessRule("O horário final deve ser após o inicial.");

        if (start.Date != date.ToDateTime(TimeOnly.MinValue).Date ||
            end.Date != date.ToDateTime(TimeOnly.MinValue).Date)
        {
            throw new DomainException("Os horários devem pertencer à mesma data.");
        }

        return Result.Success();
    }
}