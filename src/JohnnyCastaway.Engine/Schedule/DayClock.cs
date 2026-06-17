namespace JohnnyCastaway.Schedule;

public enum DayPart { Dawn, Day, Dusk, Night }
public enum Holiday { None, Halloween, Christmas, NewYear, Spring }

public readonly record struct DayState(int HourIndex, DayPart Part, Holiday Holiday);

public sealed class DayClock(TimeProvider time, int startOfDayHHMM)
{
    public static int Slot(int hhmm) => (hhmm / 50 + (hhmm % 100) / 30 + 14) % 16;

    public DayState Now()
    {
        DateTimeOffset t = time.GetLocalNow();
        int min = t.Minute < 30 ? 0 : 30;
        int nowHHMM = t.Hour * 100 + min;
        int hour = (Slot(nowHHMM) - Slot(startOfDayHHMM) + 16) % 16;
        DayPart part = hour switch
        {
            <= 1 => DayPart.Dawn,
            <= 9 => DayPart.Day,
            <= 11 => DayPart.Dusk,
            _ => DayPart.Night,
        };
        Holiday h = (t.Month, t.Day) switch
        {
            (10, >= 29) => Holiday.Halloween,
            (12, >= 23 and <= 25) => Holiday.Christmas,
            (1, 1) => Holiday.NewYear,
            (3, >= 15 and <= 17) => Holiday.Spring,
            _ => Holiday.None,
        };
        return new DayState(hour, part, h);
    }
}
