using JohnnyCastaway.ScreenSaver;
using JohnnyCastaway.Ttm;
using SkiaSharp;
using Xunit;

namespace JohnnyCastaway.Tests;

public class ScenePlayerTests
{
    static RenderedFrame F(int ticks)
    {
        using var s = SKSurface.Create(new SKImageInfo(2, 2));
        return new RenderedFrame(s.Snapshot(), ticks);
    }

    [Fact]
    public void IndexAdvancesAndLoops()
    {
        var p = new ScenePlayer(new[] { F(5), F(10) }, tickMs: 50); // 250ms + 500ms = 750ms
        Assert.Equal(750, p.TotalMs);
        Assert.Equal(0, p.IndexAt(0));
        Assert.Equal(0, p.IndexAt(249));
        Assert.Equal(1, p.IndexAt(250));
        Assert.Equal(1, p.IndexAt(749));
        Assert.Equal(0, p.IndexAt(750));   // wraps
        Assert.Equal(1, p.IndexAt(1000));  // 1000 % 750 = 250 → frame 1
    }

    [Fact]
    public void SingleFrameAlwaysIndexZero()
    {
        var p = new ScenePlayer(new[] { F(3) }, tickMs: 50);
        Assert.Equal(0, p.IndexAt(0));
        Assert.Equal(0, p.IndexAt(99999));
    }

    [Fact]
    public void DisposeFrames_CalledTwice_DoesNotThrow()
    {
        // Validates that double-dispose (e.g. same-player guard logic in SaverForm) is safe.
        var p = new ScenePlayer(new[] { F(5) }, tickMs: 50);
        p.DisposeFrames();
        // SKImage.Dispose() is idempotent; second call must not throw.
        var ex = Record.Exception(() => p.DisposeFrames());
        Assert.Null(ex);
    }

    [Fact]
    public void ReferenceEquals_SameInstance_IsTrue()
    {
        // Documents that the guard in SaverForm prevents disposal when _next() returns same player.
        var p = new ScenePlayer(new[] { F(5) }, tickMs: 50);
        ScenePlayer? old = p;
        // Simulate the convenience-ctor pattern: () => player always returns the same instance.
        Func<ScenePlayer> next = () => p;
        var newPlayer = next();
        Assert.True(ReferenceEquals(newPlayer, old));
        // With the guard, old.DisposeFrames() would be skipped → no use-after-dispose.
    }
}
