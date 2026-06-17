using JohnnyCastaway.Ads;
using JohnnyCastaway.Content;
using JohnnyCastaway.ScreenSaver;
using JohnnyCastaway.Ttm;
using SkiaSharp;
using Xunit;

namespace JohnnyCastaway.Tests;

public class AdsVignettePlayerTests
{
    sealed class FakeRenderer : IClipRenderer
    {
        public IReadOnlyList<RenderedFrame> Render(string ttm, int seq)
        {
            using var s = SKSurface.Create(new SKImageInfo(2, 2));
            return new[] { new RenderedFrame(s.Snapshot(), 2, Array.Empty<int>()), new RenderedFrame(s.Snapshot(), 3, Array.Empty<int>()) };
        }
    }

    static Instr I(string op, params int[] a) => new(op, a, null);

    [Fact]
    public void ConcatenatesPlannedClipFramesIntoOneScenePlayer()
    {
        var ads = new AdsScript(
            new Dictionary<string, string> { ["1"] = "A.TTM" },
            new[] { new AdsSegment("s", new[]
            {
                I("ADD_SEQ", 1, 1, 0, 1),
                I("IF_FINISHED", 1, 1), I("ADD_SEQ", 1, 2, 0, 1), I("END_IF"),
            }) });
        var dir = new AdsDirector(ads, new FakeRenderer(), new Random(1));
        var player = AdsVignettePlayer.Build(dir, new FakeRenderer(), segmentIndex: 0);
        // 2 clips x 2 frames = 4 frames; durations 2,3,2,3 ticks @ default 50ms = 500ms total
        Assert.Equal(4, player.FrameCount);
        Assert.Equal((2 + 3 + 2 + 3) * 50, player.TotalMs);
    }
}
