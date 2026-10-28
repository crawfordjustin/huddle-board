using HuddleBoard.Playbook;

using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// Players on the field: 5, 6 or 7, picked in Setup. Each format is its own
/// library, and a coach's choices in one are kept apart from the others.
/// </summary>
/// <remarks>
/// The switch and the isolation are about storage, so they run against the
/// hosted build over a local <see cref="StaticSite"/>, like the other storage
/// checks. The fit runs on the standalone file at every size.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class FormatChecks(AppFixture app) : IDisposable
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    private readonly StaticSite _site = new(Workspace.Deploy);

    public void Dispose() => _site.Dispose();

    private static string Ids(Format f, IEnumerable<int> nums) => string.Join(",", nums.Select(f.PlayId));

    private static async Task PickAsync(IPage page, int players)
    {
        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await page.WaitForSelectorAsync(".setrows");
        await page.ClickAsync($".seg[data-key=\"players\"] button[data-v=\"{players}\"]");
        await page.WaitForTimeoutAsync(200);
        await page.ClickAsync("#sdone");
        await page.WaitForSelectorAsync(".tile, #dlib");
        await page.WaitForTimeoutAsync(300);
    }

    private static Task<string> DeckAsync(IPage page) =>
        page.EvaluateAsync<string>("[...document.querySelectorAll('.tile')].map(t => t.dataset.id).join(',')");

    /// <summary>
    /// Setup's first row switches the whole tablet: the deck is that format's
    /// Week 1, the badge says which it is, the intro counts its kids — and what
    /// a coach did in one format is still there when he comes back, through a
    /// reload, and never shows up in another.
    /// </summary>
    [Fact]
    public async Task EachFormatKeepsItsOwnChoices()
    {
        var context = await app.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = Desk.Width, Height = Desk.Height },
        });
        await AppFixture.MarkTourSeenAsync(context);
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);

        await page.GotoAsync(_site.Origin + "/index.html");
        await page.WaitForTimeoutAsync(1200);
        Assert.Equal("Six kids. One clear job each.", await page.InnerTextAsync("#intro-message"));
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        Assert.Equal("6 ON 6", await page.InnerTextAsync("#fmt"));
        Assert.Equal(Ids(Formats.Six, Formats.Six.Starting), await DeckAsync(page));

        // 6 on 6: a shorter deck and a renamed play
        await page.EvaluateAsync("""deck = ["p_01"]; saveDeck(); setNames("p_01", "44 BELLY", "");""");

        // 5 on 5: its own Week 1, its own badge, none of 6-on-6's choices
        await PickAsync(page, 5);
        Assert.Equal("5 ON 5", await page.InnerTextAsync("#fmt"));
        Assert.Equal(Ids(Formats.Five, Formats.Five.Starting), await DeckAsync(page));
        Assert.Equal(0, await page.EvaluateAsync<int>("renamedCount()"));
        Assert.Equal(Formats.Five.Plays.Count, await page.EvaluateAsync<int>("DATA.plays.length"));
        await page.EvaluateAsync("""setPack("week3", ["f_07", "f_09"]); deck = ["f_07"]; saveDeck();""");

        // 7 on 7, and every play on its screen has seven kids
        await PickAsync(page, 7);
        Assert.Equal("7 ON 7", await page.InnerTextAsync("#fmt"));
        Assert.Equal(Ids(Formats.Seven, Formats.Seven.Starting), await DeckAsync(page));
        Assert.Equal(0, await page.EvaluateAsync<int>("savedPackCount()"));
        await page.ClickAsync(".tile");
        await page.WaitForSelectorAsync("#calls");
        Assert.Equal(7, await page.EvaluateAsync<int>("Object.keys(SC.players).length"));
        await page.EvaluateAsync("renderDeck()");

        // back to 5, through a reload: the deck and the week are as he left them
        await PickAsync(page, 5);
        await page.ReloadAsync();
        await page.WaitForTimeoutAsync(1200);
        Assert.Equal("Five kids. One clear job each.", await page.InnerTextAsync("#intro-message"));
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        Assert.Equal("f_07", await DeckAsync(page));
        Assert.Equal("f_07,f_09", await page.EvaluateAsync<string>("packSaves.week3.join(',')"));

        // and 6 on 6 kept its own, under the keys it always had
        await PickAsync(page, 6);
        Assert.Equal("p_01", await DeckAsync(page));
        Assert.Equal("44 BELLY", await page.EvaluateAsync<string>("names.p_01.coach"));
        Assert.Equal("[\"p_01\"]", await page.EvaluateAsync<string>("localStorage.getItem('hb.deck')"));
        Assert.Equal("[\"f_07\"]", await page.EvaluateAsync<string>("localStorage.getItem('hb.deck.5')"));

        // Start over puts every format back, and keeps the division
        await PickAsync(page, 5);
        await page.EvaluateAsync("resetEverything()");
        Assert.Equal(5, await page.EvaluateAsync<int>("cfg.players"));
        Assert.Equal(0, await page.EvaluateAsync<int>("savedPackCount()"));
        Assert.Equal(Ids(Formats.Five, Formats.Five.Starting), await page.EvaluateAsync<string>("deck.join(',')"));
        await page.EvaluateAsync("loadMode(6)");
        Assert.Equal(Ids(Formats.Six, Formats.Six.Starting), await page.EvaluateAsync<string>("deck.join(',')"));
        Assert.Equal(0, await page.EvaluateAsync<int>("renamedCount()"));

        await context.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A sync file carries every format, each read against its own library: a
    /// 5-on-5 play named in the 7-on-7 section, or a 5-on-5 drawing filed
    /// there, is not recognised and is counted.
    /// </summary>
    [Fact]
    public async Task ASyncFileKeepsTheFormatsApart()
    {
        var (page, errors) = await app.OpenAppAsync(Desk, players: 5);
        var result = await page.EvaluateAsync<int[]>("""
            (() => {
              setNames("f_01", "BIG FIVE", "");
              const file = JSON.parse(setupBlob());
              const clean = readSetup(JSON.stringify(file)).dropped;
              file.modes[7].deck = ["f_01", "s_01"];
              file.modes[7].plays = [{id: "u_abcd1234", v: 1, base: "EMPTY", spots: {}, routes: {}}];
              const bad = readSetup(JSON.stringify(file));
              return [clean, bad.dropped, bad.modes[7].deck.length, Object.keys(bad.modes[5].names).length];
            })()
            """);
        Assert.Equal(0, result[0]);
        Assert.Equal(2, result[1]);
        Assert.Equal(1, result[2]);
        Assert.Equal(1, result[3]);

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// The deck and the library hold up at five and at seven, at every size:
    /// every play is in the library, nothing scrolls sideways, and the badge is
    /// on screen.
    /// </summary>
    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task TheDeckAndLibraryFitInEveryFormat(string label, int width, int height)
    {
        foreach (var players in new[] { 5, 7 })
        {
            var format = Formats.All.Single(f => f.Players == players);
            var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height), players: players);
            await page.EvaluateAsync($"deck = DATA.plays.map(p => p.id); saveDeck(); renderDeck()");
            await page.WaitForTimeoutAsync(500);
            Assert.Equal(format.Plays.Count, await page.Locator(".tile").CountAsync());
            Assert.True(await page.EvaluateAsync<bool>(
                "document.documentElement.scrollWidth <= innerWidth"), $"{format.Label} deck scrolls sideways");
            var badge = await page.Locator("#fmt").BoundingBoxAsync();
            Assert.True(badge is { Width: > 0 } && badge.X + badge.Width <= width, $"{format.Label} badge is off screen");

            await page.ClickAsync("#ham");
            await page.ClickAsync("#edit");
            await page.WaitForSelectorAsync(".lrow");
            await page.WaitForTimeoutAsync(300);
            Assert.Equal(format.Plays.Count, await page.Locator(".lrow").CountAsync());
            Assert.True(await page.EvaluateAsync<bool>(
                "document.documentElement.scrollWidth <= innerWidth"), $"{format.Label} library scrolls sideways");

            await page.CloseAsync();
            Assert.True(errors.Count == 0, $"{format.Label}: " + string.Join("\n", errors));
        }
    }
}
