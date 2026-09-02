using HuddleBoard.Playbook;

using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// Start over: the deck, every custom name and every setting back to the way the
/// tablet arrived — and the game log, which is a recording rather than a
/// preference, deliberately left where it is.
/// </summary>
/// <remarks>
/// Hosted rather than standalone, because a reset that does not outlive the
/// reload has not reset anything. A file:// origin has no storage to prove it
/// with.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class ResetChecks(AppFixture app) : IDisposable
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    private readonly StaticSite _site = new(Workspace.Deploy);

    public void Dispose() => _site.Dispose();

    /// <summary>Everything a coach can change, in one readable line.</summary>
    private const string StateProbe = """
        () => [deck.slice().sort().join(','), renamedCount(), cfg.clockSecs,
               cfg.funNames, cfg.showClock, ourSide].join('|')
        """;

    [Fact]
    public async Task ResetTabletPutsEveryChoiceBackAndSticks()
    {
        var context = await app.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = Desk.Width, Height = Desk.Height },
        });
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);

        await page.GotoAsync(_site.Origin + "/index.html");
        await page.WaitForTimeoutAsync(1200);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");

        var shipped = await page.EvaluateAsync<string>(StateProbe);

        // change everything a coach can change, then bank an event log too
        await page.EvaluateAsync("""
            () => {
              setNames('p_01', 'POWER O', 'STAMPEDE');
              setNames('p_05', 'MESH', 'TANGLE');
              deck = DATA.plays.slice(0, 9).map(p => p.id); saveDeck();
              cfg.clockSecs = 25; cfg.funNames = true; cfg.showClock = false; saveCfg();
              setOurSide('orange');
              logEvent('scratch'); flushLog();
              renderDeck();
            }
            """);
        await page.WaitForTimeoutAsync(400);

        var dirty = await page.EvaluateAsync<string>(StateProbe);
        Assert.NotEqual(shipped, dirty);
        var loggedBefore = await page.EvaluateAsync<int>("events.length");
        Assert.True(loggedBefore > 0, "nothing in the log to protect");

        // one tap arms it, the second does it
        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await page.WaitForSelectorAsync("#allreset");
        await page.WaitForTimeoutAsync(300);

        await page.ClickAsync("#allreset");
        await page.WaitForTimeoutAsync(200);
        Assert.Equal("SURE?", (await page.InnerTextAsync("#allreset")).Trim().ToUpperInvariant());
        Assert.Equal(dirty, await page.EvaluateAsync<string>(StateProbe));   // one tap changes nothing

        await page.ClickAsync("#allreset");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(shipped, await page.EvaluateAsync<string>(StateProbe));

        // the log is a recording, not a preference — Start over must not eat it
        Assert.True(await page.EvaluateAsync<int>("events.length") > loggedBefore,
            "Start over cleared the game log, which has its own Clear");
        Assert.True(await page.EvaluateAsync<bool>("events.some(e => e.e === 'reset_all')"),
            "the reset itself was not logged");

        // and it survives the reload, so it reached storage and not just memory
        await page.ReloadAsync();
        await page.WaitForTimeoutAsync(1000);
        await page.ClickAsync("#start");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(shipped, await page.EvaluateAsync<string>(StateProbe));

        // the screen a coach lands back on is the shipped deck, not a stale one
        var tiles = await page.Locator(".tile").CountAsync();
        Assert.Equal(await page.EvaluateAsync<int>("DATA.defaultDeck.length"), tiles);

        await context.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
