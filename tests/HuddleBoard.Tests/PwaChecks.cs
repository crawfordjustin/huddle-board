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
    public void Dispose()
    {
        // put the real version back in dist/ for whatever runs next
        AppBuilder.Run(output: TextWriter.Null);
    }

    /// <summary>
    /// At the root is <c>dist/deploy</c> hosted alone, the static-host guarantee;
    /// at <c>/app</c> is the website, which serves the same files under a path.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("/app")]
    public async Task ItInstallsCachesOfflineAndUpdatesOnlyOnTap(string mount)
    {
        using var site = new StaticSite(Workspace.Deploy, mount);
        var url = site.Url + "index.html";
        var context = await app.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1600, Height = 1000 },
        });
        await AppFixture.MarkTourSeenAsync(context);
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);

        await page.GotoAsync(url);
        await page.WaitForTimeoutAsync(1500);

        Assert.True(
            await page.EvaluateAsync<bool>("navigator.serviceWorker.getRegistration().then(r => !!r)"),
            "the service worker did not register");
        Assert.Equal(site.Url,
            await page.EvaluateAsync<string>("navigator.serviceWorker.getRegistration().then(r => r.scope)"));
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
/// The website's root <c>/sw.js</c>: a browser that ran the app when it was
/// served at the root loses that worker on its next update check, and keeps the
/// app at <c>/app/</c> — its worker and its offline cache, which share the
/// root's <c>huddle-*</c> cache names because caches are per origin.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class KillSwitchChecks(AppFixture app) : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("hb-site-").FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // the browser can still be holding a file; it is a temp folder
        }
    }

    [Fact]
    public async Task TheOldRootWorkerGoesAndTheAppAtAppStays()
    {
        // the site as it was (the app at the root) and as it is (the app at
        // /app/). The root never served the build /app/ runs, so it gets a
        // version of its own; one shared version would be one shared cache.
        Directory.CreateDirectory(Path.Combine(_root, "app"));
        foreach (var file in Directory.GetFiles(Workspace.Deploy))
        {
            File.Copy(file, Path.Combine(_root, Path.GetFileName(file)));
            File.Copy(file, Path.Combine(_root, "app", Path.GetFileName(file)));
        }
        var oldWorker = Path.Combine(_root, "sw.js");
        File.WriteAllText(oldWorker,
            File.ReadAllText(oldWorker).Replace("const VERSION = \"", "const VERSION = \"old-root-"));
        using var site = new StaticSite(_root);

        var context = await app.Browser.NewContextAsync();
        await AppFixture.MarkTourSeenAsync(context);
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);

        await page.GotoAsync(site.Origin + "/index.html");
        await page.WaitForTimeoutAsync(1500);
        await page.GotoAsync(site.Origin + "/app/");
        await page.WaitForTimeoutAsync(1500);
        Assert.Equal([site.Origin + "/", site.Origin + "/app/"], await ScopesAsync(page));

        // deploy the website: the old root worker's next update check finds this
        File.Copy(Path.Combine(Workspace.Root, "src", "HuddleBoard.Web", "wwwroot", "sw.js"),
            Path.Combine(_root, "sw.js"), overwrite: true);
        await page.EvaluateAsync("""
            navigator.serviceWorker.getRegistrations().then(rs =>
              Promise.all(rs.filter(r => r.scope === location.origin + "/").map(r => r.update())))
            """);
        await page.WaitForTimeoutAsync(2500);

        Assert.Equal([site.Origin + "/app/"], await ScopesAsync(page));
        Assert.False(await CachedAsync(page, "/index.html"), "the root's cache survived");
        Assert.True(await CachedAsync(page, "/app/index.html"), "the kill-switch took /app/'s cache too");

        await context.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    private static async Task<string[]> ScopesAsync(IPage page) =>
        (await page.EvaluateAsync<string[]>(
            "navigator.serviceWorker.getRegistrations().then(rs => rs.map(r => r.scope))"))
        .Order(StringComparer.Ordinal).ToArray();

    private static Task<bool> CachedAsync(IPage page, string path) =>
        page.EvaluateAsync<bool>("""
            async path => {
              for (const key of await caches.keys())
                if (await (await caches.open(key)).match(location.origin + path)) return true;
              return false;
            }
            """, path);
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
        await AppFixture.MarkTourSeenAsync(page);
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
        await AppFixture.MarkTourSeenAsync(page);

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

    /// <summary>
    /// Installed, Exit closes the app's window; in a tab — even one the Full
    /// screen button made immersive — it only goes back to the intro, because
    /// the tab is the coach's. Either way the intro is drawn, so a close the OS
    /// refuses still lands somewhere.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExitClosesOnlyAnInstalledApp(bool installed)
    {
        var page = await app.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
        });
        await AppFixture.MarkTourSeenAsync(page);
        await page.AddInitScriptAsync($$"""
            window.__closed = 0;
            window.close = () => { window.__closed++; };
            if ({{(installed ? "true" : "false")}}) {
              const mm = window.matchMedia;
              window.matchMedia = q => q.includes('display-mode: fullscreen')
                ? {matches:true, addEventListener(){}, removeEventListener(){}, media:q}
                : mm(q);
            }
            """);

        await page.GotoAsync(_site.Origin + "/index.html");
        await page.WaitForTimeoutAsync(1000);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        Assert.Equal(installed, await page.EvaluateAsync<bool>("isInstalled()"));

        await page.ClickAsync("#ham");
        await page.ClickAsync("#exit");
        await page.WaitForSelectorAsync("#start");

        Assert.Equal("intro", await page.EvaluateAsync<string>("S.screen"));
        Assert.Equal(installed ? 1 : 0, await page.EvaluateAsync<int>("window.__closed"));
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
