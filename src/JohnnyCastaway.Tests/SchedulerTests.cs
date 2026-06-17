using JohnnyCastaway.Schedule;
using Xunit;

namespace JohnnyCastaway.Tests;

public class SchedulerTests
{
    static readonly Dictionary<string, int> Counts = new()
    {
        ["FISHING.ADS"] = 8, ["BUILDING.ADS"] = 9, ["STAND.ADS"] = 15, ["MARY.ADS"] = 5,
    };

    [Fact]
    public void PicksAnEligibleAdsForDay()
    {
        var s = new Scheduler(Counts, new Random(1));
        var v = s.Pick(new DayState(5, DayPart.Day, Holiday.None));
        Assert.Contains(v.AdsName, Counts.Keys);
        Assert.InRange(v.SegmentIndex, 0, Counts[v.AdsName] - 1);
    }

    [Fact]
    public void NightFavoursBuilding()
    {
        // BUILDING is the only Night-eligible ADS in Counts → must be chosen at night.
        var s = new Scheduler(Counts, new Random(7));
        for (int i = 0; i < 20; i++)
            Assert.Equal("BUILDING.ADS", s.Pick(new DayState(13, DayPart.Night, Holiday.None)).AdsName);
    }

    [Fact]
    public void SegmentIndexInRange()
    {
        var s = new Scheduler(Counts, new Random(3));
        for (int i = 0; i < 50; i++)
        {
            var v = s.Pick(new DayState(2, DayPart.Day, Holiday.None));
            Assert.InRange(v.SegmentIndex, 0, Counts[v.AdsName] - 1);
        }
    }
}
