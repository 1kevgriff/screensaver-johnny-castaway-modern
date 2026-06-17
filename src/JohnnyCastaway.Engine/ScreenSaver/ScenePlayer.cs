using JohnnyCastaway.Ttm;
using SkiaSharp;

namespace JohnnyCastaway.ScreenSaver;

public sealed class ScenePlayer
{
    private readonly IReadOnlyList<RenderedFrame> _frames;
    private readonly long[] _cumMs;   // cumulative end-time per frame
    public long TotalMs { get; }
    public int FrameCount => _frames.Count;

    public ScenePlayer(IReadOnlyList<RenderedFrame> frames, int tickMs = 50)
    {
        if (frames.Count == 0) throw new ArgumentException("no frames", nameof(frames));
        _frames = frames;
        _cumMs = new long[frames.Count];
        long acc = 0;
        for (int i = 0; i < frames.Count; i++)
        {
            acc += Math.Max(1, frames[i].DurationTicks) * (long)tickMs;
            _cumMs[i] = acc;
        }
        TotalMs = acc;
    }

    public int IndexAt(long elapsedMs)
    {
        long t = ((elapsedMs % TotalMs) + TotalMs) % TotalMs;
        for (int i = 0; i < _cumMs.Length; i++)
            if (t < _cumMs[i]) return i;
        return _frames.Count - 1;
    }

    public SKImage ImageAt(long elapsedMs) => _frames[IndexAt(elapsedMs)].Image;

    public IReadOnlyList<int> FrameSounds(int index) => _frames[index].Sounds;

    public void DisposeFrames()
    {
        foreach (var f in _frames) f.Image.Dispose();
    }
}
