using HuddleBoard.Playbook;

using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// Hiding shipped plays, from Setup's Your own tab: out of the library, out of
/// the deck and the saved weeks after a warning, back again on demand, per
/// format, carried by sync and undone by Start over.
/// </summary>
public sealed class HiddenPlayChecks(AppFixture app)
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    private static async Task OpenPickerAsync(IPage page)
    {
        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await AppFixture.SetupTabAsync(page, "yours");
        await page.ClickAsync("#hidepick");
        await page.WaitForSelectorAsync("#hidesheet .hrow");
    }

    private static Task<int> HiddenAsync(IPage page) => page.EvaluateAsync<int>("hidden.size");

    /// <summary>
    /// A play in the deck is hidden only on the second tap of Save, and then it
    /// is gone from the deck, the library, its search and Week 1. Showing it
    /// again puts it back in the library and nowhere else.
    /// </summary>
    [Fact]
    public async Task HidingTakesAPlayOutOfTheLibraryAndTheDeckAfterAWarning()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        Assert.True(await page.EvaluateAsync<bool>("deck.includes('p_01')"), "play 1 ships in the deck");

        await OpenPickerAsync(page);
        Assert.Equal(PlayLibrary.All.Count, await page.Locator("#hidesheet .hrow").CountAsync());
        Assert.Contains("IN YOUR DECK", (await page.InnerTextAsync(".hrow[data-id='p_01']")).ToUpperInvariant());
        await page.ClickAsync(".hrow[data-id='p_01']");
        await page.ClickAsync(".hrow[data-id='p_02']");
        Assert.Equal("Hidden", (await page.InnerTextAsync(".hrow[data-id='p_01'] .hstate")).Trim(), ignoreCase: true);

        // the first tap only says what will happen
        await page.ClickAsync("#hide-save");
        Assert.StartsWith("SURE? 1 LEAVES YOUR DECK", (await page.InnerTextAsync("#hide-save")).Trim().ToUpperInvariant());
        Assert.Equal(0, await HiddenAsync(page));
        Assert.True(await page.EvaluateAsync<bool>("deck.includes('p_01')"));

        await page.ClickAsync("#hide-save");
        await page.WaitForSelectorAsync("#hidesheet", new() { State = WaitForSelectorState.Detached });
        Assert.Equal(2, await HiddenAsync(page));
        Assert.False(await page.EvaluateAsync<bool>("deck.includes('p_01')"), "a hidden play stayed in the deck");
        Assert.Contains("p_01", await page.EvaluateAsync<string>("localStorage.getItem('hb.hidden')"));
        Assert.Contains($"{PlayLibrary.All.Count - 2} of {PlayLibrary.All.Count}", await page.InnerTextAsync(".setrows"));

        // gone from the deck and the library, its search, and what is left of Week 1
        await page.ClickAsync("#sdone");
        await page.WaitForSelectorAsync(".tiles");
        Assert.Equal(0, await page.Locator(".tile[data-id='p_01']").CountAsync());
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForSelectorAsync(".lrow");
        Assert.Equal(0, await page.Locator(".lrow[data-id='p_01'], .lrow[data-id='p_02']").CountAsync());
        Assert.Equal(PlayLibrary.All.Count - 2, await page.Locator(".lrow").CountAsync());
        Assert.Contains("2 shipped plays are hidden", await page.InnerTextAsync("#lnote"));
        Assert.Equal("3", (await page.InnerTextAsync("#pack-week1 .n")).Trim());
        await page.FillAsync("#lq", "22 DIVE");
        await page.WaitForTimeoutAsync(200);
        Assert.Equal(0, await page.Locator(".lrow").CountAsync());
        await page.ClickAsync("#done");

        // Show all brings them back to the library — not into the deck
        await OpenPickerAsync(page);
        await page.ClickAsync("#showall");
        await page.ClickAsync("#hide-save");
        await page.WaitForSelectorAsync("#hidesheet", new() { State = WaitForSelectorState.Detached });
        Assert.Equal(0, await HiddenAsync(page));
        Assert.False(await page.EvaluateAsync<bool>("deck.includes('p_01')"));
        Assert.Equal(PlayLibrary.All.Count, await page.EvaluateAsync<int>("shownPlays().length"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    /// <summary>
    /// The picker's buttons stay on screen however long the list is, and a
    /// coach who hides everything with nothing of his own is never stranded:
    /// the deck points to the library, and the library points to New play.
    /// </summary>
    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task HidingEverythingLeavesAWayForward(string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height));
        await OpenPickerAsync(page);

        var fits = await page.EvaluateAsync<bool>("""
            () => [document.querySelector('#hidesheet .sheet'), document.getElementById('hide-save'),
                   document.getElementById('hide-cancel'), document.getElementById('hideall')]
              .every(e => { const r = e.getBoundingClientRect();
                return r.top >= 0 && r.left >= 0 && r.bottom <= innerHeight + 1 && r.right <= innerWidth + 1; })
            """);
        Assert.True(fits, $"{label}: the picker or its buttons run off the screen");
        var row = (await page.Locator(".hrow").First.BoundingBoxAsync())!;
        Assert.True(row.Height >= 44, $"{label}: a row is only {row.Height}px tall");

        await page.ClickAsync("#hideall");
        Assert.Equal($"0 of {PlayLibrary.All.Count} shown", (await page.InnerTextAsync("#hidecount")).Trim(), ignoreCase: true);
        await page.ClickAsync("#hide-save");
        await page.ClickAsync("#hide-save");
        await page.WaitForSelectorAsync("#hidesheet", new() { State = WaitForSelectorState.Detached });
        Assert.Equal(PlayLibrary.All.Count, await HiddenAsync(page));
        Assert.Equal(0, await page.EvaluateAsync<int>("deck.length"));

        await page.ClickAsync("#sdone");
        await page.WaitForSelectorAsync("#dlib");
        await page.ClickAsync("#dlib");
        await page.WaitForSelectorAsync("#lnone");
        Assert.Equal(0, await page.Locator(".lrow").CountAsync());
        Assert.Equal(0, await page.Locator("#packs .pack[data-pack]:not(.empty)").CountAsync());
        await page.ClickAsync("#lnonew");
        await page.WaitForSelectorAsync(".fmbtn");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, $"{label}: " + string.Join(Environment.NewLine, errors));
    }

    /// <summary>
    /// Hidden is per format like everything else a coach chooses; the sync
    /// file carries it and trusts it no more than the rest, and Start over
    /// shows every play again.
    /// </summary>
    [Fact]
    public async Task HiddenIsPerFormatSyncsAndStartOverShowsEverything()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        var r = await page.EvaluateAsync<int[]>("""
            (() => {
              setHidden(["p_03", "p_04"]);
              loadMode(5); const five = hidden.size;
              loadMode(6); const six = hidden.size;
              const file = JSON.parse(setupBlob());
              resetHidden();
              const clean = readSetup(JSON.stringify(file));
              applySetup(clean);
              const landed = hidden.size;
              // a hidden play named in the deck or the hidden list twice is not believed
              file.modes[6].deck.push("p_03");
              file.modes[6].hidden.push("p_03", "f_01", "nope");
              const bad = readSetup(JSON.stringify(file));
              resetEverything();
              loadMode(6);
              return [five, six, clean.dropped, landed, bad.dropped, hidden.size];
            })()
            """);
        Assert.Equal(0, r[0]);
        Assert.Equal(2, r[1]);
        Assert.Equal(0, r[2]);
        Assert.Equal(2, r[3]);
        Assert.Equal(4, r[4]);
        Assert.Equal(0, r[5]);

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
}
