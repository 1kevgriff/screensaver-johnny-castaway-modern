using JohnnyCastaway.Assets;
using JohnnyCastaway.Content;
using JohnnyCastaway.Ttm;
using System.Text.Json;
using SkiaSharp;
using Xunit;

namespace JohnnyCastaway.Tests;

public class RenderRegressionTests
{
    static string Repo => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", ".."));

    [Fact]
    public void OfficeSceneRendersBackgroundAndSprite()
    {
        var bundle = ContentBundle.Load(Path.Combine(Repo, "content"));
        var assets = new FileAssetStore(
            Path.Combine(Repo, "extracted", "png"),
            Path.Combine(Repo, "extracted", "png"), scale: 1);
        var vm = new TtmVm(assets);
        var frames = vm.RenderSequence(bundle.Ttm["SJWORK.TTM"], "3");

        Assert.NotEmpty(frames);
        using var bmp = SKBitmap.FromImage(frames[0].Image);
        Assert.Equal(640, bmp.Width);
        Assert.Equal(350, bmp.Height);
        // The office background fills the centre — a non-black pixel exists there.
        bool anyNonBlack = false;
        for (int x = 200; x < 440 && !anyNonBlack; x += 7)
            for (int y = 150; y < 300; y += 7)
                if (bmp.GetPixel(x, y) != SKColors.Black) { anyNonBlack = true; break; }
        Assert.True(anyNonBlack, "expected the office background to be drawn");
    }

    [Fact]
    public void OfficeSceneAt4xIsNativeTimesFour()
    {
        var nativeBgSizes = LoadNativeBackgroundSizes(Path.Combine(Repo, "content", "manifest.json"));
        var bundle = ContentBundle.Load(Path.Combine(Repo, "content"));
        var assets = new FileAssetStore(
            Path.Combine(Repo, "extracted", "upscaled"),
            Path.Combine(Repo, "extracted", "upscaled"),
            scale: 4,
            nativeBackgroundSizes: nativeBgSizes);
        var vm = new TtmVm(assets);
        var frames = vm.RenderSequence(bundle.Ttm["SJWORK.TTM"], "3");

        Assert.NotEmpty(frames);
        using var bmp = SKBitmap.FromImage(frames[0].Image);

        // Native office scene is 640x350; at 4x it must be exactly 2560x1400.
        Assert.Equal(640 * 4, bmp.Width);
        Assert.Equal(350 * 4, bmp.Height);

        // The office background should fill the centre — scan for a non-black pixel.
        bool anyNonBlack = false;
        for (int x = 200 * 4; x < 440 * 4 && !anyNonBlack; x += 28)
            for (int y = 150 * 4; y < 300 * 4; y += 28)
                if (bmp.GetPixel(x, y) != SKColors.Black) { anyNonBlack = true; break; }
        Assert.True(anyNonBlack, "expected the 4x office background to be drawn");
    }

    static IReadOnlyDictionary<string, (int W, int H)> LoadNativeBackgroundSizes(string manifestPath)
    {
        var result = new Dictionary<string, (int W, int H)>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(manifestPath)) return result;

        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        if (!doc.RootElement.TryGetProperty("backgrounds", out var bgs)) return result;

        foreach (var entry in bgs.EnumerateObject())
        {
            if (entry.Value.TryGetProperty("native", out var native))
            {
                var arr = native.EnumerateArray().ToArray();
                if (arr.Length == 2)
                    result[entry.Name] = (arr[0].GetInt32(), arr[1].GetInt32());
            }
        }
        return result;
    }
}
