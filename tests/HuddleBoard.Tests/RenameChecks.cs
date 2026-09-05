using HuddleBoard.Playbook;

using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// A coach's own play names: set from the library, read everywhere the shipped
/// name was read, kept through an update, and put back in one place.
/// </summary>
/// <remarks>
/// This runs against the hosted build rather than the standalone file, because
/// the whole point is storage and a file:// origin has none. The update is a
/// real one — a fresh build with a new version, offered by the service worker
/// and taken on tap — so "survives an update" means what it says.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class RenameChecks(AppFixture app) : IDisposable
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    /// <summary>Play 1: shipped as 22 DIVE / BULLDOZER, and in the default deck.</summary>
    private const string Row = ".lrow[data-id=\"p_01\"]";

    private const string Tile = ".tile[data-id=\"p_01\"]";

    /// <summary>Kid names are the exporter's, not the play's — read it back out.</summary>
    private static readonly string ShippedKidName =
        System.Text.Json.JsonDocument.Parse(
                File.ReadAllText(Path.Combine(Workspace.Dist, "proto_data.json")))
            .RootElement.GetProperty("plays")[0].GetProperty("kidName").GetString()!;

    private readonly StaticSite _site = new(Workspace.Deploy);

    public void Dispose()
    {
        _site.Dispose();

        // put the real version back in dist/ for whatever runs next
        AppBuilder.Run(output: TextWriter.Null);
    }

    [Fact]
    public async Task NamesCanBeChangedSurviveAnUpdateAndAreResetOnDemand()
    {
        var shipped = PlayLibrary.All.Single(p => p.Num == 1);
        var context = await app.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = Desk.Width, Height = Desk.Height },
        });
        await AppFixture.MarkTourSeenAsync(context);
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);

        var url = _site.Origin + "/index.html";
        await page.GotoAsync(url);
        await page.WaitForTimeoutAsync(1500);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        var installed = await page.EvaluateAsync<string>("APP_BUILD");

        async Task<string> RowName() => (await page.InnerTextAsync(Row + " .lname b")).Trim();

        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForSelectorAsync(Row);
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(shipped.Name, await RowName());
        Assert.Equal(0, await page.Locator(Row + " [data-ren].on").CountAsync());
        Assert.Equal(1, await page.Locator(Row + ".on").CountAsync());   // play 1 ships in the deck

        // rename both halves at once
        await page.ClickAsync(Row + " [data-ren]");
        await page.WaitForSelectorAsync("#rensheet");
        await page.FillAsync("#rn-real", "44 Belly");
        await page.FillAsync("#rn-fun", "Cannonball");
        await page.ClickAsync("#rn-save");
        await page.WaitForTimeoutAsync(400);

        Assert.Equal(0, await page.Locator("#rensheet").CountAsync());
        Assert.Equal("44 BELLY", await RowName());
        Assert.Equal(1, await page.Locator(Row + " [data-ren].on").CountAsync());
        Assert.Equal(1, await page.Locator(Row + ".on").CountAsync());
            // ^ the pencil sits inside the row's own button; if its click leaked,
            //   play 1 would have been dropped from the deck instead.

        // search finds it by the new name and still by the one it shipped with
        await page.FillAsync("#lq", "cannonball");
        await page.WaitForTimeoutAsync(350);
        Assert.Equal(1, await page.Locator(".lrow").CountAsync());
        await page.FillAsync("#lq", shipped.Name);
        await page.WaitForTimeoutAsync(350);
        Assert.Equal(1, await page.Locator(Row).CountAsync());
        await page.FillAsync("#lq", "");
        await page.WaitForTimeoutAsync(350);

        // it reads through to the deck and the play screen, real and fun alike
        await page.ClickAsync("#done");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal("44 BELLY", (await page.InnerTextAsync(Tile + " .kid")).Trim());
        await page.EvaluateAsync("cfg.funNames = true; saveCfg(); renderDeck();");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal("CANNONBALL", (await page.InnerTextAsync(Tile + " .kid")).Trim());

        await page.ClickAsync(Tile);
        await page.WaitForTimeoutAsync(400);
        Assert.Equal("CANNONBALL", (await page.InnerTextAsync(".titlewrap .kid")).Trim());
        Assert.Equal("44 BELLY", (await page.InnerTextAsync(".titlewrap .coach")).Trim());
        await page.ClickAsync("#back");
        await page.WaitForTimeoutAsync(400);

        // ship a new build and take it. The names are the coach's, not the build's.
        Assert.Equal(0, AppBuilder.Run("9.9.9-rename", TextWriter.Null));
        await page.EvaluateAsync("navigator.serviceWorker.getRegistration().then(r => r.update())");
        await page.WaitForTimeoutAsync(2500);
        await page.ClickAsync("#upd");
        await page.WaitForTimeoutAsync(2500);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        await page.WaitForTimeoutAsync(400);

        Assert.Equal("9.9.9-rename", await page.EvaluateAsync<string>("APP_BUILD"));
        Assert.NotEqual(installed, await page.EvaluateAsync<string>("APP_BUILD"));
        Assert.Equal("CANNONBALL", (await page.InnerTextAsync(Tile + " .kid")).Trim());

        // reset all, from the one place that offers it, and only on the second tap
        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await page.WaitForSelectorAsync("#nmreset");
        await page.WaitForTimeoutAsync(300);
        Assert.False(await page.Locator("#nmreset").IsDisabledAsync(),
            "Reset all is dead while a play is renamed");

        await page.ClickAsync("#nmreset");
        await page.WaitForTimeoutAsync(200);
        Assert.Equal("SURE?", (await page.InnerTextAsync("#nmreset")).Trim().ToUpperInvariant());
        await page.ClickAsync("#nmreset");
        await page.WaitForTimeoutAsync(400);
        Assert.True(await page.Locator("#nmreset").IsDisabledAsync(),
            "Reset all is still live with nothing left to reset");

        await page.ClickAsync("#sdone");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(ShippedKidName, (await page.InnerTextAsync(Tile + " .kid")).Trim());

        // and it stays reset across a reload
        await page.ReloadAsync();
        await page.WaitForTimeoutAsync(1000);
        await page.ClickAsync("#start");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(ShippedKidName, (await page.InnerTextAsync(Tile + " .kid")).Trim());

        await context.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A play name is the one string in this app a coach types, so it is the one
    /// string that can carry markup. It has to land as text, not as tags.
    /// </summary>
    [Fact]
    public async Task ATypedNameCannotBreakTheMarkup()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.EvaluateAsync(
            "names = {}; setNames('p_01', '<b>X</b> & CO', 'A\"B'); renderDeck();");
        await page.WaitForTimeoutAsync(400);

        Assert.Equal("<B>X</B> & CO", (await page.InnerTextAsync(Tile + " .kid")).Trim());
        Assert.Equal(0, await page.Locator(Tile + " .kid b").CountAsync());

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
