using LabViroMol.Modules.Scheduling.Domain.Schedules.Policies;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Xunit;

namespace LabViroMol.Modules.Scheduling.Domain.UnitTests.Policies;

public class BusinessTimePoliciesTests
{
    [Fact]
    public void Validate_WhenDateIsInPast_ThrowsDomainException()
    {
        var date = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(9, 0)));
        var end = new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)));

        Assert.Throws<DomainException>(() => BusinessTimePolicies.Validate(date, start, end));
    }

    [Fact]
    public void Validate_WhenDateIsWeekend_ReturnsBusinessRuleFailure()
    {
        var date = NextDate(DayOfWeek.Saturday);
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(9, 0)));
        var end = new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)));

        var result = BusinessTimePolicies.Validate(date, start, end);

        Assert.True(result.IsFailure);
        Assert.Contains("dia útil", result.Errors[0]);
    }

    [Fact]
    public void Validate_WhenStartIsOutsideBusinessHours_ReturnsBusinessRuleFailure()
    {
        var date = NextBusinessDay();
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(8, 59)));
        var end = new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)));

        var result = BusinessTimePolicies.Validate(date, start, end);

        Assert.True(result.IsFailure);
        Assert.Contains("horário inicial", result.Errors[0]);
    }

    [Fact]
    public void Validate_WhenEndIsOutsideBusinessHours_ReturnsBusinessRuleFailure()
    {
        var date = NextBusinessDay();
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(9, 0)));
        var end = new DateTimeOffset(date.ToDateTime(new TimeOnly(16, 1)));

        var result = BusinessTimePolicies.Validate(date, start, end);

        Assert.True(result.IsFailure);
        Assert.Contains("horário final", result.Errors[0]);
    }

    [Fact]
    public void Validate_WhenEndIsBeforeStart_ReturnsBusinessRuleFailure()
    {
        var date = NextBusinessDay();
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(15, 0)));
        var end = new DateTimeOffset(date.ToDateTime(new TimeOnly(14, 0)));

        var result = BusinessTimePolicies.Validate(date, start, end);

        Assert.True(result.IsFailure);
        Assert.Contains("após o inicial", result.Errors[0]);
    }

    [Fact]
    public void Validate_WhenTimesBelongToDifferentDate_ThrowsDomainException()
    {
        var date = NextBusinessDay();
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(9, 0)));

        var nextDay = date.AddDays(1);
        while (nextDay.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            nextDay = nextDay.AddDays(1);
        var end = new DateTimeOffset(nextDay.ToDateTime(new TimeOnly(10, 0)));

        Assert.Throws<DomainException>(() => BusinessTimePolicies.Validate(date, start, end));
    }

    [Fact]
    public void Validate_WhenInputIsValid_ReturnsSuccess()
    {
        var date = NextBusinessDay();
        var start = new DateTimeOffset(date.ToDateTime(new TimeOnly(9, 0)));
        var end = new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)));

        var result = BusinessTimePolicies.Validate(date, start, end);

        Assert.True(result.IsSuccess);
    }

    private static DateOnly NextBusinessDay()
    {
        var date = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private static DateOnly NextDate(DayOfWeek dayOfWeek)
    {
        var date = DateOnly.FromDateTime(DateTime.Today).AddDays(1);
        while (date.DayOfWeek != dayOfWeek)
        {
            date = date.AddDays(1);
        }

        return date;
    }
}
