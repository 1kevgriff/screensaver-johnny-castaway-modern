using JohnnyCastaway.Ads;
using JohnnyCastaway.Ttm;

namespace JohnnyCastaway.ScreenSaver;

public static class AdsVignettePlayer
{
    /// <summary>Plan an ADS segment and concatenate every planned clip's frames into one looping ScenePlayer.</summary>
    public static ScenePlayer Build(AdsDirector director, IClipRenderer renderer, int segmentIndex, int tickMs = 50)
    {
        var plan = director.PlanSegment(segmentIndex);
        var frames = new List<RenderedFrame>();
        foreach (var clip in plan)
            frames.AddRange(renderer.Render(clip.TtmFile, clip.SeqNum));
        if (frames.Count == 0)
            throw new InvalidOperationException($"ADS segment {segmentIndex} planned no clips");
        return new ScenePlayer(frames, tickMs);
    }
}
