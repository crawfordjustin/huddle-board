using SkiaSharp;

namespace HuddleBoard.Playbook;

/// <summary>
/// App icons: the bookmark art, scaled down whole, so the installed app wears
/// exactly the picture the website's bookmark does.
/// </summary>
/// <remarks>
/// The art is authored at <c>art/bookmark-icon.png</c> — a rounded tile with
/// transparent corners — and the icons are that picture and nothing else;
/// replacing the file and rebuilding is the whole process. The plain icons are
/// the art edge to edge, corners and all. Two are solid instead, on the tile's
/// own colour, because the platform shapes them and a transparent pixel there
/// is a hole: the maskable one is the art shrunk into the 80% safe zone, since
/// a launcher crops it to its own shape and cropping the art would cut off the
/// kid's feet; and the iOS one is the art full size, since iOS rounds the
/// corners itself and fills anything transparent with black.
/// </remarks>
public static class IconRenderer
{
    /// <summary>The icon, as authored.</summary>
    public static string SourcePath => Path.Combine(Workspace.Root, "art", "bookmark-icon.png");

    private static readonly (string Name, int Size, double Pad, bool Solid)[] Icons =
    [
        ("icon-192.png", 192, 0, false),
        ("icon-512.png", 512, 0, false),
        ("icon-maskable-512.png", 512, .10, true),
        ("icon-apple-180.png", 180, 0, true),
    ];

    /// <summary>
    /// The tile's colour: the middle of the left edge, a few pixels in, where
    /// the rounded corners and the antialiased rim are both out of the way.
    /// </summary>
    private static SKColor TileColour(SKBitmap art) =>
        art.GetPixel(Math.Min(4, art.Width - 1), art.Height / 2).WithAlpha(255);

    /// <summary>True when an icon is missing or older than the art.</summary>
    public static bool IsStale =>
        Icons.Any(i =>
        {
            var path = Path.Combine(Workspace.Deploy, i.Name);
            return !File.Exists(path)
                || (File.Exists(SourcePath) && File.GetLastWriteTimeUtc(path) < File.GetLastWriteTimeUtc(SourcePath));
        });

    private static SKBitmap Draw(SKBitmap art, int size, double padFraction, bool solid)
    {
        var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(solid ? TileColour(art) : SKColors.Transparent);

        var pad = (float)(size * padFraction);
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawImage(SKImage.FromBitmap(art), new SKRect(0, 0, art.Width, art.Height),
                         new SKRect(pad, pad, size - pad, size - pad),
                         new SKSamplingOptions(SKCubicResampler.Mitchell), paint);

        return SKBitmap.FromImage(surface.Snapshot());
    }

    /// <summary>Writes the icons into <c>dist/deploy/</c>.</summary>
    public static int Run(TextWriter? output = null)
    {
        var o = output ?? Console.Out;
        if (!File.Exists(SourcePath))
            throw new FileNotFoundException($"the app icon is missing — put a PNG at {SourcePath}", SourcePath);

        using var art = SKBitmap.Decode(SourcePath)
            ?? throw new InvalidOperationException($"could not decode {SourcePath} as an image");
        var deploy = Workspace.Ensure(Workspace.Deploy);

        foreach (var (name, size, pad, solid) in Icons)
        {
            using var bitmap = Draw(art, size, pad, solid);
            using var image = SKImage.FromBitmap(bitmap);
            using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            var path = Path.Combine(deploy, name);
            using (var file = File.Create(path))
                png.SaveTo(file);
            o.WriteLine("wrote {0}", path);
        }

        return 0;
    }
}
