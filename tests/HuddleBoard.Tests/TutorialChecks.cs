using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuddleBoard.Tests;

/// <summary>
/// The tutorial: six slides between the first START and the deck, once, and
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

        // the question comes first, every answer on screen and big enough for a thumb
        Assert.Equal(0, await page.Locator(".deck").CountAsync());
        var answers = await page.EvalOnSelectorAllAsync<string[]>("#askfmt [data-players]", """
            els => els.filter(e => { const r = e.getBoundingClientRect();
                return r.top >= 0 && r.bottom <= innerHeight && r.left >= 0 && r.right <= innerWidth
                    && r.height >= 44; }).map(e => e.dataset.players)
            """);
        Assert.Equal(["5", "6", "7"], answers);
        Assert.Equal("6", await page.GetAttributeAsync("#askfmt .askbtn.on", "data-players"));
        Assert.False(await page.EvaluateAsync<bool>(
            "document.documentElement.scrollHeight > innerHeight + 1"), $"{size}: the question scrolls the page");
        await AppFixture.AnswerPlayersAsync(page);

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

            if (g.NextLabel.Equals("Pick plays", StringComparison.OrdinalIgnoreCase))
                break;
            Assert.Equal("Next", g.NextLabel, ignoreCase: true);
            await page.ClickAsync("#tournext");
            await page.WaitForTimeoutAsync(150);
        }
        Assert.Equal(6, slides);

        // the first time through, the way out is the play library — where a new coach chooses
        await page.ClickAsync("#tournext");
        await page.WaitForSelectorAsync(".lib .lrow");
        Assert.Equal("library", await page.EvaluateAsync<string>("S.screen"));

        // and the library says what it is, all of it on screen, until he has read it
        await page.WaitForSelectorAsync("#welsheet");
        var fits = await page.EvaluateAsync<bool>("""
            () => { const s = document.querySelector('#welsheet .sheet'), r = s.getBoundingClientRect();
                    return r.top >= 0 && r.bottom <= innerHeight && r.left >= 0 && r.right <= innerWidth
                        && s.scrollHeight <= s.clientHeight + 1; }
            """);
        Assert.True(fits, $"{size}: the library's note does not fit on screen");
        var ok = (await page.Locator("#wel-ok").BoundingBoxAsync())!;
        Assert.True(ok.Height >= 44, $"{size}: Got it is only {ok.Height}px tall");
        await page.ClickAsync("#wel-ok");
        Assert.Equal(0, await page.Locator("#welsheet").CountAsync());
        await page.ClickAsync("#done");
        await page.WaitForSelectorAsync(".tile");
        Assert.Equal("1", await page.EvaluateAsync<string>("localStorage.getItem('hb.tour')"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public async Task ItShowsOnceAndComesBackFromTheMenu()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], fresh: true);
        await AppFixture.AnswerPlayersAsync(page);

        // Skip is the coach saying no, and that counts as having seen it
        Assert.Equal("Skip tutorial", (await page.InnerTextAsync("#tourskip")).Trim(), ignoreCase: true);
        await page.ClickAsync("#tourskip");
        await page.WaitForSelectorAsync(".lib .lrow");   // skipping lands where a new coach chooses, too
        await page.ClickAsync("#wel-ok");

        // the second START on this tablet goes straight to the deck, from storage and not memory
        await page.ReloadAsync();
        await page.WaitForTimeoutAsync(500);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".tiles");
        Assert.Equal("deck", await page.EvaluateAsync<string>("S.screen"));
        Assert.Equal(0, await page.Locator(".tour").CountAsync());

        // the library's note was for the first visit: from the menu it is just the library
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForSelectorAsync(".lib .lrow");
        await page.WaitForTimeoutAsync(200);
        Assert.Equal(0, await page.Locator("#welsheet").CountAsync());
        await page.ClickAsync("#done");
        await page.WaitForSelectorAsync(".tiles");
        Assert.Equal(0, await page.Locator("#askfmt").CountAsync());

        // it is still one tap away, and from there the buttons say so
        // the menu's tutorial does not ask again: this coach has answered
        await page.ClickAsync("#ham");
        await page.ClickAsync("#tour");
        await page.WaitForSelectorAsync(".tour[data-slide]");
        Assert.Equal(0, await page.Locator("#askfmt").CountAsync());
        Assert.Equal("Close", (await page.InnerTextAsync("#tourskip")).Trim(), ignoreCase: true);
        Assert.True(await page.Locator("#tourback").IsDisabledAsync(), "Back is live on the first slide");
        await page.ClickAsync("#tournext");
        await page.WaitForTimeoutAsync(150);
        Assert.False(await page.Locator("#tourback").IsDisabledAsync(), "Back is dead on the second slide");
        await page.ClickAsync("#tourback");
        await page.WaitForTimeoutAsync(150);
        Assert.Equal("1", await page.GetAttributeAsync(".tour", "data-slide"));
        await page.ClickAsync("#tourskip");
        await page.WaitForSelectorAsync(".tiles");                        // from the menu, back to the deck
        Assert.Equal("deck", await page.EvaluateAsync<string>("S.screen"));

        // Start over is the one thing that makes this somebody's first time again
        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await AppFixture.SetupTabAsync(page, "tablet");
        await page.WaitForSelectorAsync("#allreset");
        await page.ClickAsync("#allreset");
        await page.WaitForTimeoutAsync(200);
        await page.ClickAsync("#allreset");
        await page.WaitForTimeoutAsync(400);
        Assert.Null(await page.EvaluateAsync<string?>("localStorage.getItem('hb.tour')"));
        // ...lands on the intro, and its START asks again, with the kept answer lit
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync("#askfmt");
        Assert.Equal("6", await page.GetAttributeAsync("#askfmt .askbtn.on", "data-players"));
        await AppFixture.AnswerPlayersAsync(page);

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// The answer is the format, from the first slide on: pick 7 and the
    /// tutorial's tile is a 7-on-7 play, Setup says 7, and the next open of
    /// the app counts seven kids — from storage, not memory.
    /// </summary>
    [Fact]
    public async Task TheFirstAnswerIsTheFormatFromTheFirstSlideOn()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], fresh: true);
        await AppFixture.AnswerPlayersAsync(page, 7);

        Assert.Equal(7, await page.EvaluateAsync<int>("cfg.players"));
        Assert.Equal(7, await page.EvaluateAsync<int>("DATA.players"));
        Assert.Contains(await page.EvaluateAsync<string>("inDeck()[0].coachName"),
            await page.InnerTextAsync(".tourart"));
        Assert.StartsWith("s_", await page.EvaluateAsync<string>("inDeck()[0].id"));
        Assert.True(await page.EvaluateAsync<bool>(
            "events.some(e => e.e === 'setting' && e.p.key === 'players' && e.p.first)"), "the answer was not logged");

        await page.ClickAsync("#tourskip");
        await page.WaitForSelectorAsync(".lib .lrow");
        Assert.Contains("7 on 7", await page.InnerTextAsync("#welsheet"));
        await page.ClickAsync("#wel-ok");
        Assert.Equal("7 ON 7", await page.InnerTextAsync("#lfmt"));
        Assert.StartsWith("s_", await page.GetAttributeAsync(".lrow", "data-id"));
        await page.ReloadAsync();
        await page.WaitForSelectorAsync("#intro-message");
        Assert.Equal("Seven kids. One clear job each.", await page.InnerTextAsync("#intro-message"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
}
