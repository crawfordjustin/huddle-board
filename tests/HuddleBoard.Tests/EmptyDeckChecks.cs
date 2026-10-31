using HuddleBoard.Playbook;

namespace HuddleBoard.Tests;

/// <summary>
/// A deck can be empty. A coach who clears it to build his own sees what the
/// play library is and one button to it, and the deck stays empty — through a
/// reload and through a sync — until he puts something in it. Only Start over
/// brings Week 1 back.
/// </summary>
public sealed class EmptyDeckChecks(AppFixture app)
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    [Fact]
    public async Task TheLastPlayCanComeOutAndTheDeckSaysWhereThePlaysAre()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForSelectorAsync(".lrow");
        foreach (var n in PlayPacks.Starting)
        {
            await page.ClickAsync($".lrow[data-id=\"p_{n:00}\"] .lmark");
            await page.WaitForTimeoutAsync(120);
        }
        Assert.Equal(0, await page.EvaluateAsync<int>("deck.length"));
        Assert.Equal("0 IN YOUR DECK", (await page.InnerTextAsync("#lcount")).Trim().ToUpperInvariant());

        await page.ClickAsync("#done");
        await page.WaitForSelectorAsync("#dempty");
        Assert.Equal(0, await page.Locator(".tile").CountAsync());
        Assert.Equal(0, await page.Locator("#dfilt").CountAsync());       // nothing to narrow
        Assert.Contains("play library", (await page.InnerTextAsync("#dempty")).ToLowerInvariant());
        Assert.Contains("0 PLAYS", (await page.InnerTextAsync(".dh-right")).ToUpperInvariant());

        // an empty deck stays empty: a reload does not quietly refill it with Week 1
        await page.ReloadAsync();
        await page.WaitForTimeoutAsync(400);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync("#dempty");

        // the button goes to the library
        await page.ClickAsync("#dlib");
        await page.WaitForSelectorAsync(".lib");
        Assert.Equal(0, await page.Locator(".lrow.on").CountAsync());

        // and Start over is what brings Week 1 back
        await page.EvaluateAsync("resetEverything(); renderDeck();");
        await page.WaitForTimeoutAsync(300);
        Assert.Equal(PlayPacks.Starting.Count, await page.Locator(".tile").CountAsync());

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// An empty deck in a sync file is a choice like any other deck, so the
    /// tablet that imports it ends up empty too, and nothing is reported as
    /// unrecognised. Neither are the empty weeks every file now spells out.
    /// </summary>
    [Fact]
    public async Task AnEmptyDeckTravelsInTheSyncFile()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        var dropped = await page.EvaluateAsync<int>("""
            (() => {
              deck = []; saveDeck();
              const s = readSetup(setupBlob());
              deck = DATA.defaultDeck.slice(); saveDeck();
              applySetup(s);
              return s.dropped;
            })()
            """);
        Assert.Equal(0, dropped);
        Assert.Equal(0, await page.EvaluateAsync<int>("deck.length"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task TheEmptyDeckFitsEveryTablet(string label, int width, int height)
    {
        var size = new Viewport(label, width, height);
        var (page, errors) = await app.OpenAppAsync(size);
        await page.EvaluateAsync("deck = []; saveDeck(); renderDeck();");
        await page.WaitForSelectorAsync("#dempty");
        await page.WaitForTimeoutAsync(200);

        foreach (var sel in new[] { "#dempty h2", "#dlib", "#ham" })
        {
            var box = await page.Locator(sel).BoundingBoxAsync();
            Assert.True(box is not null && box.X >= 0 && box.Y >= 0
                        && box.X + box.Width <= width + 1 && box.Y + box.Height <= height + 1,
                $"{sel} is off the screen at {size}");
        }

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
