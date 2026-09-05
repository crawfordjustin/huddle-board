using HuddleBoard.Playbook;

using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// Play packs: the saved decks a coach starts from, chosen by name on the
/// Change plays screen. Taking one replaces the deck, so it takes two taps.
/// </summary>
/// <remarks>
/// The things that matter here are the same as for a sync import: that a pack
/// lands exactly, that the tablet says which pack it is on, and that a stray
/// thumb on the way past costs nothing.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class PackChecks(AppFixture app)
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    private static readonly PlayPack Week3 = PlayPacks.All.Single(k => k.Id == "week3");

    private static string Ids(IEnumerable<int> nums) =>
        string.Join(",", nums.Select(n => $"p_{n:00}").Order(StringComparer.Ordinal));

    private const string DeckProbe = "() => deck.slice().sort().join(',')";

    private static async Task OpenLibraryAsync(IPage page)
    {
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForSelectorAsync("#packs .pack");
        await page.WaitForTimeoutAsync(400);
    }

    [Fact]
    public async Task TheTabletArrivesOnWeekOne()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await OpenLibraryAsync(page);

        Assert.Equal(PlayPacks.All.Count, await page.Locator("#packs .pack[data-pack]").CountAsync());
        Assert.Equal(PlayPacks.All.Select(k => k.Name.ToUpperInvariant()).ToList(),
            (await page.Locator("#packs .pack[data-pack]").AllInnerTextsAsync())
                .Select(t => t.Split('\n')[0].Trim().ToUpperInvariant()).ToList());

        // the shipped deck IS the first pack, and the tablet says so
        Assert.Equal(Ids(PlayPacks.Starting), await page.EvaluateAsync<string>(DeckProbe));
        Assert.Equal(["pack-week1"], await page.Locator("#packs .pack.on").EvaluateAllAsync<string[]>("e => e.map(b => b.id)"));
        Assert.Contains(PlayPacks.All[0].Blurb, await page.InnerTextAsync("#lnote"));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task APackTakesTwoTapsAndThenReplacesTheDeckExactly()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.EvaluateAsync("deck = DATA.plays.slice(0, 9).map(p => p.id); saveDeck();");
        await OpenLibraryAsync(page);
        var before = await page.EvaluateAsync<string>(DeckProbe);
        Assert.Equal(0, await page.Locator("#packs .pack.on").CountAsync());

        // first tap arms and changes nothing
        await page.ClickAsync("#pack-week3");
        await page.WaitForTimeoutAsync(150);
        Assert.Equal("SURE?", (await page.InnerTextAsync("#pack-week3")).Trim().ToUpperInvariant());
        Assert.Equal(before, await page.EvaluateAsync<string>(DeckProbe));

        // second tap takes the pack: the deck, the rows, the count, the note, storage
        await page.ClickAsync("#pack-week3");
        await page.WaitForTimeoutAsync(400);
        var want = Ids(Week3.Plays);
        Assert.Equal(want, await page.EvaluateAsync<string>(DeckProbe));
        Assert.Equal(want, await page.EvaluateAsync<string>(
            "() => JSON.parse(localStorage.getItem('hb.deck')).sort().join(',')"));
        Assert.Equal(want, string.Join(",",
            (await page.Locator(".lrow.on").EvaluateAllAsync<string[]>("e => e.map(b => b.dataset.id)"))
                .Order(StringComparer.Ordinal)));
        Assert.Equal($"{Week3.Plays.Count} IN YOUR DECK", (await page.InnerTextAsync("#lcount")).Trim().ToUpperInvariant());
        Assert.Equal(["pack-week3"], await page.Locator("#packs .pack.on").EvaluateAllAsync<string[]>("e => e.map(b => b.id)"));
        Assert.Contains(Week3.Blurb, await page.InnerTextAsync("#lnote"));

        // one play off the pack and it is no longer that pack
        var stray = PlayLibrary.All.First(p => !Week3.Plays.Contains(p.Num));
        await page.ClickAsync($".lrow[data-id=\"p_{stray.Num:00}\"]");
        await page.WaitForTimeoutAsync(300);
        Assert.Equal(0, await page.Locator("#packs .pack.on").CountAsync());
        Assert.DoesNotContain(Week3.Blurb, await page.InnerTextAsync("#lnote"));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task AnArmedPackLapsesOnItsOwn()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await OpenLibraryAsync(page);
        var before = await page.EvaluateAsync<string>(DeckProbe);

        await page.ClickAsync("#pack-week3");
        await page.WaitForTimeoutAsync(2800);
        var label = (await page.InnerTextAsync("#pack-week3")).Trim();
        Assert.StartsWith(Week3.Name, label, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await page.Locator("#packs .pack[data-armed]").CountAsync());
        Assert.Equal(before, await page.EvaluateAsync<string>(DeckProbe));
        Assert.Empty(errors);
    }

    /// <summary>
    /// Save deck, then the week: the coach's deck goes into that slot, the chip
    /// wears the pencil, taking the week back gives the saved deck, and Start
    /// over puts the shipped week back.
    /// </summary>
    [Fact]
    public async Task ADeckCanBeSavedIntoAWeekAndTakenBackOut()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.EvaluateAsync("deck = DATA.plays.slice(5, 10).map(p => p.id); saveDeck();");
        await OpenLibraryAsync(page);
        var mine = await page.EvaluateAsync<string>(DeckProbe);
        Assert.Equal(0, await page.Locator("#packs .pack.mine").CountAsync());

        // Save deck asks which week, and says so
        await page.ClickAsync("#psave");
        await page.WaitForTimeoutAsync(150);
        Assert.Equal("SAVE INTO", (await page.InnerTextAsync("#packs .plab")).Trim().ToUpperInvariant());
        Assert.Equal(1, await page.Locator("#packs.saving").CountAsync());

        // the week takes the deck; nothing about the deck itself changes
        await page.ClickAsync("#pack-week4");
        await page.WaitForTimeoutAsync(300);
        Assert.Equal(mine, await page.EvaluateAsync<string>(DeckProbe));
        Assert.Equal(0, await page.Locator("#packs .plab").CountAsync());    // at rest the strip has no label
        Assert.Equal(["pack-week4"], await page.Locator("#packs .pack.mine").EvaluateAllAsync<string[]>("e => e.map(b => b.id)"));
        Assert.Equal(["pack-week4"], await page.Locator("#packs .pack.on").EvaluateAllAsync<string[]>("e => e.map(b => b.id)"));
        Assert.Contains("Your own deck", await page.InnerTextAsync("#lnote"));
        Assert.Equal(mine, await page.EvaluateAsync<string>(
            "() => JSON.parse(localStorage.getItem('hb.packs')).byId.week4.slice().sort().join(',')"));

        // take another week, then take the saved one back: two taps, the saved deck lands
        await page.ClickAsync("#pack-week1");
        await page.ClickAsync("#pack-week1");
        await page.WaitForTimeoutAsync(300);
        Assert.NotEqual(mine, await page.EvaluateAsync<string>(DeckProbe));
        await page.ClickAsync("#pack-week4");
        await page.ClickAsync("#pack-week4");
        await page.WaitForTimeoutAsync(300);
        Assert.Equal(mine, await page.EvaluateAsync<string>(DeckProbe));

        // saving the shipped deck back into its own slot is not a save
        await page.ClickAsync("#pack-week1");
        await page.ClickAsync("#pack-week1");
        await page.WaitForTimeoutAsync(300);
        await page.ClickAsync("#psave");
        await page.ClickAsync("#pack-week1");
        await page.WaitForTimeoutAsync(300);
        Assert.Equal(1, await page.EvaluateAsync<int>("savedPackCount()"));

        // Start over puts the shipped week back
        await page.EvaluateAsync("resetEverything(); renderLibrary();");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(0, await page.EvaluateAsync<int>("savedPackCount()"));
        Assert.Equal(0, await page.Locator("#packs .pack.mine").CountAsync());
        Assert.Equal(Ids(PlayPacks.All.Single(k => k.Id == "week4").Plays),
            await page.EvaluateAsync<string>("() => packsNow().find(k => k.id === 'week4').plays.slice().sort().join(',')"));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task SaveDeckLapsesOnItsOwn()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await OpenLibraryAsync(page);

        await page.ClickAsync("#psave");
        await page.WaitForTimeoutAsync(6500);
        Assert.Equal(0, await page.Locator("#packs .plab").CountAsync());
        Assert.Equal(0, await page.Locator("#packs.saving").CountAsync());
        Assert.Equal(0, await page.EvaluateAsync<int>("savedPackCount()"));
        Assert.Empty(errors);
    }

    /// <summary>
    /// Rename, then the week: the coach's name goes on the chip and in the note,
    /// reaches storage, retyping the shipped name is not a rename, Reset in the
    /// sheet puts the shipped name back, and Start over does too. The deck in
    /// the slot is untouched throughout.
    /// </summary>
    [Fact]
    public async Task AWeekCanBeRenamedAndTheNameOutlivesTheDeck()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await OpenLibraryAsync(page);
        var before = await page.EvaluateAsync<string>(DeckProbe);
        Assert.Equal(0, await page.Locator("#packs .plab").CountAsync());

        // Rename asks which week, and says so
        await page.ClickAsync("#prename");
        await page.WaitForTimeoutAsync(150);
        Assert.Equal("RENAME", (await page.InnerTextAsync("#packs .plab")).Trim().ToUpperInvariant());
        Assert.Equal(1, await page.Locator("#packs.renaming").CountAsync());
        Assert.Equal("CANCEL", (await page.InnerTextAsync("#prename")).Trim().ToUpperInvariant());

        // the week opens the sheet, and the mode is over
        await page.ClickAsync("#pack-week3");
        await page.WaitForSelectorAsync("#pnsheet");
        Assert.Equal(0, await page.Locator("#packs.renaming").CountAsync());
        Assert.Equal(Week3.Name, await page.InputValueAsync("#pn-name"));
        Assert.True(await page.Locator("#pn-reset").IsDisabledAsync(), "Reset is live on a week that is not renamed");
        await page.FillAsync("#pn-name", "  playoffs   vs  hawks ");
        await page.ClickAsync("#pn-save");
        await page.WaitForTimeoutAsync(300);

        // the chip, the storage, the count; the deck and the slot's plays are untouched
        Assert.Equal(0, await page.Locator("#pnsheet").CountAsync());
        Assert.StartsWith("PLAYOFFS VS HAWKS", (await page.InnerTextAsync("#pack-week3")).Trim().ToUpperInvariant());
        Assert.Equal("playoffs vs hawks", await page.EvaluateAsync<string>(
            "() => JSON.parse(localStorage.getItem('hb.packnames')).byId.week3"));
        Assert.Equal(1, await page.EvaluateAsync<int>("renamedPackCount()"));
        Assert.Equal(0, await page.EvaluateAsync<int>("savedPackCount()"));
        Assert.Equal(0, await page.Locator("#packs .pack.mine").CountAsync());
        Assert.Equal(before, await page.EvaluateAsync<string>(DeckProbe));

        // taking the week still takes the shipped plays, and the note wears the coach's name
        await page.ClickAsync("#pack-week3");
        await page.ClickAsync("#pack-week3");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(Ids(Week3.Plays), await page.EvaluateAsync<string>(DeckProbe));
        Assert.Contains("playoffs vs hawks", await page.InnerTextAsync("#lnote"));
        Assert.Contains(Week3.Blurb, await page.InnerTextAsync("#lnote"));

        // Setup counts it, and its Reset all is live
        await page.ClickAsync("#done");
        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await page.WaitForSelectorAsync("#pkreset");
        Assert.Contains("1 is renamed", await page.InnerTextAsync(".setrows"));
        Assert.False(await page.Locator("#pkreset").IsDisabledAsync(), "Reset all is dead while a week is renamed");
        await page.ClickAsync("#sdone");
        await page.WaitForSelectorAsync(".tile");
        await OpenLibraryAsync(page);

        // retyping the shipped name is not a rename
        await page.ClickAsync("#prename");
        await page.ClickAsync("#pack-week3");
        await page.WaitForSelectorAsync("#pnsheet");
        Assert.False(await page.Locator("#pn-reset").IsDisabledAsync());
        await page.FillAsync("#pn-name", Week3.Name.ToLowerInvariant());
        await page.PressAsync("#pn-name", "Enter");
        await page.WaitForTimeoutAsync(300);
        Assert.Equal(0, await page.EvaluateAsync<int>("renamedPackCount()"));
        Assert.StartsWith(Week3.Name, (await page.InnerTextAsync("#pack-week3")).Trim(), StringComparison.OrdinalIgnoreCase);

        // Reset in the sheet puts the shipped name back
        await page.EvaluateAsync("setPackName('week3', 'SEAHAWKS'); renderLibrary();");
        await page.WaitForTimeoutAsync(400);
        Assert.StartsWith("SEAHAWKS", (await page.InnerTextAsync("#pack-week3")).Trim().ToUpperInvariant());
        await page.ClickAsync("#prename");
        await page.ClickAsync("#pack-week3");
        await page.WaitForSelectorAsync("#pnsheet");
        await page.ClickAsync("#pn-reset");
        await page.WaitForTimeoutAsync(300);
        Assert.Equal(0, await page.EvaluateAsync<int>("renamedPackCount()"));
        Assert.StartsWith(Week3.Name, (await page.InnerTextAsync("#pack-week3")).Trim(), StringComparison.OrdinalIgnoreCase);

        // Start over puts every name back, alongside every saved deck
        await page.EvaluateAsync("setPackName('week2', 'TIGERS'); setPackName('week5', 'BYE'); resetEverything(); renderLibrary();");
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(0, await page.EvaluateAsync<int>("renamedPackCount()"));
        Assert.Equal(0, await page.EvaluateAsync<int>(
            "Object.keys(JSON.parse(localStorage.getItem('hb.packnames')).byId).length"));
        Assert.Empty(errors);
    }

    [Fact]
    public async Task RenameLapsesOnItsOwn()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await OpenLibraryAsync(page);

        await page.ClickAsync("#prename");
        await page.WaitForTimeoutAsync(6500);
        Assert.Equal(0, await page.Locator("#packs .plab").CountAsync());
        Assert.Equal(0, await page.Locator("#packs.renaming").CountAsync());
        Assert.Equal(0, await page.Locator("#pnsheet").CountAsync());
        Assert.Equal(0, await page.EvaluateAsync<int>("renamedPackCount()"));

        // and one verb cancels the other: Save deck while renaming asks the save question instead
        await page.ClickAsync("#prename");
        await page.ClickAsync("#psave");
        await page.WaitForTimeoutAsync(150);
        Assert.Equal("SAVE INTO", (await page.InnerTextAsync("#packs .plab")).Trim().ToUpperInvariant());
        Assert.Equal(0, await page.Locator("#packs.renaming").CountAsync());
        await page.ClickAsync("#psave");
        await page.WaitForTimeoutAsync(150);
        Assert.Equal(0, await page.Locator("#packs .plab").CountAsync());
        Assert.Empty(errors);
    }

    [Fact]
    public async Task APackWhosePlaysAreMissingIsNotOffered()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await AppFixture.InjectPlaysAsync(page, 30);          // s_ ids: no pack resolves
        await page.EvaluateAsync("renderLibrary()");
        await page.WaitForTimeoutAsync(400);

        Assert.Equal(0, await page.Locator("#packs .pack[data-pack]").CountAsync());
        Assert.False(await page.Locator("#packs").IsVisibleAsync());
        Assert.Empty(errors);
    }

    /// <summary>The strip is a row above the plays, at every size, and it
    /// leaves the first row of plays where a thumb can reach it.</summary>
    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task TheStripSitsAboveTheRowsAtEverySize(string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height));
        await OpenLibraryAsync(page);

        var strip = await page.Locator("#packs").BoundingBoxAsync();
        var lib = await page.Locator("#lib").BoundingBoxAsync();
        var first = await page.Locator(".lrow").First.BoundingBoxAsync();
        Assert.NotNull(strip);
        Assert.NotNull(lib);
        Assert.NotNull(first);

        foreach (var chip in await page.Locator("#packs .pack").AllAsync())
        {
            var box = await chip.BoundingBoxAsync();
            Assert.NotNull(box);
            Assert.True(box.X >= 0 && box.X + box.Width <= width, $"{label}: a pack chip is off the side");
            Assert.True(box.Y + box.Height <= lib.Y + 1, $"{label}: a pack chip overlaps the rows");
        }

        Assert.True(first.Y >= lib.Y - 1 && first.Y + first.Height <= height,
            $"{label}: the first row of plays is not on screen");
        Assert.True(lib.Height >= first.Height * 2, $"{label}: the strip left too little room for plays");
        Assert.Empty(errors);
    }
}
