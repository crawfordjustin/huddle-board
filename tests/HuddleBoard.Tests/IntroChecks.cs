using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuddleBoard.Tests;

/// <summary>
/// The intro screen: art, one green button, and a way through to the deck on
/// every tablet shape.
/// </summary>
/// <remarks>
/// This is the screen a coach lands on cold, so the failure that matters is not
/// an ugly one — it is START sitting below the fold on the short landscape
/// tablet, which strands him on the first screen with a game about to start.
/// The geometry assertions are all about that.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class IntroChecks(AppFixture app)
{
    private sealed record Layout(
        [property: JsonPropertyName("kids")] int Kids,
        [property: JsonPropertyName("blue")] int Blue,
        [property: JsonPropertyName("orange")] int Orange,
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
          const kids = [...document.querySelectorAll('.introart .introkid')];
          const side = s => kids.filter(k => k.dataset.side === s).length;
          // stringified, then deserialized on the C# side: Playwright's own
          // object converter needs a parameterless constructor, which a record
          // with positional parameters does not have
          return JSON.stringify(
                 {kids: kids.length, blue: side('blue'), orange: side('orange'),
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

        var g = JsonSerializer.Deserialize<Layout>(
            await page.EvaluateAsync<string>(Measure))!;

        // six on the field, three a side — the same six the app is about
        Assert.Equal(6, g.Kids);
        Assert.Equal(3, g.Blue);
        Assert.Equal(3, g.Orange);

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

        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        Assert.True(await page.Locator(".tile").CountAsync() > 0, "START did not reach the deck");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Rule 3 reaches the artwork too: a kid knows he is WIDE BLUE, and there is
    /// no letter on the field to tell him otherwise.
    /// </summary>
    [Fact]
    public async Task ThereAreNoLettersInTheArt()
    {
        var (page, _) = await app.OpenAppAsync(AppFixture.Sizes[0], intro: true);

        Assert.Equal(0, await page.Locator(".introart svg text").CountAsync());
        Assert.Equal(0, await page.Locator(".introart svg tspan").CountAsync());

        await page.CloseAsync();
    }
}
