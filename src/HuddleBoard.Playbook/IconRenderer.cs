using SkiaSharp;

namespace HuddleBoard.Playbook;

/// <summary>
/// App icons: the bookmark art, scaled down whole, so the installed app wears
/// exactly the picture the website's bookmark does.
/// </summary>
/// <remarks>
/// The art is authored at <c>art/bookmark-icon.png</c> — a rounded tile on a
/// pale square — and the icons are that picture and nothing else; replacing
/// the file and rebuilding is the whole process. The plain icons are the art
/// edge to edge. The maskable one is the art shrunk into the 80% safe zone on
/// its own corner colour, because a launcher crops a maskable icon to its own
/// shape, and cropping the art would cut off the kid's feet.
/// </remarks>
public static class IconRenderer
{
    /// <summary>The icon, as authored.</summary>
    public static string SourcePath => Path.Combine(Workspace.Root, "art", "bookmark-icon.png");

    private static readonly (string Name, int Size, double Pad)[] Icons =
    [
        ("icon-192.png", 192, 0),
        ("icon-512.png", 512, 0),
        ("icon-maskable-512.png", 512, .10),
    ];

    /// <summary>True when an icon is missing or older than the art.</summary>
    public static bool IsStale =>
        Icons.Any(i =>
        {
            var path = Path.Combine(Workspace.Deploy, i.Name);
            return !File.Exists(path)
                || (File.Exists(SourcePath) && File.GetLastWriteTimeUtc(path) < File.GetLastWriteTimeUtc(SourcePath));
        });

    private static SKBitmap Draw(SKBitmap art, int size, double padFraction)
    {
        var info = new SKImageInfo(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(art.GetPixel(0, 0));

        var pad = (float)(size * padFraction);
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawImage(SKImage.FromBitmap(art), new SKRect(0, 0, art.Width, art.Height),
                         new SKRect(pad, pad, size - pad, size - pad),
                         new SKSamplingOptions(SKCubicResampler.Mitchell), paint);

        return SKBitmap.FromImage(surface.Snapshot());
    }

    /// <summary>Writes the three icons into <c>dist/deploy/</c>.</summary>
    public static int Run(TextWriter? output = null)
    {
        var o = output ?? Console.Out;
        if (!File.Exists(SourcePath))
            throw new FileNotFoundException($"the app icon is missing — put a PNG at {SourcePath}", SourcePath);

        using var art = SKBitmap.Decode(SourcePath)
            ?? throw new InvalidOperationException($"could not decode {SourcePath} as an image");
        var deploy = Workspace.Ensure(Workspace.Deploy);

        foreach (var (name, size, pad) in Icons)
        {
            using var bitmap = Draw(art, size, pad);
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
