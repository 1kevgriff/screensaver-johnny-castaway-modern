using JohnnyCastaway.Assets;
using JohnnyCastaway.Content;
using JohnnyCastaway.Ttm;
using SkiaSharp;
using Xunit;

namespace JohnnyCastaway.Tests;

public class TtmFrameModelTests
{
    // Asset store with 4 single-frame sprites of distinct solid colors in slot s, frame 0.
    sealed class ColorStore : IAssetStore
    {
        public int Scale => 1;
        public SKImage? Background(string scr) => null;
        public IReadOnlyList<SKImage> Sprite(string bmp)
        {
            // bmp name "Cn" -> color index n; one 1x1 frame
            int n = int.Parse(bmp.Substring(1));
            var info = new SKImageInfo(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surf = SKSurface.Create(info);
            surf.Canvas.Clear(new SKColor((byte)(n * 40), 0, 0, 255));
            return new[] { surf.Snapshot() };
        }
    }

    static Instr I(string op, params int[] a) => new(op, a, null);
    static Instr Str(string op, string s) => new(op, System.Array.Empty<int>(), s);

    // Build a TtmScript whose FULL document order is:
    //  scene1 frame: SET_SCENE 1; SELECT_BMP 1; LOAD_BMP C1; DRAW_BMP 0 0 0 1; FINISH_FRAME
    //  scene2 frame: SET_SCENE 2; SELECT_BMP 2; LOAD_BMP C2; DRAW_BMP 0 0 0 2; FINISH_FRAME
    //  back frame  : SELECT_BMP 3; LOAD_BMP C3; DRAW_BMP 0 0 0 3; FINISH_FRAME; GOTO 2
    // Rendering scene 1 should play: frame(scene1), frame(scene2), back-frame, then GOTO 2 -> loops
    //   scene2, back, scene2, back ... bounded by the cap. So the sprite-color sequence begins 1,2,3,2,3,...
    static TtmScript Script()
    {
        var seq1 = new TtmSequence("s1", new[]
        {
            I("SET_SCENE", 1), I("SELECT_BMP", 1), Str("LOAD_BMP", "C1"),
            I("DRAW_BMP", 0, 0, 0, 1), I("FINISH_FRAME"),
        });
        var seq2 = new TtmSequence("s2", new[]
        {
            I("SET_SCENE", 2), I("SELECT_BMP", 2), Str("LOAD_BMP", "C2"),
            I("DRAW_BMP", 0, 0, 0, 2), I("FINISH_FRAME"),
            I("SELECT_BMP", 3), Str("LOAD_BMP", "C3"), I("DRAW_BMP", 0, 0, 0, 3),
            I("FINISH_FRAME"), I("GOTO", 2),
        });
        return new TtmScript(3, new Dictionary<string, TtmSequence> { ["1"] = seq1, ["2"] = seq2 });
    }

    static byte Red(RenderedFrame f)
    {
        using var bmp = SKBitmap.FromImage(f.Image);
        return bmp.GetPixel(0, 0).Red;
    }

    [Fact]
    public void GotoMakesCelOrderNonLinearAndBounded()
    {
        var vm = new TtmVm(new ColorStore());
        var frames = vm.RenderSequence(Script(), "1");
        // First three cels: scene1 (40), scene2 (80), back (120), then loop scene2/back.
        Assert.True(frames.Count >= 5, $"expected a bounded loop of frames, got {frames.Count}");
        Assert.Equal(40, Red(frames[0]));
        Assert.Equal(80, Red(frames[1]));
        Assert.Equal(120, Red(frames[2]));
        Assert.Equal(80, Red(frames[3]));   // GOTO 2 looped back to scene 2
        Assert.Equal(120, Red(frames[4]));
        Assert.True(frames.Count <= 600, "must be bounded by the cap");
    }

    [Fact]
    public void SequenceStopsAtNextSceneWhenNoGoto()
    {
        // Rendering scene 1 (no goto in scene 1's frame) stops before scene 2's frame.
        var seq1 = new TtmSequence("s1", new[]
        {
            I("SET_SCENE", 1), I("SELECT_BMP", 1), new Instr("LOAD_BMP", System.Array.Empty<int>(), "C1"),
            I("DRAW_BMP", 0, 0, 0, 1), I("FINISH_FRAME"),
        });
        var seq2 = new TtmSequence("s2", new[]
        {
            I("SET_SCENE", 2), I("SELECT_BMP", 2), new Instr("LOAD_BMP", System.Array.Empty<int>(), "C2"),
            I("DRAW_BMP", 0, 0, 0, 2), I("FINISH_FRAME"),
        });
        var script = new TtmScript(2, new Dictionary<string, TtmSequence> { ["1"] = seq1, ["2"] = seq2 });
        var vm = new TtmVm(new ColorStore());
        var frames = vm.RenderSequence(script, "1");
        Assert.Single(frames);             // only scene 1's single frame
        Assert.Equal(40, Red(frames[0]));
    }
}
