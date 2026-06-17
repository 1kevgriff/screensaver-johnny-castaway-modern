using JohnnyCastaway.Schedule;
using Xunit;

namespace JohnnyCastaway.Tests;

public class ScheduledProviderTests
{
    // The provider composition (DayClock+Scheduler) is exercised here at the unit level:
    // a Scheduler over real-ish counts must always return a loadable vignette for any DayPart.
    [Theory]
    [InlineData(DayPart.Dawn)]
    [InlineData(DayPart.Day)]
    [InlineData(DayPart.Dusk)]
    [InlineData(DayPart.Night)]
    public void SchedulerAlwaysYieldsAVignetteForEveryPart(DayPart part)
    {
        var counts = new Dictionary<string, int>
        {
            ["FISHING.ADS"] = 8, ["BUILDING.ADS"] = 9, ["STAND.ADS"] = 15,
            ["ACTIVITY.ADS"] = 10, ["JOHNNY.ADS"] = 6,
        };
        var s = new Scheduler(counts, new Random(11));
        var v = s.Pick(new DayState(0, part, Holiday.None));
        Assert.Contains(v.AdsName, counts.Keys);
        Assert.InRange(v.SegmentIndex, 0, counts[v.AdsName] - 1);
    }
}
