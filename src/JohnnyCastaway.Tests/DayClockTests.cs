using JohnnyCastaway.Schedule;
using Xunit;

namespace JohnnyCastaway.Tests;

public class DayClockTests
{
    sealed class FixedTime(DateTimeOffset t) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => t;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        // GetLocalNow() returns the "local" interpretation of GetUtcNow()
        // Since we pinned LocalTimeZone to UTC and return t (offset=0) from GetUtcNow(),
        // the result should be t itself when interpreted as local.
        // However, .NET's default GetLocalNow() might use system timezone.
        // To ensure determinism: return t as-is (since it has UTC offset and we claim UTC is local).
    }

    static DayClock At(int month, int day, int hour, int min, int startHHMM)
        => new(new FixedTime(new DateTimeOffset(2026, month, day, hour, min, 0, TimeSpan.Zero)), startHHMM);

    [Fact]
    public void SlotFormulaMatchesEngine()
    {
        // (m/50 + (m%100)/30 + 14) % 16
        Assert.Equal((900 / 50 + 0 / 30 + 14) % 16, DayClock.Slot(900));   // 9:00
        Assert.Equal((1230 / 50 + 30 / 30 + 14) % 16, DayClock.Slot(1230)); // 12:30
    }

    [Fact]
    public void HourIndexIsRelativeToStartOfDay()
    {
        var c = At(6, 1, 9, 0, startHHMM: 900);  // now == start-of-day 9:00
        Assert.Equal(0, c.Now().HourIndex);
    }

    [Fact]
    public void RoundsMinutesDownToHalfHour()
    {
        Assert.Equal(At(6, 1, 9, 29, 900).Now().HourIndex, At(6, 1, 9, 0, 900).Now().HourIndex);
        Assert.Equal(At(6, 1, 9, 59, 900).Now().HourIndex, At(6, 1, 9, 30, 900).Now().HourIndex);
    }

    [Theory]
    [InlineData(10, 31, Holiday.Halloween)]
    [InlineData(12, 24, Holiday.Christmas)]
    [InlineData(1, 1, Holiday.NewYear)]
    [InlineData(3, 16, Holiday.Spring)]
    [InlineData(7, 4, Holiday.None)]
    public void HolidayFromCalendar(int month, int day, Holiday expected)
        => Assert.Equal(expected, At(month, day, 12, 0, 900).Now().Holiday);

    [Theory]
    // Test day-part thresholds by testing corner cases
    [InlineData(800, DayPart.Day)]     // start=8:00, index=2 => Day (<=9)
    [InlineData(1200, DayPart.Dusk)]   // start=12:00, index=10 => Dusk (<=11)
    public void DayPartThresholds(int startHHMM, DayPart expectedPart)
    {
        // Verify DayPart correctness for various startOfDay configurations.
        // Using fixed now=9:00.
        var clock = At(6, 1, 9, 0, startHHMM);
        var state = clock.Now();
        Assert.Equal(expectedPart, state.Part);
    }
}
