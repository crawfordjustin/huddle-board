using HuddleBoard.Playbook;

using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// The hosted build: installs, caches offline, and offers a new build without
/// ever interrupting a live play.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class PwaChecks(AppFixture app) : IDisposable
{
    private readonly StaticSite _site = new(Workspace.Deploy);

    public void Dispose()
    {
        _site.Dispose();

        // put the real version back in dist/ for whatever runs next
        AppBuilder.Run(output: TextWriter.Null);
    }

    [Fact]
    public async Task ItInstallsCachesOfflineAndUpdatesOnlyOnTap()
    {
        var url = _site.Origin + "/index.html";
        var context = await app.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1600, Height = 1000 },
        });
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);

        await page.GotoAsync(url);
        await page.WaitForTimeoutAsync(1500);

        Assert.True(
            await page.EvaluateAsync<bool>("navigator.serviceWorker.getRegistration().then(r => !!r)"),
            "the service worker did not register");
        Assert.Equal(1, await page.Locator("link[rel=manifest]").CountAsync());
        var installed = await page.EvaluateAsync<string>("APP_BUILD");
        Assert.False(string.IsNullOrWhiteSpace(installed));

        // go offline and make sure it still boots from cache
        await page.WaitForTimeoutAsync(1200);
        await context.SetOfflineAsync(true);
        await page.GotoAsync(url);
        await page.WaitForTimeoutAsync(1200);
        Assert.Equal(0, await page.Locator("#upd").CountAsync());
        await page.ClickAsync("#start");
        await page.WaitForTimeoutAsync(300);
        Assert.True(await page.Locator(".tile").CountAsync() > 0, "offline reload showed no tiles");
        Assert.Equal(0, await page.Locator("#upd").CountAsync());
        await context.SetOfflineAsync(false);

        // ship a new build and confirm the tablet is offered it, but not given it
        Assert.Equal(0, AppBuilder.Run("9.9.9-test", TextWriter.Null));
        await page.EvaluateAsync("navigator.serviceWorker.getRegistration().then(r => r.update())");
        await page.WaitForTimeoutAsync(2500);

        Assert.Equal(1, await page.Locator("#upd").CountAsync());
        Assert.Equal(installed, await page.EvaluateAsync<string>("APP_BUILD"));

        // the intro makes the same offer — it is where a coach lands opening the
        // app before a game, the one moment an update costs nothing
        await page.ClickAsync("#ham");
        await page.ClickAsync("#exit");
        await page.WaitForSelectorAsync("#start");
        Assert.Equal(1, await page.Locator(".introsay #upd").CountAsync());
        Assert.Equal(installed, await page.EvaluateAsync<string>("APP_BUILD"));

        await page.ClickAsync("#upd");
        await page.WaitForTimeoutAsync(2500);
        Assert.Equal("9.9.9-test", await page.EvaluateAsync<string>("APP_BUILD"));

        await context.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}

/// <summary>
/// The Full screen button: offered in an ordinary tab and from a local file,
/// suppressed once the app is installed and already full screen.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class FullScreenChecks(AppFixture app) : IDisposable
{
    private readonly StaticSite _site = new(Workspace.Deploy);

    public void Dispose() => _site.Dispose();

    [Fact]
    public async Task InATabTheButtonIsOfferedAndWorks()
    {
        var page = await app.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
        });
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);

        await page.GotoAsync(_site.Origin + "/index.html");
        await page.WaitForTimeoutAsync(1200);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");

        Assert.Equal(1, await page.Locator("#fs").CountAsync());
        Assert.False(await page.EvaluateAsync<bool>("isImmersive()"));

        await page.ClickAsync("#fs");
        await page.WaitForTimeoutAsync(600);

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public async Task OnceInstalledTheButtonIsHidden()
    {
        var page = await app.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
        });

        // pretend the app was launched from the home screen
        await page.AddInitScriptAsync("""
            const mm = window.matchMedia;
            window.matchMedia = q => q.includes('display-mode: fullscreen')
              ? {matches:true, addEventListener(){}, removeEventListener(){}, media:q}
              : mm(q);
            """);

        await page.GotoAsync(_site.Origin + "/index.html");
        await page.WaitForTimeoutAsync(1000);
        // onto the deck, or the button being absent proves nothing — the intro
        // does not carry it either
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");

        Assert.Equal(0, await page.Locator("#fs").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("isImmersive()"));
        await page.CloseAsync();
    }

    /// <summary>A local file can never be installed, so the button stays.</summary>
    [Fact]
    public async Task FromALocalFileTheButtonIsStillOffered()
    {
        var (page, _) = await app.OpenAppAsync(new Viewport("tab", 1400, 900), settle: 700);
        Assert.Equal(1, await page.Locator("#fs").CountAsync());
        await page.CloseAsync();
    }
}
