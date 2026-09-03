using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// The deck's filter bar: the same two questions the library answers — run or
/// pass, and which situation — asked of the deck a coach actually carries.
/// </summary>
/// <remarks>
/// The danger with a filter on this screen is not that it fails to filter. It
/// is a coach picking the tablet up two drives later, seeing two plays, and
/// believing his deck is gone. So the checks that matter most here are the ones
/// about saying so: the header count, and the filter never surviving a trip
/// back through the intro.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class DeckFilterChecks(AppFixture app)
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    private static string DeckOf(int n) =>
        $"deck = DATA.plays.slice(0,{n}).map(p => p.id); saveDeck(); renderDeck();";

    [Fact]
    public async Task TheBarNarrowsTheDeckAndSaysHowMuchOfItIsShowing()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.EvaluateAsync(DeckOf(14));
        await page.WaitForTimeoutAsync(400);

        async Task<int> Tiles() => await page.Locator(".tile").CountAsync();
        async Task<string> Count() => (await page.InnerTextAsync(".dh-right p")).Trim();

        Assert.Equal(14, await Tiles());
        Assert.Equal("14 PLAYS", (await Count()).ToUpperInvariant());

        // a situation chip leaves only that situation
        await page.ClickAsync("#dfilt [data-cat=\"GOAL LINE\"]");
        await page.WaitForTimeoutAsync(400);
        var shown = await Tiles();
        Assert.InRange(shown, 1, 13);
        Assert.Equal($"{shown} OF 14", (await Count()).ToUpperInvariant());
        Assert.True(
            await page.EvalOnSelectorAllAsync<bool>(
                ".tile .catchip", "e => e.length > 0 && e.every(c => c.textContent.trim() === 'GOAL LINE')"),
            "a play from another situation survived the filter");

        // run/pass is the other facet, and the two combine
        await page.ClickAsync("#dfilt [data-kind=\"pass\"]");
        await page.WaitForTimeoutAsync(400);
        Assert.True(
            await page.EvalOnSelectorAllAsync<bool>(".tile .kind", "e => e.every(k => k.textContent.trim() === 'Pass')"),
            "the Pass filter let a run through");

        // back to everything
        await page.ClickAsync("#dfilt [data-cat=\"all\"]");
        await page.ClickAsync("#dfilt [data-kind=\"all\"]");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(14, await Tiles());
        Assert.Equal("14 PLAYS", (await Count()).ToUpperInvariant());

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Run plus a situation the deck has no run in is reachable in two taps, and
    /// it must not look like the deck was emptied.
    /// </summary>
    [Fact]
    public async Task AnEmptyCombinationSaysSoAndOffersTheWayBack()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.EvaluateAsync(DeckOf(14));
        await page.WaitForTimeoutAsync(400);

        await page.ClickAsync("#dfilt [data-kind=\"run\"]");
        await page.ClickAsync("#dfilt [data-cat=\"GOAL LINE\"]");
        await page.WaitForTimeoutAsync(400);

        Assert.Equal(0, await page.Locator(".tile").CountAsync());
        Assert.Equal(1, await page.Locator(".lempty").CountAsync());
        Assert.Contains("14", await page.InnerTextAsync(".decknote"));

        await page.ClickAsync("#dclear");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(14, await page.Locator(".tile").CountAsync());

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// The chips share the top bar with the clock and the menu, so they cost
    /// nothing and show for any deck with something to narrow by. They stay
    /// away only when every play would answer both questions the same way.
    /// </summary>
    [Fact]
    public async Task TheBarStaysAwayOnlyWhenThereIsNothingToNarrow()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);

        // one play: one run, one situation — nothing to switch to
        await page.EvaluateAsync(DeckOf(1));
        await page.WaitForTimeoutAsync(300);
        Assert.Equal(0, await page.Locator("#dfilt").CountAsync());

        // the shipped starting deck of four mixes run and pass, so it filters
        await page.EvaluateAsync("deck = DATA.defaultDeck.slice(); saveDeck(); renderDeck();");
        await page.WaitForTimeoutAsync(300);
        Assert.Equal(1, await page.Locator("#dfilt").CountAsync());

        await page.EvaluateAsync(DeckOf(14));
        await page.WaitForTimeoutAsync(300);
        Assert.Equal(1, await page.Locator("#dfilt").CountAsync());

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A filter left on is a deck that looks eaten. Leaving to the intro is the
    /// one moment it has to let go.
    /// </summary>
    [Fact]
    public async Task LeavingToTheIntroClearsTheFilter()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.EvaluateAsync(DeckOf(14));
        await page.WaitForTimeoutAsync(400);

        await page.ClickAsync("#dfilt [data-cat=\"GOAL LINE\"]");
        await page.WaitForTimeoutAsync(400);
        Assert.InRange(await page.Locator(".tile").CountAsync(), 1, 13);

        await page.ClickAsync("#ham");
        await page.ClickAsync("#exit");
        await page.WaitForSelectorAsync("#start");
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".tiles");
        await page.WaitForTimeoutAsync(400);

        Assert.Equal(14, await page.Locator(".tile").CountAsync());

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Every tile says its situation, at every column count. It used to be
    /// hidden the moment the grid went past two columns — which is every deck
    /// big enough for a coach to have to hunt through.
    /// </summary>
    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task EveryTileShowsItsSituationAndNothingIsClipped(string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height));
        await page.EvaluateAsync(DeckOf(14));
        await page.WaitForTimeoutAsync(500);

        var tiles = await page.Locator(".tile").CountAsync();
        Assert.Equal(14, tiles);

        var chips = await page.EvalOnSelectorAllAsync<int>(
            ".tile .catchip", "e => e.filter(c => c.getBoundingClientRect().width > 0).length");
        Assert.Equal(tiles, chips);

        // nothing on a tile may fall outside the card. The tagline is the one
        // line allowed to disappear to keep that true; the rest may not be cut.
        var spilled = await page.EvalOnSelectorAllAsync<string[]>(".tile", """
            els => els.filter(t => {
              const r = t.getBoundingClientRect();
              const cs = getComputedStyle(t);
              const lid = r.top + parseFloat(cs.paddingTop);
              const floor = r.bottom - parseFloat(cs.paddingBottom);
              return [...t.querySelectorAll('.kindrow, .kid, .coach, .tag')]
                .filter(e => e.getBoundingClientRect().height > 0)
                .some(e => {
                  const b = e.getBoundingClientRect();
                  return b.top < lid - 1.5 || b.bottom > floor + 1.5;
                });
            }).map(t => t.querySelector('.kid').textContent.trim())
            """);

        await page.CloseAsync();

        Assert.True(spilled.Length == 0,
            $"{label}: clipped out of the card: {string.Join(", ", spilled)}");
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
