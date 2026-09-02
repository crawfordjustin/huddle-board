using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuddleBoard.Tests;

/// <summary>
/// The intro screen: the illustration, one green button, and a way through to
/// the deck on every tablet shape.
/// </summary>
/// <remarks>
/// This is the screen a coach lands on cold, so the failure that matters is not
/// an ugly one — it is START sitting below the fold on the short landscape
/// tablet, which strands him on the first screen with a game about to start.
/// Most of the geometry here is about that.
///
/// The art is now one inlined image rather than figures drawn from pose data,
/// so the other thing worth holding is that it actually arrived. A data URI the
/// build got wrong fails silently: the layout is fine, the button works, and the
/// panel is simply empty. <c>naturalWidth</c> is the only thing that tells you.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class IntroChecks(AppFixture app)
{
    private sealed record Layout(
        [property: JsonPropertyName("loaded")] bool Loaded,
        [property: JsonPropertyName("natW")] int NaturalWidth,
        [property: JsonPropertyName("natH")] int NaturalHeight,
        [property: JsonPropertyName("shownW")] double ShownWidth,
        [property: JsonPropertyName("shownH")] double ShownHeight,
        [property: JsonPropertyName("btnTop")] double ButtonTop,
        [property: JsonPropertyName("btnBottom")] double ButtonBottom,
        [property: JsonPropertyName("btnLeft")] double ButtonLeft,
        [property: JsonPropertyName("btnRight")] double ButtonRight,
        [property: JsonPropertyName("btnH")] double ButtonHeight,
        [property: JsonPropertyName("artH")] double ArtHeight,
        [property: JsonPropertyName("overflow")] bool Overflow);

    private const string Measure = """
        () => {
          const b = document.getElementById('start').getBoundingClientRect();
          const art = document.querySelector('.introart').getBoundingClientRect();
          const img = document.querySelector('.introimg');
          const r = img.getBoundingClientRect();
          // stringified, then deserialized on the C# side: Playwright's own
          // object converter needs a parameterless constructor, which a record
          // with positional parameters does not have
          return JSON.stringify(
                 {loaded: img.complete && img.naturalWidth > 0,
                  natW: img.naturalWidth, natH: img.naturalHeight,
                  shownW: +r.width.toFixed(1), shownH: +r.height.toFixed(1),
                  btnTop: +b.top.toFixed(1), btnBottom: +b.bottom.toFixed(1),
                  btnLeft: +b.left.toFixed(1), btnRight: +b.right.toFixed(1),
                  btnH: +b.height.toFixed(1), artH: +art.height.toFixed(1),
                  overflow: document.documentElement.scrollHeight > innerHeight + 1
                         || document.documentElement.scrollWidth > innerWidth + 1});
        }
        """;

    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task StartIsReachableAndTheArtIsThereOnEveryTablet(string label, int width, int height)
    {
        var size = new Viewport(label, width, height);
        var (page, errors) = await app.OpenAppAsync(size, settle: 400, intro: true);
        await page.WaitForFunctionAsync(
            "() => { const i = document.querySelector('.introimg'); return i && i.complete; }");

        var g = JsonSerializer.Deserialize<Layout>(
            await page.EvaluateAsync<string>(Measure))!;

        // the picture actually arrived — the one failure that hides itself
        Assert.True(g.Loaded, "the intro illustration did not decode; the data URI is wrong");
        Assert.True(g.NaturalWidth > 400 && g.NaturalHeight > 200,
            $"the illustration decoded at {g.NaturalWidth}x{g.NaturalHeight}, too small to be it");

        // the button is wholly on screen, and big enough to hit with gloves on
        Assert.True(g.ButtonTop >= 0 && g.ButtonBottom <= height,
            $"START runs from {g.ButtonTop} to {g.ButtonBottom} on a {height}px tall screen");
        Assert.True(g.ButtonLeft >= 0 && g.ButtonRight <= width,
            $"START runs from {g.ButtonLeft} to {g.ButtonRight} on a {width}px wide screen");
        Assert.True(g.ButtonHeight >= 48, $"START is only {g.ButtonHeight}px tall");
        Assert.False(g.Overflow, "the intro scrolled instead of fitting");

        // and the art did not collapse to nothing to make that happen
        Assert.True(g.ArtHeight > height * 0.25,
            $"the art is only {g.ArtHeight}px tall on a {height}px screen");
        Assert.True(g.ShownWidth > 100 && g.ShownHeight > 60,
            $"the illustration is drawn at {g.ShownWidth}x{g.ShownHeight}");

        // ...and it is not squashed. Sizing an image in both directions at once
        // is the easy way to stretch it, and a stretched cartoon reads as broken.
        var shown = g.ShownWidth / g.ShownHeight;
        var natural = (double)g.NaturalWidth / g.NaturalHeight;
        Assert.True(Math.Abs(shown - natural) < 0.02,
            $"the illustration is drawn at {shown:F3} against a natural {natural:F3} — it is being stretched");

        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        Assert.True(await page.Locator(".tile").CountAsync() > 0, "START did not reach the deck");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// The art ships inside the one HTML file, so its weight is the app's
    /// weight. The build downsamples and re-encodes to keep that true; this is
    /// the line that says how much is too much.
    /// </summary>
    [Fact]
    public void TheInlinedArtStaysSmallEnoughToShip()
    {
        var art = HuddleBoard.Playbook.IntroArt.Build();

        Assert.True(art.Bytes < 400 * 1024,
            $"the intro art is {art.Bytes / 1024}KB encoded — every cold open and every "
            + "service-worker update pays that");
        Assert.True(art.Width <= 1600, $"the art is inlined at {art.Width}px wide");
        Assert.StartsWith("data:image/", art.DataUri, StringComparison.Ordinal);
    }
}
