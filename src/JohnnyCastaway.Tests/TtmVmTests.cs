using JohnnyCastaway.Assets;
using JohnnyCastaway.Content;
using JohnnyCastaway.Ttm;
using SkiaSharp;
using Xunit;

namespace JohnnyCastaway.Tests;

public class TtmVmTests
{
    sealed class FakeStore : IAssetStore
    {
        public int Scale => 1;
        public SKImage? Background(string scr)
        {
            return null;
        }
        public IReadOnlyList<SKImage> Sprite(string bmp)
        {
            var info = new SKImageInfo(4, 4, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info) ?? throw new InvalidOperationException("SKSurface.Create returned null");
            surface.Canvas.Clear(new SKColor(255, 0, 0, 255)); // opaque red 4x4
            return new[] { surface.Snapshot() };
        }
    }

    [Fact]
    public void DrawBmpCompositesSpriteAndFinishFrameEmits()
    {
        var vm = new TtmVm(new FakeStore());
        var body = new[]
        {
            new Instr("SELECT_BMP", new[] { 3 }, null),
            new Instr("LOAD_BMP", Array.Empty<int>(), "X.BMP"),
            new Instr("DRAW_BMP", new[] { 1, 1, 0, 3 }, null),
            new Instr("SET_DELAY", new[] { 5 }, null),
            new Instr("FINISH_FRAME", Array.Empty<int>(), null),
        };
        var frames = vm.RunInstructions(Array.Empty<Instr>(), body);
        Assert.Single(frames);
        Assert.Equal(5, frames[0].DurationTicks);
        using var bmp = SKBitmap.FromImage(frames[0].Image);
        Assert.Equal(new SKColor(255, 0, 0, 255), bmp.GetPixel(2, 2)); // sprite drawn at (1,1)
        Assert.NotEqual(new SKColor(255, 0, 0, 255), bmp.GetPixel(8, 8)); // elsewhere untouched
    }
}
