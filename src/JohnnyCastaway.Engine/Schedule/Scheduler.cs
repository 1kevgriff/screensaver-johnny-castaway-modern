namespace JohnnyCastaway.Schedule;

public readonly record struct Vignette(string AdsName, int SegmentIndex);

public sealed class Scheduler(IReadOnlyDictionary<string, int> adsSegmentCounts, Random rng)
{
    private sealed record Rule(DayPart[] Parts, int Weight);

    private static readonly Dictionary<string, Rule> Table = new()
    {
        ["FISHING.ADS"]  = new([DayPart.Day, DayPart.Dusk], 3),
        ["STAND.ADS"]    = new([DayPart.Day, DayPart.Dawn, DayPart.Dusk], 2),
        ["ACTIVITY.ADS"] = new([DayPart.Day], 3),
        ["VISITOR.ADS"]  = new([DayPart.Day, DayPart.Dusk], 2),
        ["MISCGAG.ADS"]  = new([DayPart.Day], 2),
        ["WALKSTUF.ADS"] = new([DayPart.Day, DayPart.Dawn], 2),
        ["MARY.ADS"]     = new([DayPart.Day, DayPart.Dusk], 1),
        ["SUZY.ADS"]     = new([DayPart.Day], 1),
        ["BUILDING.ADS"] = new([DayPart.Night, DayPart.Dusk], 3),
        ["JOHNNY.ADS"]   = new([DayPart.Day, DayPart.Night], 1),
    };

    public Vignette Pick(DayState state)
    {
        var opts = new List<(string ads, int weight)>();
        foreach (var (ads, count) in adsSegmentCounts)
        {
            if (count <= 0) continue;
            var rule = Table.TryGetValue(ads.ToUpperInvariant(), out var r) ? r : new Rule([DayPart.Day], 1);
            if (Array.IndexOf(rule.Parts, state.Part) >= 0)
                opts.Add((ads, rule.Weight));
        }
        if (opts.Count == 0)  // fallback: anything available
            foreach (var (ads, count) in adsSegmentCounts)
                if (count > 0) opts.Add((ads, 1));

        string chosen = WeightedPick(opts);
        int seg = rng.Next(adsSegmentCounts[chosen]);
        return new Vignette(chosen, seg);
    }

    private string WeightedPick(List<(string ads, int weight)> opts)
    {
        int total = 0;
        foreach (var o in opts) total += o.weight;
        int roll = rng.Next(total);
        foreach (var o in opts) { if (roll < o.weight) return o.ads; roll -= o.weight; }
        return opts[^1].ads;
    }
}
