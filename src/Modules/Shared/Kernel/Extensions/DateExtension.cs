namespace LabViroMol.Modules.Shared.Kernel.Extensions;

public static class DateExtension
{
    extension(DateOnly date)
    {
        public bool IsBusinessDay() => !date.DayOfWeek.Equals(DayOfWeek.Saturday) && !date.DayOfWeek.Equals(DayOfWeek.Sunday);
        public bool IsBefore(DateOnly to) => date < to;
    }

    extension(DateTimeOffset dateTimeOffset)
    {
        public bool IsBefore(DateTimeOffset to) => dateTimeOffset < to;
        public bool IsBusinessHour()
        {
            var dayOfWeek = dateTimeOffset.DayOfWeek;
            var hour = dateTimeOffset.TimeOfDay;

            var isWeekday = dayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;

            var start = TimeSpan.FromHours(9);
            var end = TimeSpan.FromHours(16);

            var isWithinHours = hour >= start && hour <= end;

            return isWeekday && isWithinHours;
        }
    }


}
