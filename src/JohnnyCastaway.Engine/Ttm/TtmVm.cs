using JohnnyCastaway.Assets;
using JohnnyCastaway.Content;
using SkiaSharp;

namespace JohnnyCastaway.Ttm;

public sealed class TtmVm(IAssetStore assets, int seed = 1234)
{
    private const int LogicalW = 640, LogicalH = 480;
    private readonly Random _rng = new(seed);
    private readonly Dictionary<int, IReadOnlyList<SKImage>> _shapes = new();
    private readonly Dictionary<int, (int X, int Y, SKImage Img)> _getput = new();
    private int _curSlot, _getputNum, _delay = 1;
    private SKImage? _bg;
    private SKSurface _fb = null!;
    private readonly List<RenderedFrame> _frames = new();
    private readonly List<int> _pendingSounds = new();
    private int S => assets.Scale;

    private void NewFb()
    {
        int w = (_bg?.Width) ?? LogicalW * S, h = (_bg?.Height) ?? LogicalH * S;
        _fb?.Dispose();
        var created = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
        _fb = created ?? throw new InvalidOperationException("SKSurface.Create returned null");
        _fb.Canvas.Clear(SKColors.Black);
        if (_bg is not null) _fb.Canvas.DrawImage(_bg, 0, 0);
    }

    /// <summary>Render a SET_SCENE block; runs a load pre-pass over all earlier instructions.</summary>
    public IReadOnlyList<RenderedFrame> RenderSequence(TtmScript script, string seqNum)
    {
        // Flatten every sequence in document order; find target block by seqNum marker.
        var pre = new List<Instr>();
        var body = new List<Instr>();
        bool inTarget = false;
        foreach (var (key, seq) in script.Sequences)
        {
            bool isTarget = key == seqNum;
            if (isTarget) inTarget = true;
            else if (inTarget) break;          // next sequence after target ends body
            (isTarget ? body : pre).AddRange(seq.Instructions);
        }
        return RunInstructions(pre, body);
    }

    public IReadOnlyList<RenderedFrame> RunInstructions(IEnumerable<Instr> loadPass, IEnumerable<Instr> body)
    {
        _frames.Clear();
        _pendingSounds.Clear();
        NewFb();
        foreach (var i in loadPass) Exec(i, loadOnly: true);
        if (_bg is not null) NewFb();
        foreach (var i in body) Exec(i, loadOnly: false);
        return _frames;
    }

    private void Exec(Instr i, bool loadOnly)
    {
        switch (i.Op)
        {
            case "SELECT_BMP" when i.Args.Length > 0: _curSlot = i.Args[0]; return;
            case "LOAD_BMP" when i.Str is not null: _shapes[_curSlot] = assets.Sprite(i.Str); return;
            case "LOAD_SCR" when i.Str is not null:
                _bg = assets.Background(Path.GetFileNameWithoutExtension(i.Str) + ".SCR"); NewFb(); return;
            case "SET_GETPUT_NUM" when i.Args.Length > 0: _getputNum = i.Args[0]; return;
            case "SAVE_GETPUT_REGION" when i.Args.Length >= 4:
                SaveGetput(i.Args); return;
        }
        if (loadOnly) return;                  // below = draw/timing, skipped in pre-pass
        switch (i.Op)
        {
            case "PLAY_SAMPLE" when i.Args.Length > 0: _pendingSounds.Add(i.Args[0]); break;
            case "SET_DELAY" when i.Args.Length > 0: _delay = Math.Max(1, i.Args[0]); break;
            case "SET_RANDOM_DELAY" when i.Args.Length >= 2:
                int lo = Math.Min(i.Args[0], i.Args[1]), hi = Math.Max(i.Args[0], i.Args[1]);
                _delay = Math.Max(1, _rng.Next(lo, hi + 1)); break;
            case "DRAW_GETPUT" when i.Args.Length > 0:
                if (_getput.TryGetValue(i.Args[0], out var g))
                    _fb.Canvas.DrawImage(g.Img, g.X, g.Y); break;
            case "DRAW_BMP" or "DRAW_SPRITE_FLIPV" or "DRAW_SPRITE_FLIPH" or "DRAW_SPRITE_FLIPHV":
                DrawSprite(i); break;
            case "FINISH_FRAME":
                _frames.Add(new RenderedFrame(_fb.Snapshot(), _delay, _pendingSounds.ToArray()));
                _pendingSounds.Clear();
                break;
        }
    }

    private void SaveGetput(int[] a)
    {
        int x = a[0] * S, y = a[1] * S, w = a[2] * S, h = a[3] * S;
        var rect = SKRectI.Create(x, y, w, h);
        rect.Intersect(SKRectI.Create(0, 0, _fb.Canvas.DeviceClipBounds.Width, _fb.Canvas.DeviceClipBounds.Height));
        if (rect.Width <= 0 || rect.Height <= 0) return;
        using var snap = _fb.Snapshot();
        var subset = snap.Subset(rect);
        if (subset is null) return;
        _getput[_getputNum] = (rect.Left, rect.Top, subset);
    }

    private void DrawSprite(Instr i)
    {
        int x, y, frame, slot;
        if (i.Args.Length >= 4) { x = i.Args[0]; y = i.Args[1]; frame = i.Args[2]; slot = i.Args[3]; }
        else if (i.Args.Length >= 2) { x = i.Args[0]; y = i.Args[1]; frame = 0; slot = _curSlot; }
        else return;
        if (!_shapes.TryGetValue(slot, out var frames) || frame < 0 || frame >= frames.Count) return;
        var spr = frames[frame];
        var canvas = _fb.Canvas;
        canvas.Save();
        float fx = x * S, fy = y * S;
        var m = i.Op switch
        {
            "DRAW_SPRITE_FLIPH" => SKMatrix.CreateScaleTranslation(-1, 1, fx + spr.Width, fy),
            "DRAW_SPRITE_FLIPV" => SKMatrix.CreateScaleTranslation(1, -1, fx, fy + spr.Height),
            "DRAW_SPRITE_FLIPHV" => SKMatrix.CreateScaleTranslation(-1, -1, fx + spr.Width, fy + spr.Height),
            _ => SKMatrix.CreateTranslation(fx, fy),
        };
        canvas.SetMatrix(m);
        canvas.DrawImage(spr, 0, 0);
        canvas.Restore();
    }
}
