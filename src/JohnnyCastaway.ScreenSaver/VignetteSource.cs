using JohnnyCastaway.Ads;
using JohnnyCastaway.Assets;
using JohnnyCastaway.Content;
using JohnnyCastaway.Schedule;
using JohnnyCastaway.Ttm;
using System.Text.Json;

namespace JohnnyCastaway.ScreenSaver;

public sealed class TtmClipRenderer(ContentBundle bundle, IAssetStore assets) : IClipRenderer
{
    public IReadOnlyList<RenderedFrame> Render(string ttmFile, int seqNum)
        => new TtmVm(assets).RenderSequence(bundle.Ttm[ttmFile.ToUpperInvariant()], seqNum.ToString());
}

public static class VignetteSource
{
    /// <summary>Render a TTM sequence to a looping ScenePlayer using the up-res'd assets.</summary>
    public static ScenePlayer Load(string repoRoot, string ttm, string seq, int scale)
    {
        string contentDir = Path.Combine(repoRoot, "content");
        var bundle = ContentBundle.Load(contentDir);
        var native = LoadNativeBackgroundSizes(Path.Combine(contentDir, "manifest.json"));
        string assetsRoot = scale == 4
            ? Path.Combine(repoRoot, "extracted", "upscaled")
            : Path.Combine(repoRoot, "extracted", "png");
        var store = new FileAssetStore(assetsRoot, assetsRoot, scale, native);
        var vm = new TtmVm(store);
        var frames = vm.RenderSequence(bundle.Ttm[ttm.ToUpperInvariant()], seq);
        return new ScenePlayer(frames);
    }

    /// <summary>Plan and render an ADS segment into a looping ScenePlayer.</summary>
    public static ScenePlayer LoadAds(string repoRoot, string adsName, int segmentIndex, int scale, int seed)
    {
        string contentDir = Path.Combine(repoRoot, "content");
        var bundle = ContentBundle.Load(contentDir);
        var native = LoadNativeBackgroundSizes(Path.Combine(contentDir, "manifest.json"));
        string assetsRoot = scale == 4
            ? Path.Combine(repoRoot, "extracted", "upscaled")
            : Path.Combine(repoRoot, "extracted", "png");
        var store = new FileAssetStore(assetsRoot, assetsRoot, scale, native);
        return BuildAdsPlayer(bundle, store, adsName, segmentIndex, seed);
    }

    /// <summary>
    /// Returns a thunk that each call picks a time-appropriate vignette via DayClock+Scheduler
    /// and renders it into a fresh ScenePlayer. ContentBundle and assets are loaded once.
    /// </summary>
    public static Func<ScenePlayer> CreateScheduledProvider(string repoRoot, int scale, int startOfDayHHMM, int seed)
    {
        string contentDir = Path.Combine(repoRoot, "content");
        var bundle = ContentBundle.Load(contentDir);
        var native = LoadNativeBackgroundSizes(Path.Combine(contentDir, "manifest.json"));
        string assetsRoot = scale == 4
            ? Path.Combine(repoRoot, "extracted", "upscaled")
            : Path.Combine(repoRoot, "extracted", "png");
        var store = new FileAssetStore(assetsRoot, assetsRoot, scale, native);
        var counts = bundle.Ads.ToDictionary(kv => kv.Key, kv => kv.Value.Segments.Count);
        var clock = new DayClock(TimeProvider.System, startOfDayHHMM);
        var scheduler = new Scheduler(counts, new Random(seed));
        int n = seed;
        return () =>
        {
            var v = scheduler.Pick(clock.Now());
            return BuildAdsPlayer(bundle, store, v.AdsName, v.SegmentIndex, unchecked(++n));
        };
    }

    /// <summary>
    /// Builds a ScenePlayer for the given ADS segment using pre-loaded bundle and asset store.
    /// </summary>
    private static ScenePlayer BuildAdsPlayer(
        ContentBundle bundle, FileAssetStore store,
        string adsName, int segmentIndex, int seed)
    {
        var renderer = new TtmClipRenderer(bundle, store);
        var director = new AdsDirector(bundle.Ads[adsName.ToUpperInvariant()], renderer, new Random(seed));
        return AdsVignettePlayer.Build(director, renderer, segmentIndex);
    }

    private static Dictionary<string, (int W, int H)> LoadNativeBackgroundSizes(string manifestPath)
    {
        var result = new Dictionary<string, (int, int)>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(manifestPath)) return result;
        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        if (doc.RootElement.TryGetProperty("backgrounds", out var bgs))
            foreach (var b in bgs.EnumerateObject())
            {
                var n = b.Value.GetProperty("native");
                result[b.Name] = (n[0].GetInt32(), n[1].GetInt32());
            }
        return result;
    }
}
