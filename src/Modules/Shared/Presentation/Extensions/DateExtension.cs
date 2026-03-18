namespace LabViroMol.Modules.Shared.Presentation.Extensions;

public static class DateExtension
{
    extension(DateOnly date)
    {
        public bool IsBusinessDay() => !date.DayOfWeek.Equals(DayOfWeek.Saturday) && !date.DayOfWeek.Equals(DayOfWeek.Sunday);
        public bool IsBefore(DateOnly to) => date < to;
    }

    public static bool IsBefore(this DateTimeOffset dateTimeOffset, DateTimeOffset to) => dateTimeOffset < to;
    
}