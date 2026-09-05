using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuddleBoard.Tests;

/// <summary>
/// The tutorial: four slides between the first START and the deck, once, and
/// in the menu after that.
/// </summary>
/// <remarks>
/// Two things can go wrong here and both strand a coach. The slides are the
/// one screen with no other way off it, so NEXT below the fold on the short
/// landscape tablet is a dead end on the first open — the same failure
/// <see cref="IntroChecks"/> guards against, one screen later. And "once"
/// has to mean once: a tutorial that comes back on every open is a tax on
/// every game, so the flag has to reach storage from Skip as well as from
/// the last slide, and only Start over may clear it.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class TutorialChecks(AppFixture app)
{
    private sealed record Layout(
        [property: JsonPropertyName("slide")] int Slide,
        [property: JsonPropertyName("nextLabel")] string NextLabel,
        [property: JsonPropertyName("nextTop")] double NextTop,
        [property: JsonPropertyName("nextBottom")] double NextBottom,
        [property: JsonPropertyName("nextH")] double NextHeight,
        [property: JsonPropertyName("picW")] double PictureWidth,
        [property: JsonPropertyName("picH")] double PictureHeight,
        [property: JsonPropertyName("picInside")] bool PictureInside,
        [property: JsonPropertyName("textOver")] double TextOverflow,
        [property: JsonPropertyName("overflow")] bool Overflow,
        [property: JsonPropertyName("h")] double Height);

    private const string Measure = """
        () => {
          const t = document.querySelector('.tour');
          const next = document.getElementById('tournext');
          const n = next.getBoundingClientRect();
          const art = document.querySelector('.tourart').getBoundingClientRect();
          const pic = document.querySelector('.tourart > *').getBoundingClientRect();
          const say = document.querySelector('.toursay');
          return JSON.stringify({
            slide: +t.dataset.slide,
            nextLabel: next.textContent.trim(),
            nextTop: +n.top.toFixed(1), nextBottom: +n.bottom.toFixed(1), nextH: +n.height.toFixed(1),
            picW: +pic.width.toFixed(1), picH: +pic.height.toFixed(1),
            picInside: pic.left >= art.left - 1 && pic.right <= art.right + 1
                    && pic.top >= art.top - 1 && pic.bottom <= art.bottom + 1,
            textOver: say.scrollHeight - say.clientHeight,
            overflow: document.documentElement.scrollHeight > innerHeight + 1
                   || document.documentElement.scrollWidth > innerWidth + 1,
            h: innerHeight});
        }
        """;

    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task TheFirstStartWalksEverySlideAndLandsOnTheDeck(string label, int w, int h)
    {
        var size = new Viewport(label, w, h);
        var (page, errors) = await app.OpenAppAsync(size, fresh: true);

        Assert.Equal(0, await page.Locator(".deck").CountAsync());
        var slides = 0;
        while (true)
        {
            var g = JsonSerializer.Deserialize<Layout>(await page.EvaluateAsync<string>(Measure))!;
            slides++;
            Assert.Equal(slides, g.Slide);

            // the way off the screen is on the screen, and big enough for a thumb
            Assert.True(g.NextTop >= 0 && g.NextBottom <= g.Height,
                $"{size} slide {g.Slide}: NEXT sits at {g.NextTop}-{g.NextBottom} on a {g.Height}px screen");
            Assert.True(g.NextHeight >= 44, $"{size} slide {g.Slide}: NEXT is only {g.NextHeight}px tall");
            Assert.False(g.Overflow, $"{size} slide {g.Slide}: the page scrolls");

            // the picture is there, and inside its box rather than spilling over the words
            Assert.True(g.PictureWidth > 120 && g.PictureHeight > 60,
                $"{size} slide {g.Slide}: the picture is drawn at {g.PictureWidth}x{g.PictureHeight}");
            Assert.True(g.PictureInside, $"{size} slide {g.Slide}: the picture runs out of its box");
            Assert.True(g.TextOverflow <= 2,
                $"{size} slide {g.Slide}: the words need {g.TextOverflow}px more than they have");

            if (g.NextLabel.Equals("Go to deck", StringComparison.OrdinalIgnoreCase))
                break;
            Assert.Equal("Next", g.NextLabel, ignoreCase: true);
            await page.ClickAsync("#tournext");
            await page.WaitForTimeoutAsync(150);
        }
        Assert.InRange(slides, 3, 4);

        await page.ClickAsync("#tournext");
        await page.WaitForSelectorAsync(".deck");
        Assert.True(await page.Locator(".tile").CountAsync() > 0, "the last slide did not reach the deck");
        Assert.Equal("1", await page.EvaluateAsync<string>("localStorage.getItem('hb.tour')"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public async Task ItShowsOnceAndComesBackFromTheMenu()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], fresh: true);

        // Skip is the coach saying no, and that counts as having seen it
        Assert.Equal("Skip tutorial", (await page.InnerTextAsync("#tourskip")).Trim(), ignoreCase: true);
        await page.ClickAsync("#tourskip");
        await page.WaitForSelectorAsync(".deck");

        // the second START on this tablet goes straight to the deck, from storage and not memory
        await page.ReloadAsync();
        await page.WaitForTimeoutAsync(500);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        Assert.Equal(0, await page.Locator(".tour").CountAsync());

        // it is still one tap away, and from there the buttons say so
        await page.ClickAsync("#ham");
        await page.ClickAsync("#tour");
        await page.WaitForSelectorAsync(".tour");
        Assert.Equal("Close", (await page.InnerTextAsync("#tourskip")).Trim(), ignoreCase: true);
        Assert.True(await page.Locator("#tourback").IsDisabledAsync(), "Back is live on the first slide");
        await page.ClickAsync("#tournext");
        await page.WaitForTimeoutAsync(150);
        Assert.False(await page.Locator("#tourback").IsDisabledAsync(), "Back is dead on the second slide");
        await page.ClickAsync("#tourback");
        await page.WaitForTimeoutAsync(150);
        Assert.Equal("1", await page.GetAttributeAsync(".tour", "data-slide"));
        await page.ClickAsync("#tourskip");
        await page.WaitForSelectorAsync(".deck");

        // Start over is the one thing that makes this somebody's first time again
        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await page.WaitForSelectorAsync("#allreset");
        await page.ClickAsync("#allreset");
        await page.WaitForTimeoutAsync(200);
        await page.ClickAsync("#allreset");
        await page.WaitForTimeoutAsync(400);
        Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('hb.tour')"));
        await page.ClickAsync("#sdone");
        await page.ClickAsync("#ham");
        await page.ClickAsync("#exit");
        await page.WaitForSelectorAsync("#start");
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".tour");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
