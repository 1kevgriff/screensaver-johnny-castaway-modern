using JohnnyCastaway.Assets;
using JohnnyCastaway.Content;
using JohnnyCastaway.Ttm;
using System.Text.Json;
using SkiaSharp;

if (args is not ["--render", var ttm, var seq, var outPath, ..])
{
    Console.Error.WriteLine("usage: --render <TTM> <seq> <out.png> [--scale 1|4]");
    return 1;
}

int scale = 1;
int scaleIdx = Array.IndexOf(args, "--scale");
if (scaleIdx >= 0)
{
    if (scaleIdx + 1 < args.Length && int.TryParse(args[scaleIdx + 1], out int parsed) && (parsed == 1 || parsed == 4))
        scale = parsed;
    // else: invalid or missing value — default to 1
}

string repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
string assetsRoot = scale == 4 ? Path.Combine(repo, "extracted", "upscaled")
                               : Path.Combine(repo, "extracted", "png");

// Load native background sizes from manifest so backgrounds can be normalized to native*scale.
var nativeBgSizes = LoadNativeBackgroundSizes(Path.Combine(repo, "content", "manifest.json"));

var bundle = ContentBundle.Load(Path.Combine(repo, "content"));
if (!bundle.Ttm.TryGetValue(ttm.ToUpperInvariant(), out var script)) { Console.Error.WriteLine("no such TTM"); return 2; }
var vm = new TtmVm(new FileAssetStore(assetsRoot, assetsRoot, scale, nativeBgSizes));
var frames = vm.RenderSequence(script, seq);
if (frames.Count == 0) { Console.Error.WriteLine("empty/unknown sequence"); return 2; }

using var data = frames[0].Image.Encode(SKEncodedImageFormat.Png, 100)
    ?? throw new InvalidOperationException("SKImage.Encode returned null");
using var fs = File.Create(outPath);
data.SaveTo(fs);
Console.WriteLine($"wrote {outPath} ({frames[0].Image.Width}x{frames[0].Image.Height}, {frames.Count} frames)");
return 0;

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
