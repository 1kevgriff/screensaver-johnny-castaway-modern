using JohnnyCastaway.Content;
using Xunit;

namespace JohnnyCastaway.Tests;

public class ContentBundleTests
{
    static string ContentDir =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content");

    [Fact]
    public void LoadsTtmSequenceWithDrawAndBackground()
    {
        var bundle = ContentBundle.Load(ContentDir);
        var seq = bundle.Ttm["SJWORK.TTM"].Sequences["3"];
        Assert.StartsWith("J in the office", seq.Label);
        Assert.Contains(seq.Instructions, i => i.Op == "LOAD_SCR" && i.Str == "JOFFICE.SCR");
        Assert.Contains(seq.Instructions, i => i.Op == "DRAW_BMP");
    }

    [Fact]
    public void LoadsAdsWithResolvedTtmRef()
    {
        var bundle = ContentBundle.Load(ContentDir);
        var fishing = bundle.Ads["FISHING.ADS"];
        Assert.Equal("MJFISH.TTM", fishing.Res["1"]);
        Assert.Contains(fishing.Segments, s => s.Instructions.Any(i => i.Op == "ADD_SEQ"));
    }
}
