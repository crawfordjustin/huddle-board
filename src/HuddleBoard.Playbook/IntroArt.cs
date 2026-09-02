using SkiaSharp;

namespace HuddleBoard.Playbook;

/// <summary>
/// The intro illustration, turned into a data URI so it can live inside the one
/// HTML file the app ships as.
/// </summary>
/// <remarks>
/// The app is one file that opens on a tablet with the radio off, so the art
/// cannot be a second request — it is base64 in the markup or it is nothing.
/// That makes its weight the whole problem. The source PNG is a couple of
/// megabytes, which base64 would make nearly three; that is twenty times the
/// rest of the app put together and it would be paid on every cold open and
/// every service-worker update.
///
/// So the build downsamples it to the largest size any tablet actually paints
/// it at and re-encodes it. The format is chosen by measuring, not by taste:
/// each candidate is encoded and the smallest that is still a faithful picture
/// wins. This is a photographic-looking illustration with no transparency,
/// which is exactly the case PNG is worst at and WebP is best at.
/// </remarks>
public static class IntroArt
{
    /// <summary>The illustration, as authored. Replacing this file is the whole
    /// process for changing the intro art.</summary>
    public static string SourcePath => Path.Combine(Workspace.Root, "art", "intro-art.png");

    /// <summary>
    /// Widest the panel is ever painted: the art column is about 58% of a
    /// 1600px-wide tablet, and doubling that covers a 2x display without paying
    /// for resolution nobody can see.
    /// </summary>
    private const int MaxWidth = 1600;

    private static readonly (SKEncodedImageFormat Format, int Quality, string Mime)[] Candidates =
    [
        (SKEncodedImageFormat.Webp, 82, "image/webp"),
        (SKEncodedImageFormat.Jpeg, 84, "image/jpeg"),
        (SKEncodedImageFormat.Png, 100, "image/png"),
    ];

    /// <summary>What the encoder settled on, for the build to report.</summary>
    public sealed record Encoded(string DataUri, string Mime, int Width, int Height, int Bytes);

    /// <summary>Reads, downsamples and encodes the illustration.</summary>
    public static Encoded Build()
    {
        if (!File.Exists(SourcePath))
            throw new FileNotFoundException(
                $"the intro illustration is missing — put a PNG at {SourcePath}", SourcePath);

        using var source = SKBitmap.Decode(SourcePath)
            ?? throw new InvalidOperationException($"could not decode {SourcePath} as an image");

        using var scaled = Fit(source, MaxWidth);

        (SKData Data, string Mime)? best = null;
        foreach (var (format, quality, mime) in Candidates)
        {
            var data = scaled.Encode(format, quality);
            if (data is null)
                continue;                      // this build of Skia cannot write that format
            if (best is null || data.Size < best.Value.Data.Size)
            {
                best?.Data.Dispose();
                best = (data, mime);
            }
            else
            {
                data.Dispose();
            }
        }

        if (best is null)
            throw new InvalidOperationException("no image encoder was available");

        var (bytes, chosen) = (best.Value.Data.ToArray(), best.Value.Mime);
        best.Value.Data.Dispose();

        return new Encoded(
            $"data:{chosen};base64,{Convert.ToBase64String(bytes)}",
            chosen, scaled.Width, scaled.Height, bytes.Length);
    }

    /// <summary>
    /// Downsamples to a maximum width, and never upsamples — enlarging art the
    /// coach supplied would only make it softer and heavier at once.
    /// </summary>
    private static SKBitmap Fit(SKBitmap source, int maxWidth)
    {
        if (source.Width <= maxWidth)
            return source.Copy();

        var height = (int)Math.Round(source.Height * (double)maxWidth / source.Width);
        var target = new SKBitmap(new SKImageInfo(maxWidth, height, SKColorType.Rgba8888,
                                                  SKAlphaType.Premul));
        using var canvas = new SKCanvas(target);
        using var paint = new SKPaint { IsAntialias = true };
        canvas.DrawBitmap(source, new SKRect(0, 0, source.Width, source.Height),
                          new SKRect(0, 0, maxWidth, height),
                          new SKSamplingOptions(SKCubicResampler.Mitchell), paint);
        return target;
    }
}
