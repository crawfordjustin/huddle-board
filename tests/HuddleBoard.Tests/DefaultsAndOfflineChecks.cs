using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// What a coach sees on a tablet that has never run this before, and what
/// happens on the standalone copy with no network at all.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class DefaultsAndOfflineChecks(AppFixture app)
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    [Fact]
    public async Task AFreshTabletShowsRealPlayNames()
    {
        var (page, errors) = await app.OpenAppAsync(Desk, settle: 500);

        Assert.False(await page.EvaluateAsync<bool>("cfg.funNames"),
            "a fresh install should start on the real play names, not the kid names");

        var tile = await page.EvalOnSelectorAsync<string>(".tile .kid", "e=>e.textContent.trim()");
        Assert.Equal("22 DIVE", tile);

        await page.EvaluateAsync("openPlay('p_01')");
        await page.WaitForTimeoutAsync(300);
        var title = await page.EvalOnSelectorAsync<string>(".titlewrap .kid", "e=>e.textContent.trim()");
        Assert.Equal("22 DIVE", title);

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public async Task ACoachWhoChoseFunNamesKeepsThem()
    {
        var context = await app.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = Desk.Width, Height = Desk.Height },
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(app.AppUri);
        await page.WaitForTimeoutAsync(400);

        await page.EvaluateAsync("cfg.funNames=true; saveCfg();");
        await page.ReloadAsync();
        await page.WaitForTimeoutAsync(500);
        await page.ClickAsync("#start");
        await page.WaitForTimeoutAsync(300);

        Assert.True(await page.EvaluateAsync<bool>("cfg.funNames"), "the choice did not survive a reload");
        Assert.Equal("BULLDOZER",
            await page.EvalOnSelectorAsync<string>(".tile .kid", "e=>e.textContent.trim()"));

        // and the toggle still works the other way
        await page.EvaluateAsync("cfg.funNames=false; saveCfg(); renderDeck();");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal("22 DIVE",
            await page.EvalOnSelectorAsync<string>(".tile .kid", "e=>e.textContent.trim()"));

        await context.CloseAsync();
    }

    /// <summary>
    /// The standalone file has to work with the radio off, from file://, with no
    /// service worker at all.
    /// </summary>
    [Fact]
    public async Task TheStandaloneFileWorksWithNoNetwork()
    {
        var context = await app.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = Desk.Width, Height = Desk.Height },
            Offline = true,
        });
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);

        await page.GotoAsync(app.AppUri);
        await page.WaitForTimeoutAsync(900);

        // the intro is the first thing offline too, and its art is drawn rather
        // than fetched precisely so that this holds
        Assert.Equal(6, await page.Locator(".introart .introkid").CountAsync());
        await page.ClickAsync("#start");
        await page.WaitForTimeoutAsync(300);

        Assert.True(await page.Locator(".tile").CountAsync() > 0, "no tiles rendered offline");
        Assert.False(string.IsNullOrWhiteSpace(await page.EvaluateAsync<string>("APP_BUILD")));
        Assert.True(await page.EvaluateAsync<bool>("!location.protocol.startsWith('http')"));

        await page.ClickAsync(".tile[data-id='p_01']");
        await page.ClickAsync("#stage");
        await page.WaitForTimeoutAsync(1500);
        Assert.True(await page.Locator("#field polyline").CountAsync() > 0,
            "no routes were drawn offline");

        await page.ClickAsync("#back");
        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await page.WaitForTimeoutAsync(300);

        await context.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
