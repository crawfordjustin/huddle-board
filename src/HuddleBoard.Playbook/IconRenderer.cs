using SkiaSharp;

namespace HuddleBoard.Playbook;

/// <summary>
/// App icons: a tablet with the ball on its screen, drawn so it still reads
/// at 48px.
/// </summary>
/// <remarks>
/// Everything is drawn at four times the final size and downsampled, because
/// at 48px a one-pixel edge is gone before it starts. The ball shape and its
/// colours are the same ones the play screen draws (<c>BALL_BODY</c>,
/// <c>BALL_LACES</c> in <c>huddle_src.html</c>) — one picture of the ball,
/// not two.
/// </remarks>
public static class IconRenderer
{
    private static readonly SKColor OutOfBounds = new(30, 37, 33);
    private static readonly SKColor Screen = new(247, 249, 246);
    private static readonly SKColor BallFill = new(0x7A, 0x45, 0x20);
    private static readonly SKColor BallEdge = new(0x14, 0x1A, 0x16);
    private static readonly SKColor BallLace = new(0xF7, 0xF9, 0xF6);

    private static SKBitmap Draw(int size, double padFraction)
    {
        var s = size * 4;
        var info = new SKImageInfo(s, s, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(OutOfBounds);

        var pad = (int)(s * padFraction);
        var fw = s - (pad * 2);

        // the tablet's screen
        var corner = fw * .14f;
        using (var screen = new SKPaint { Color = Screen, IsAntialias = true })
            canvas.DrawRoundRect(new SKRect(pad, pad, s - pad, s - pad), corner, corner, screen);

        // the ball, centred on the screen
        var cx = s / 2f;
        var cy = s / 2f;
        var bw = fw * .32f;
        var bh = bw * (13.5f / 22f);

        using (var fill = new SKPaint { Color = BallFill, IsAntialias = true })
        using (var edge = new SKPaint
               {
                   Color = BallEdge,
                   IsAntialias = true,
                   Style = SKPaintStyle.Stroke,
                   StrokeWidth = Math.Max(3, fw * .014f),
               })
        {
            var bodyBuilder = new SKPathBuilder();
            bodyBuilder.MoveTo(cx - bw, cy);
            bodyBuilder.QuadTo(cx, cy - bh, cx + bw, cy);
            bodyBuilder.QuadTo(cx, cy + bh, cx - bw, cy);
            bodyBuilder.Close();
            using var body = bodyBuilder.Detach();
            canvas.DrawPath(body, fill);
            canvas.DrawPath(body, edge);
        }

        using (var lace = new SKPaint
               {
                   Color = BallLace,
                   IsAntialias = true,
                   Style = SKPaintStyle.Stroke,
                   StrokeWidth = Math.Max(3, fw * .014f),
                   StrokeCap = SKStrokeCap.Round,
               })
        {
            var lacesBuilder = new SKPathBuilder();
            lacesBuilder.MoveTo(cx - bw * (8.5f / 22), cy);
            lacesBuilder.LineTo(cx + bw * (8.5f / 22), cy);
            for (var t = -6f; t <= 6f; t += 6f)
            {
                lacesBuilder.MoveTo(cx + bw * (t / 22), cy - bh * (3.5f / 13.5f));
                lacesBuilder.LineTo(cx + bw * (t / 22), cy + bh * (3.5f / 13.5f));
            }

            using var laces = lacesBuilder.Detach();
            canvas.DrawPath(laces, lace);
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
