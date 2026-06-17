using SkiaSharp;

namespace JohnnyCastaway.Assets;

public interface IAssetStore
{
    int Scale { get; }
    SKImage? Background(string scrName);            // scrName like "JOFFICE.SCR"
    IReadOnlyList<SKImage> Sprite(string bmpName);  // bmpName like "SJWORK.BMP"
}

public sealed class FileAssetStore(
    string spritesRoot,
    string backgroundsRoot,
    int scale,
    IReadOnlyDictionary<string, (int W, int H)>? nativeBackgroundSizes = null) : IAssetStore
{
    private readonly Dictionary<string, SKImage?> _bg = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<SKImage>> _spr = new(StringComparer.OrdinalIgnoreCase);

    public int Scale => scale;

    public SKImage? Background(string scrName)
    {
        if (_bg.TryGetValue(scrName, out var img)) return img;
        var path = Path.Combine(backgroundsRoot, scrName + ".png");
        img = File.Exists(path) ? SKImage.FromEncodedData(path) : null;

        if (img is not null && nativeBackgroundSizes is not null &&
            nativeBackgroundSizes.TryGetValue(scrName, out var native))
        {
            int targetW = native.W * scale;
            int targetH = native.H * scale;
            if (img.Width != targetW || img.Height != targetH)
            {
                var info = new SKImageInfo(targetW, targetH);
                using var surface = SKSurface.Create(info);
                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    FilterQuality = SKFilterQuality.High
                };
                surface.Canvas.DrawImage(img, new SKRect(0, 0, targetW, targetH), paint);
                img.Dispose();
                img = surface.Snapshot();
            }
        }

        _bg[scrName] = img;
        return img;
    }

    public IReadOnlyList<SKImage> Sprite(string bmpName)
    {
        if (_spr.TryGetValue(bmpName, out var frames)) return frames;
        var dir = Path.Combine(spritesRoot, bmpName);
        var list = new List<SKImage>();
        if (Directory.Exists(dir))
            foreach (var f in Directory.GetFiles(dir, "f*.png").OrderBy(x => x))
            {
                var img = SKImage.FromEncodedData(f);
                if (img is not null) list.Add(img);
            }
        _spr[bmpName] = list;
        return list;
    }
}
