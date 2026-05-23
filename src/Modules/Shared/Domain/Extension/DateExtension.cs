namespace LabViroMol.Modules.Shared.Domain.Extension;

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

            var start = TimeSpan.FromHours(8);
            var end = TimeSpan.FromHours(18);

            var isWithinHours = hour >= start && hour <= end;

            return isWeekday && isWithinHours;
        }
    }
    
    
}