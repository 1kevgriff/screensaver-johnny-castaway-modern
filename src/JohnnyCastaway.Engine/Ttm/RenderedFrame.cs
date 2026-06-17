using SkiaSharp;

namespace JohnnyCastaway.Ttm;

public record RenderedFrame(SKImage Image, int DurationTicks, IReadOnlyList<int> Sounds);
