using JohnnyCastaway.Assets;
using JohnnyCastaway.Content;
using SkiaSharp;

namespace JohnnyCastaway.Ttm;

public sealed class TtmVm(IAssetStore assets, int seed = 1234)
{
    private const int LogicalW = 640, LogicalH = 480;
    private const int MaxFrames = 600;
    private readonly Random _rng = new(seed);
    private readonly Dictionary<int, IReadOnlyList<SKImage>> _shapes = new();
    private readonly Dictionary<int, (int X, int Y, SKImage Img)> _getput = new();
    private int _curSlot, _getputNum, _delay = 1;
    private SKImage? _bg;
    private SKSurface _fb = null!;
    private readonly List<RenderedFrame> _frames = new();
    private readonly List<int> _pendingSounds = new();

    // Set by Exec when it encounters GOTO; the RenderSequence walk loop reads+clears this
    // and resolves it to a frame index via the scene->frame table.
    private int? _pendingGotoScene;

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

    /// <summary>
    /// Concatenate all sequences in insertion (document) order to produce the full TTM instruction stream.
    /// </summary>
    private static IReadOnlyList<Instr> FlattenAll(TtmScript script)
    {
        var all = new List<Instr>();
        foreach (var (_, seq) in script.Sequences)
            all.AddRange(seq.Instructions);
        return all;
    }

    /// <summary>
    /// Split a flat instruction list into frames. Each frame is the slice of instructions up to and
    /// including a FINISH_FRAME. A trailing GOTO (or any instruction) after the last FINISH_FRAME
    /// attaches to the last frame.
    /// </summary>
    private static List<List<Instr>> SplitIntoFrames(IReadOnlyList<Instr> all)
    {
        var frames = new List<List<Instr>>();
        var cur = new List<Instr>();
        foreach (var instr in all)
        {
            cur.Add(instr);
            if (instr.Op == "FINISH_FRAME")
            {
                frames.Add(cur);
                cur = new List<Instr>();
            }
        }
        // Trailing instructions after last FINISH_FRAME (e.g. a GOTO) attach to last frame.
        if (cur.Count > 0)
        {
            if (frames.Count > 0)
                frames[^1].AddRange(cur);
            else
                frames.Add(cur); // edge case: no FINISH_FRAME at all
        }
        return frames;
    }

    /// <summary>
    /// Build a map from SET_SCENE argument value to frame index.
    /// A frame whose first instruction is SET_SCENE n is the start of scene n.
    /// </summary>
    private static Dictionary<int, int> BuildSceneToFrame(List<List<Instr>> frames)
    {
        var map = new Dictionary<int, int>();
        for (int i = 0; i < frames.Count; i++)
        {
            var frame = frames[i];
            if (frame.Count > 0 && frame[0].Op == "SET_SCENE" && frame[0].Args.Length >= 1)
                map[frame[0].Args[0]] = i;
        }
        return map;
    }

    /// <summary>
    /// Render a SET_SCENE block using the DGDS frame model.
    /// Pre-passes all frames before the target sequence in loadOnly mode to set up assets and
    /// getput regions, then walks frames from the sequence start, honouring GOTO for non-linear
    /// cel ordering. Stops when entering a different scene sequentially that is not explicitly
    /// GOTO-targeted anywhere in the TTM, or when hitting the frame cap (600).
    /// </summary>
    public IReadOnlyList<RenderedFrame> RenderSequence(TtmScript script, string seqNum)
    {
        _frames.Clear();
        _pendingSounds.Clear();
        _pendingGotoScene = null;

        var all = FlattenAll(script);
        var frames = SplitIntoFrames(all);
        var sceneToFrame = BuildSceneToFrame(frames);

        // Pre-compute the set of scenes that are explicitly GOTO-targeted in this TTM.
        // When advancing sequentially into a scene boundary, we only stop if that scene is NOT
        // a GOTO target — if it IS a GOTO target, it is part of the animation loop and must
        // be traversed even on the initial sequential pass leading to the first GOTO occurrence.
        var gotoTargetedScenes = new HashSet<int>();
        foreach (var instr in all)
            if (instr.Op == "GOTO" && instr.Args.Length > 0)
                gotoTargetedScenes.Add(instr.Args[0]);

        int seqScene = int.Parse(seqNum);
        int start = sceneToFrame.GetValueOrDefault(seqScene, 0);

        // Load pre-pass: execute frames [0, start) in loadOnly mode so assets/getput regions are primed.
        NewFb();
        for (int fi = 0; fi < start; fi++)
            foreach (var instr in frames[fi])
                Exec(instr, loadOnly: true);

        // Reset to a clean framebuffer now that assets are loaded.
        NewFb();

        // Render walk.
        int cur = start;
        int emitted = 0;
        int? pendingGoto = null;
        bool arrivedViaGoto = false;

        while (true)
        {
            if (cur < 0 || cur >= frames.Count)
                break;

            // Stop if we've entered a different scene's start frame via sequential advancement
            // AND that scene is not a GOTO target anywhere in the TTM. A GOTO-targeted scene is
            // part of the animation loop and must be traversed on the sequential pre-pass that
            // leads up to the frame containing the GOTO. When arriving via GOTO, skip this check.
            if (!arrivedViaGoto && cur != start && frames[cur].Count > 0)
            {
                var firstOp = frames[cur][0];
                if (firstOp.Op == "SET_SCENE" && firstOp.Args.Length >= 1
                    && firstOp.Args[0] != seqScene
                    && !gotoTargetedScenes.Contains(firstOp.Args[0]))
                    break;
            }

            // Execute all instructions in the current frame.
            _pendingGotoScene = null;
            foreach (var instr in frames[cur])
                Exec(instr, loadOnly: false);

            // If Exec recorded a GOTO, resolve it to a frame index.
            if (_pendingGotoScene.HasValue)
                pendingGoto = sceneToFrame.GetValueOrDefault(_pendingGotoScene.Value, -1);

            emitted++;
            if (emitted >= MaxFrames)
                break;

            int next = pendingGoto ?? (cur + 1);
            arrivedViaGoto = pendingGoto.HasValue;
            cur = next;
            pendingGoto = null;
        }

        return _frames;
    }

    /// <summary>
    /// Linear instruction executor used by the existing unit tests (TtmVmTests).
    /// RenderSequence no longer routes through this — it uses the frame model above.
    /// </summary>
    public IReadOnlyList<RenderedFrame> RunInstructions(IEnumerable<Instr> loadPass, IEnumerable<Instr> body)
    {
        _frames.Clear();
        _pendingSounds.Clear();
        _pendingGotoScene = null;
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
            case "GOTO" when i.Args.Length > 0:
                // Record the raw scene number; RenderSequence's walk loop resolves to frame index.
                _pendingGotoScene = i.Args[0];
                break;
            // SET_FRAME (0x2010): sets a frame variable used by some sequences.
            // Treated as a no-op for now; refinement can follow once GOTO ordering is correct.
            case "SET_FRAME":
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
