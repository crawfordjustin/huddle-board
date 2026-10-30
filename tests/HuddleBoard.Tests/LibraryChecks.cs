namespace HuddleBoard.Tests;

/// <summary>
/// The library screen: filters, search, the empty state, and the fact that
/// adding a play does not silently drop the filter you were using.
/// </summary>
public sealed class LibraryChecks(AppFixture app)
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    [Fact]
    public async Task FiltersAndSearchNarrowTheList()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForTimeoutAsync(500);
        Assert.Equal(0, await page.Locator("#clock").CountAsync());

        async Task<int> Rows() =>
            await page.EvaluateAsync<int>("() => document.querySelectorAll('.lrow').length");

        var all = await Rows();
        Assert.Equal(HuddleBoard.Playbook.PlayLibrary.All.Count, all);
        Assert.Equal(all, await page.Locator(".lcomplexity").CountAsync());
        Assert.Equal("SIMPLE", await page.InnerTextAsync(".lrow[data-id='p_01'] .lcomplexity"));
        Assert.Equal("ADVANCED", await page.InnerTextAsync(".lrow[data-id='p_27'] .lcomplexity"));

        await page.ClickAsync("[data-kind=\"run\"]");
        await page.WaitForTimeoutAsync(300);
        var runs = await Rows();
        Assert.InRange(runs, 1, all - 1);
        Assert.True(
            await page.EvalOnSelectorAllAsync<bool>(".lkind", "e=>e.every(x=>x.textContent=='Run')"),
            "the Run filter let a non-run through");

        await page.ClickAsync("[data-kind=\"pass\"]");
        await page.WaitForTimeoutAsync(300);
        Assert.InRange(await Rows(), 1, all - 1);

        await page.ClickAsync("[data-kind=\"all\"]");
        await page.ClickAsync("[data-cat=\"GOAL LINE\"]");
        await page.WaitForTimeoutAsync(300);
        var goalLine = await Rows();
        Assert.InRange(goalLine, 1, all - 1);

        await page.ClickAsync("[data-cat=\"all\"]");
        await page.WaitForTimeoutAsync(200);
        await page.FillAsync("#lq", "wheel");
        await page.WaitForTimeoutAsync(350);
        Assert.InRange(await Rows(), 1, all - 1);
        Assert.True(await page.EvaluateAsync<bool>("document.activeElement.id === 'lq'"),
            "typing in the search box lost focus, so the next keystroke goes nowhere");

        await page.FillAsync("#lq", "zzzz");
        await page.WaitForTimeoutAsync(350);
        Assert.Equal(0, await Rows());
        Assert.Equal(1, await page.Locator(".lempty").CountAsync());

        await page.ClickAsync("#lclear");
        await page.WaitForTimeoutAsync(350);
        Assert.Equal(all, await Rows());

        await page.ClickAsync("#lonly");
        await page.WaitForTimeoutAsync(300);
        Assert.InRange(await Rows(), 1, all - 1);

        // toggling a play while filtered must not drop the filter
        await page.ClickAsync("#lonly");
        await page.ClickAsync("[data-cat=\"RUN ZONE\"]");
        await page.WaitForTimeoutAsync(300);
        var before = await Rows();
        await page.EvalOnSelectorAsync(".lrow", "e=>e.click()");
        await page.WaitForTimeoutAsync(350);
        Assert.Equal(before, await Rows());
        Assert.True(
            await page.EvalOnSelectorAsync<bool>("[data-cat=\"RUN ZONE\"]",
                "e=>e.classList.contains('on')"),
            "adding a play while filtered dropped the filter");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A hundred plays is well past where the library sits today, and it is the
    /// size that found the <c>min-height: 0</c> bug that silently capped the
    /// list at about twenty-two.
    /// </summary>
    [Fact]
    public async Task TheLibraryStillWorksAtAHundredPlays()
    {
        var (page, errors) = await app.OpenAppAsync(Desk, settle: 300);
        await AppFixture.InjectPlaysAsync(page, 100);
        await page.EvaluateAsync("renderLibrary()");
        await page.WaitForTimeoutAsync(600);

        var rows = await page.EvaluateAsync<int>("() => document.querySelectorAll('.lrow').length");
        Assert.Equal(100, rows);

        await page.ClickAsync("[data-cat=\"GOAL LINE\"]");
        await page.WaitForTimeoutAsync(400);
        Assert.InRange(await page.EvaluateAsync<int>("() => document.querySelectorAll('.lrow').length"),
            1, 99);

        await page.ClickAsync("[data-cat=\"all\"]");
        await page.WaitForTimeoutAsync(300);
        var ms = await page.EvaluateAsync<double>("""
            () => {const t=performance.now();
                   document.querySelector('[data-kind="pass"]').click();
                   return performance.now()-t;}
            """);
        Assert.True(ms < 400, $"one filter tap took {ms:F0} ms at 100 plays");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// The thumbnail on a library row opens the play large, and only the
    /// thumbnail does: the preview is several times the size of the glance,
    /// all of it on screen at every size, it changes nothing about the deck,
    /// and it goes away on Close, on Escape and on a tap outside. The rest of
    /// the row still adds and removes the play.
    /// </summary>
    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task TheThumbnailOpensTheCardLargeAndNothingElseDoes(string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height));
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForSelectorAsync(".lrow");
        await page.WaitForTimeoutAsync(300);

        const string Row = ".lrow[data-id='p_01']";
        const string Deck = "deck.join(',')";
        var before = await page.EvaluateAsync<string>(Deck);
        var small = (await page.Locator(Row + " .lthumb .thumbsvg").BoundingBoxAsync())!.Width;

        await page.ClickAsync(Row + " .lthumb");
        await page.WaitForSelectorAsync("#prevsheet");
        Assert.Equal(before, await page.EvaluateAsync<string>(Deck));
        Assert.Contains(await page.EvaluateAsync<string>("nameOf(DATA.plays.find(p => p.id === 'p_01')).big"),
            await page.InnerTextAsync("#prevtitle"));

        var big = (await page.Locator("#prevsheet .thumbsvg").BoundingBoxAsync())!;
        Assert.True(big.Width >= small * 3, $"{label}: the preview is {big.Width:0}px wide against a {small:0}px thumbnail");
        var fits = await page.EvaluateAsync<bool>("""
            () => { const r = document.querySelector('#prevsheet .sheet').getBoundingClientRect();
                    const s = document.querySelector('#prevsheet .sheet');
                    return r.top >= 0 && r.left >= 0 && r.bottom <= innerHeight && r.right <= innerWidth
                        && s.scrollHeight <= s.clientHeight + 1; }
            """);
        Assert.True(fits, $"{label}: the preview does not fit on screen");

        await page.ClickAsync("#prev-close");
        Assert.Equal(0, await page.Locator("#prevsheet").CountAsync());

        await page.ClickAsync(Row + " .lthumb");
        await page.Keyboard.PressAsync("Escape");
        Assert.Equal(0, await page.Locator("#prevsheet").CountAsync());

        await page.ClickAsync(Row + " .lthumb");
        await page.Mouse.ClickAsync(4, 4);
        Assert.Equal(0, await page.Locator("#prevsheet").CountAsync());
        Assert.Equal(before, await page.EvaluateAsync<string>(Deck));

        // the name, not the picture: still the row's own tap, which takes it out of the deck
        await page.ClickAsync(Row + " .lname");
        await page.WaitForTimeoutAsync(200);
        Assert.False(await page.EvaluateAsync<bool>("deck.includes('p_01')"), "the row stopped toggling the deck");
        Assert.Equal(0, await page.Locator("#prevsheet").CountAsync());

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }
}
