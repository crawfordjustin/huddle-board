using SkiaSharp;

namespace HuddleBoard.Playbook;

/// <summary>
/// App icons: the field mark, drawn so it still reads at 48px.
/// </summary>
/// <remarks>
/// Everything is drawn at four times the final size and downsampled, because at
/// 48px the sidelines are barely a pixel wide and aliasing eats them.
/// </remarks>
public static class IconRenderer
{
    private static readonly SKColor OutOfBounds = new(30, 37, 33);
    private static readonly SKColor Field = new(247, 249, 246);
    private static readonly SKColor Blue = new(29, 111, 208);
    private static readonly SKColor Orange = new(228, 97, 15);
    private static readonly SKColor Green = new(18, 122, 77);
    private static readonly SKColor BallYellow = new(255, 196, 0);
    private static readonly SKColor Scrimmage = new(174, 184, 176);

    private static void Rect(SKCanvas c, float x0, float y0, float x1, float y1, SKColor colour)
    {
        using var paint = new SKPaint { Color = colour, IsAntialias = true };
        c.DrawRect(new SKRect(x0, y0, x1, y1), paint);
    }

    private static SKBitmap Draw(int size, double padFraction)
    {
        var s = size * 4;
        var info = new SKImageInfo(s, s, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(OutOfBounds);

        var pad = (int)(s * padFraction);
        var fw = s - (pad * 2);

        // playing surface
        float fx0 = pad + (int)(fw * .17), fx1 = s - pad - (int)(fw * .17);
        float fy0 = pad, fy1 = s - pad;
        Rect(canvas, fx0, fy0, fx1, fy1, Field);

        // the two sidelines
        var bw = Math.Max(3, (int)(fw * .055));
        Rect(canvas, fx0, fy0, fx0 + bw, fy1, Blue);
        Rect(canvas, fx1 - bw, fy0, fx1, fy1, Orange);

        // line of scrimmage
        var los = fy0 + (int)((fy1 - fy0) * .62);
        Rect(canvas, fx0 + bw, los, fx1 - bw, los + Math.Max(2, (int)(s * .012)), Scrimmage);

        // one route, breaking upfield
        var cx = (fx0 + fx1) / 2;
        var lw = Math.Max(4, (int)(fw * .085));
        using (var route = new SKPaint
               {
                   Color = Green,
                   IsAntialias = true,
                   Style = SKPaintStyle.Stroke,
                   StrokeWidth = lw,
                   StrokeJoin = SKStrokeJoin.Round,
                   StrokeCap = SKStrokeCap.Round,
               })
        {
            var builder = new SKPathBuilder();
            builder.MoveTo(cx - (int)(fw * .10), fy1 - (int)((fy1 - fy0) * .16));
            builder.LineTo(cx - (int)(fw * .02), los);
            builder.LineTo(cx + (int)(fw * .09), fy0 + (int)((fy1 - fy0) * .14));
            using var path = builder.Detach();
            canvas.DrawPath(path, route);
        }

        // the ball
        var r = (int)(fw * .085);
        float bx = cx - (int)(fw * .10), by = fy1 - (int)((fy1 - fy0) * .16);
        using (var fill = new SKPaint { Color = BallYellow, IsAntialias = true })
            canvas.DrawCircle(bx, by, r, fill);
        using (var edge = new SKPaint
               {
                   Color = OutOfBounds,
                   IsAntialias = true,
                   Style = SKPaintStyle.Stroke,
                   StrokeWidth = Math.Max(2, (int)(s * .008)),
               })
        {
            canvas.DrawCircle(bx, by, r, edge);
        }

        using var full = SKBitmap.FromImage(surface.Snapshot());
        return full.Resize(new SKImageInfo(size, size), new SKSamplingOptions(SKCubicResampler.Mitchell))
               ?? throw new InvalidOperationException("icon downsample failed");
    }

    /// <summary>Writes the three icons into <c>dist/deploy/</c>.</summary>
    public static int Run(TextWriter? output = null)
    {
        var o = output ?? Console.Out;
        var deploy = Workspace.Ensure(Workspace.Deploy);

        foreach (var (name, size, pad) in new[]
                 {
                     ("icon-192.png", 192, .09),
                     ("icon-512.png", 512, .09),
                     ("icon-maskable-512.png", 512, .20),
                 })
        {
            using var bitmap = Draw(size, pad);
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
