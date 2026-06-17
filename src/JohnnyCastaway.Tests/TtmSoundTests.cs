using JohnnyCastaway.Content;
using JohnnyCastaway.Ttm;
using SkiaSharp;
using Xunit;

namespace JohnnyCastaway.Tests;

public class TtmSoundTests
{
    sealed class OneRedFrame : JohnnyCastaway.Assets.IAssetStore
    {
        public int Scale => 1;
        public SKImage? Background(string scr) => null;
        public IReadOnlyList<SKImage> Sprite(string bmp)
        {
            using var s = SKSurface.Create(new SKImageInfo(2, 2));
            return new[] { s.Snapshot() };
        }
    }
    static Instr I(string op, params int[] a) => new(op, a, null);

    [Fact]
    public void PlaySampleAttachesToTheFrameItOccursIn()
    {
        var vm = new TtmVm(new OneRedFrame());
        var body = new[]
        {
            I("SELECT_BMP", 1), new Instr("LOAD_BMP", Array.Empty<int>(), "X.BMP"),
            I("PLAY_SAMPLE", 7),
            I("DRAW_BMP", 0, 0, 0, 1),
            I("FINISH_FRAME"),
            I("DRAW_BMP", 0, 0, 0, 1),
            I("FINISH_FRAME"),
        };
        var frames = vm.RunInstructions(Array.Empty<Instr>(), body);
        Assert.Equal(2, frames.Count);
        Assert.Equal(new[] { 7 }, frames[0].Sounds);
        Assert.Empty(frames[1].Sounds);
    }
}
