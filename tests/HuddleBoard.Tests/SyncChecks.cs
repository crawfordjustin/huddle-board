using System.Text.Json;

using HuddleBoard.Playbook;

using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// Sync tablets: one coach's deck, names, settings and sideline, exported as a
/// file and imported on an assistant's tablet — which then agrees with the
/// first one, keeps its own game log, and still agrees after a reload.
/// </summary>
/// <remarks>
/// Hosted rather than standalone, because the whole point is what reaches
/// storage on the second tablet. Each tablet is its own browser context, which
/// is what gives it its own localStorage.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class SyncChecks(AppFixture app) : IDisposable
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    private readonly StaticSite _site = new(Workspace.Deploy);

    public void Dispose() => _site.Dispose();

    /// <summary>Everything the file carries, in one comparable line.</summary>
    private const string StateProbe = """
        () => [deck.join(','), JSON.stringify(names), JSON.stringify(packSaves), JSON.stringify(packNames),
               cfg.clockSecs, cfg.warnAt, cfg.vibrate, cfg.funNames, cfg.showClock, ourSide].join('|')
        """;

    /// <summary>A fresh tablet: its own context, its own storage, past the intro.</summary>
    private async Task<(IBrowserContext Context, IPage Page, List<string> Errors)> OpenTabletAsync()
    {
        var context = await app.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = Desk.Width, Height = Desk.Height },
            AcceptDownloads = true,
        });
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);

        await page.GotoAsync(_site.Origin + "/index.html");
        await page.WaitForTimeoutAsync(1200);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        return (context, page, errors);
    }

    private static async Task OpenSetupAsync(IPage page)
    {
        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        // the picker is a hidden input behind the Import button, so wait for it to exist, not to show
        await page.WaitForSelectorAsync("#syncfile",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached });
        await page.WaitForTimeoutAsync(300);
    }

    /// <summary>
    /// Feeds a file to Import the way the system picker would, and waits for the
    /// row to report on it.
    /// </summary>
    private static async Task ImportAsync(IPage page, string path)
    {
        await page.SetInputFilesAsync("#syncfile", path);
        await page.WaitForSelectorAsync("#syncmsg");
        await page.WaitForTimeoutAsync(300);
    }

    [Fact]
    public async Task OneTabletsChoicesLandOnAnotherAndStick()
    {
        // ---- the head coach's tablet, changed from the way it came
        var (a, pageA, errorsA) = await OpenTabletAsync();
        await pageA.EvaluateAsync("""
            () => {
              setNames('p_01', 'POWER O', 'STAMPEDE');
              setNames('p_05', '', 'TANGLE');
              setPack('week2', DATA.plays.slice(4, 9).map(p => p.id));
              setPackName('week2', 'PLAYOFFS');
              deck = DATA.plays.slice(2, 13).map(p => p.id); saveDeck();
              cfg.clockSecs = 25; cfg.warnAt = 15; cfg.funNames = true; cfg.showClock = false;
              saveCfg();
              setOurSide('orange');
              renderDeck();
            }
            """);
        await pageA.WaitForTimeoutAsync(300);
        var wanted = await pageA.EvaluateAsync<string>(StateProbe);

        // headless Chromium has no share sheet, so Export falls through to a download
        await OpenSetupAsync(pageA);
        var download = await pageA.RunAndWaitForDownloadAsync(() => pageA.ClickAsync("#syncexp"));
        Assert.StartsWith("huddle-setup-", download.SuggestedFilename);
        Assert.EndsWith(".json", download.SuggestedFilename);
        // Playwright throws the download away with its context, so keep our own copy
        var dir = Path.Combine(Path.GetTempPath(), "hb-sync-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, download.SuggestedFilename);
        await download.SaveAsAsync(path);
        Assert.True(await pageA.EvaluateAsync<bool>("events.some(e => e.e === 'sync_export')"),
            "the export was not logged");

        using (var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path)))
        {
            var root = doc.RootElement;
            Assert.Equal("huddleboard.setup", root.GetProperty("format").GetString());
            Assert.Equal(1, root.GetProperty("v").GetInt32());
            Assert.Equal(11, root.GetProperty("deck").GetArrayLength());
            Assert.Equal("orange", root.GetProperty("ourSide").GetString());
            Assert.Equal(25, root.GetProperty("settings").GetProperty("clockSecs").GetInt32());
            Assert.Equal("STAMPEDE",
                root.GetProperty("names").GetProperty("p_01").GetProperty("kid").GetString());
            // every week is spelled out, shipped or saved, so the file stands alone
            var packs = root.GetProperty("packs");
            Assert.Equal(PlayPacks.All.Count, packs.EnumerateObject().Count());
            Assert.Equal(5, packs.GetProperty("week2").GetArrayLength());
            Assert.Equal(4, packs.GetProperty("week1").GetArrayLength());
            // a week's name travels like a play's: only the one that differs
            var packNames = root.GetProperty("packNames");
            Assert.Single(packNames.EnumerateObject());
            Assert.Equal("PLAYOFFS", packNames.GetProperty("week2").GetString());
            // the file carries choices, never the recording
            Assert.False(root.TryGetProperty("events", out _), "the game log leaked into the setup file");
        }
        await a.CloseAsync();
        Assert.True(errorsA.Count == 0, string.Join("\n", errorsA));

        // ---- the assistant's tablet, straight out of the box, with a log of its own
        var (b, pageB, errorsB) = await OpenTabletAsync();
        var shipped = await pageB.EvaluateAsync<string>(StateProbe);
        Assert.NotEqual(wanted, shipped);
        // a week this tablet saved on its own has to go: the file decides every slot
        await pageB.EvaluateAsync("setPack('week5', DATA.plays.slice(0, 3).map(p => p.id));");
        await pageB.EvaluateAsync("setPackName('week5', 'BYE WEEK');");
        await pageB.EvaluateAsync("logEvent('scratch'); flushLog();");
        var loggedBefore = await pageB.EvaluateAsync<int>("events.length");
        Assert.True(loggedBefore > 0, "nothing in the log to protect");

        await OpenSetupAsync(pageB);
        await ImportAsync(pageB, path);

        Assert.Equal(wanted, await pageB.EvaluateAsync<string>(StateProbe));
        var msg = await pageB.InnerTextAsync("#syncmsg");
        Assert.Contains("11-play deck", msg);
        Assert.Contains("2 renamed plays", msg);
        Assert.Contains("1 saved week", msg);
        Assert.Contains("1 renamed week", msg);
        Assert.Contains("ORANGE", msg);
        Assert.DoesNotContain("not recognised", msg);
        Assert.Equal("week2", await pageB.EvaluateAsync<string>("Object.keys(packSaves).join(',')"));
        Assert.Equal("week2", await pageB.EvaluateAsync<string>("Object.keys(packNames).join(',')"));

        // the log is this tablet's recording; a sync must not eat it
        Assert.True(await pageB.EvaluateAsync<int>("events.length") > loggedBefore,
            "Import cleared the game log");
        Assert.True(await pageB.EvaluateAsync<bool>("events.some(e => e.e === 'sync_import')"),
            "the import itself was not logged");

        // the counts on Setup already tell the new story
        Assert.Contains("2 plays renamed", await pageB.InnerTextAsync(".setrows"));

        // and it reached storage, not just memory
        await pageB.ReloadAsync();
        await pageB.WaitForTimeoutAsync(1000);
        await pageB.ClickAsync("#start");
        await pageB.WaitForTimeoutAsync(400);
        Assert.Equal(wanted, await pageB.EvaluateAsync<string>(StateProbe));
        Assert.Equal(11, await pageB.Locator(".tile").CountAsync());
        Assert.Equal("TANGLE",
            (await pageB.InnerTextAsync(".tile[data-id=\"p_05\"] .kid")).Trim());

        await b.CloseAsync();
        Directory.Delete(dir, recursive: true);
        Assert.True(errorsB.Count == 0, string.Join("\n", errorsB));
    }

    /// <summary>
    /// A setup file is the second string in this app that somebody else wrote.
    /// Play ids it names must exist, settings must be values Setup itself offers,
    /// and a name with markup in it has to land as text.
    /// </summary>
    [Fact]
    public async Task ImportTrustsNothingInTheFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hb-sync-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var hostile = Path.Combine(dir, "hostile.json");
        await File.WriteAllTextAsync(hostile, """
            {"format":"huddleboard.setup","v":1,
             "deck":["p_03","p_99","p_03",7,"p_04"],
             "names":{"p_01":{"coach":"<b>X</b> & CO","kid":"A\"B"},
                      "p_77":{"coach":"GHOST"},
                      "p_02":"not an object"},
             "settings":{"clockSecs":99,"warnAt":15,"vibrate":"yes","showClock":false,
                         "funNames":true,"__proto__":{"polluted":true}},
             "packs":{"week3":["p_02","p_99","p_02",5],"week9":["p_01"],"week4":"nope","week5":[]},
             "packNames":{"week2":"<b>X</b> & CO","week9":"GHOST","week3":5,"week1":" week 1 "},
             "ourSide":"left"}
            """);
        var garbage = Path.Combine(dir, "garbage.json");
        await File.WriteAllTextAsync(garbage, "{\"format\":\"huddleboard.events\",\"v\":1,\"events\":[]}");

        var (ctx, page, errors) = await OpenTabletAsync();
        await page.EvaluateAsync("cfg.clockSecs = 30; saveCfg(); setOurSide('orange');");
        await OpenSetupAsync(page);
        await ImportAsync(page, hostile);

        // deck: known ids only, once each, in the file's order
        Assert.Equal("p_03,p_04", await page.EvaluateAsync<string>("deck.join(',')"));
        // names: only for plays that exist, and only as tidy text
        Assert.Equal("p_01", await page.EvaluateAsync<string>("Object.keys(names).join(',')"));
        // settings: the good values land, the bad ones fall back to the shipped default —
        // never to whatever this tablet had before, so the result depends on the file alone
        Assert.Equal(40, await page.EvaluateAsync<int>("cfg.clockSecs"));
        Assert.Equal(15, await page.EvaluateAsync<int>("cfg.warnAt"));
        Assert.True(await page.EvaluateAsync<bool>("cfg.vibrate === true"));
        Assert.True(await page.EvaluateAsync<bool>("cfg.showClock === false"));
        Assert.True(await page.EvaluateAsync<bool>("cfg.funNames === true"));
        Assert.True(await page.EvaluateAsync<bool>("cfg.polluted === undefined"));
        // a sideline that is not a colour is not a sideline
        Assert.Equal("orange", await page.EvaluateAsync<string>("ourSide"));
        // saved weeks: a slot the build knows, holding plays the library has, once each
        Assert.Equal("week3", await page.EvaluateAsync<string>("Object.keys(packSaves).join(',')"));
        Assert.Equal("p_02", await page.EvaluateAsync<string>("packSaves.week3.join(',')"));
        // week names: a slot the build knows, text only, and the shipped name retyped is not a rename
        Assert.Equal("week2", await page.EvaluateAsync<string>("Object.keys(packNames).join(',')"));
        Assert.Equal("<b>X</b> & CO", await page.EvaluateAsync<string>("packNames.week2"));

        var msg = await page.InnerTextAsync("#syncmsg");
        Assert.Contains("Imported", msg);
        Assert.Contains("not recognised", msg);

        // the markup in the name is text on the tile, not a tag in it
        await page.ClickAsync("#sdone");
        await page.WaitForSelectorAsync(".tile");
        await page.EvaluateAsync("deck = ['p_01']; saveDeck(); renderDeck();");
        await page.WaitForTimeoutAsync(400);
        const string tile = ".tile[data-id=\"p_01\"]";
        Assert.Equal("A\"B", (await page.InnerTextAsync(tile + " .kid")).Trim());
        Assert.Equal(0, await page.Locator(tile + " .kid b").CountAsync());
        await page.EvaluateAsync("cfg.funNames = false; saveCfg(); renderDeck();");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal("<B>X</B> & CO", (await page.InnerTextAsync(tile + " .kid")).Trim());
        Assert.Equal(0, await page.Locator(tile + " .kid b").CountAsync());

        // and the markup in the week's name is text on its chip, not a tag in it
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForSelectorAsync("#pack-week2");
        Assert.StartsWith("<B>X</B> & CO", (await page.InnerTextAsync("#pack-week2")).Trim().ToUpperInvariant());
        Assert.Equal(0, await page.Locator("#pack-week2 b").CountAsync());
        await page.ClickAsync("#done");
        await page.WaitForSelectorAsync(".tile");

        // a file that is not a setup file changes nothing at all, and says so
        var before = await page.EvaluateAsync<string>(StateProbe);
        await OpenSetupAsync(page);
        await ImportAsync(page, garbage);
        Assert.Equal(before, await page.EvaluateAsync<string>(StateProbe));
        Assert.Contains("not a Huddle Board setup file", await page.InnerTextAsync("#syncmsg"));
        Assert.False(await page.EvaluateAsync<bool>(
            "events.filter(e => e.e === 'sync_import').length > 1"),
            "a rejected file was logged as an import");

        // the message is for the render it belongs to, not every render after
        await page.ClickAsync("#sdone");
        await page.WaitForSelectorAsync(".tile");
        await OpenSetupAsync(page);
        Assert.Equal(0, await page.Locator("#syncmsg").CountAsync());

        await ctx.CloseAsync();
        Directory.Delete(dir, recursive: true);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
