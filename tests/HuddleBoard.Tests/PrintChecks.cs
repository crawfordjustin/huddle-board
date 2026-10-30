using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// The deck on paper: Print on the Change plays screen puts every play in the
/// deck on one quick-reference sheet, in deck order, as the tile's own picture
/// with both names.
/// </summary>
/// <remarks>
/// The print dialog is the browser's and headless Chromium has none, so
/// <c>print()</c> is stubbed and the sheet is read under print media and as a
/// real PDF — which is the only way to know a 4x4 grid actually fits one page.
/// </remarks>
public sealed partial class PrintChecks(AppFixture app)
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    private static async Task PrintAsync(IPage page)
    {
        await page.EvaluateAsync("() => { window.print = () => { window.__printed = (window.__printed || 0) + 1; }; }");
        await page.EvaluateAsync("renderLibrary()");
        await page.WaitForSelectorAsync("#lprint");
        await page.ClickAsync("#lprint");
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });
    }

    private static async Task<int> PdfPagesAsync(IPage page)
    {
        var pdf = await page.PdfAsync(new PagePdfOptions { PreferCSSPageSize = true, PrintBackground = true });
        return PageObject().Matches(Encoding.Latin1.GetString(pdf)).Count;
    }

    [GeneratedRegex(@"/Type\s*/Page\b(?!s)")]
    private static partial Regex PageObject();

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public async Task TheSheetIsTheDeckInOrderAndNothingElse(int players)
    {
        var (page, errors) = await app.OpenAppAsync(Desk, players: players);
        // reversed, so deck order and library order cannot agree by accident
        await page.EvaluateAsync("deck = DATA.plays.slice(0, 5).map(p => p.id).reverse(); saveDeck();");
        await PrintAsync(page);

        Assert.Equal(1, await page.EvaluateAsync<int>("window.__printed"));
        Assert.Equal(
            await page.EvaluateAsync<string[]>("deck.map(id => coachNameOf(DATA.plays.find(p => p.id === id)))"),
            (await page.Locator("#printsheet .pname b").AllInnerTextsAsync()).Select(t => t.Trim()).ToArray());
        Assert.Equal(5, await page.Locator("#printsheet .pcell .thumbsvg").CountAsync());
        Assert.Equal(1, await page.Locator("#printsheet .pgrid.g3").CountAsync());

        // the kids' name is there under the coach's, and so are both tags
        var kid = await page.EvaluateAsync<string>("kidNameOf(DATA.plays.find(p => p.id === deck[0]))");
        Assert.Contains(kid, await page.Locator("#printsheet .psub").First.InnerTextAsync());
        Assert.Equal(2, await page.Locator("#printsheet .pcell").First.Locator(".ptag").CountAsync());

        // in print, the sheet is the only thing on the page
        Assert.Equal(
            "none",
            await page.EvaluateAsync<string>("getComputedStyle(document.getElementById('app')).display"));
        Assert.True(await page.Locator("#printsheet").IsVisibleAsync());
        Assert.Equal(1, await PdfPagesAsync(page));
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(4, "g2", 1)]
    [InlineData(9, "g3", 1)]
    [InlineData(16, "g4", 1)]
    [InlineData(17, "g4", 2)]
    public async Task TheGridGrowsWithTheDeckAndRunsOnToAnotherPage(int plays, string grid, int pages)
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await AppFixture.InjectPlaysAsync(page, 40);
        await page.EvaluateAsync($"deck = DATA.plays.slice(0, {plays}).map(p => p.id); saveDeck();");
        await PrintAsync(page);

        Assert.Equal(plays, await page.Locator("#printsheet .pcell").CountAsync());
        Assert.Equal(pages, await page.Locator($"#printsheet .pgrid.{grid}").CountAsync());
        Assert.Equal(pages, await PdfPagesAsync(page));

        // every cell keeps its picture and its name inside its own box
        var spill = await page.EvaluateAsync<string[]>("""
            () => [...document.querySelectorAll('#printsheet .pcell')].filter(c => {
              const svg = c.querySelector('.thumbsvg').getBoundingClientRect();
              return svg.height < 20 || c.scrollHeight > c.clientHeight + 1;
            }).map(c => c.querySelector('.pname b').textContent)
            """);
        Assert.Empty(spill);
        Assert.Empty(errors);
    }

    [Fact]
    public async Task RenamesAndDrawnPlaysPrintAsTheyAreAndMarkupDoesNot()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.EvaluateAsync("""
            () => {
              names = {}; setNames(deck[0], '<b>X</b> & CO', '<i>KIDS</i>');
              const spots = {};
              for (const k of Object.keys(FORMATIONS.SPREAD))
                spots[k] = [FORMATIONS.SPREAD[k].x, FORMATIONS.SPREAD[k].y];
              const d = saneDesign({id: 'u_print01', base: 'SPREAD', spots,
                routes: {Y: {pts: [[-5, 5], [-1, 5]]}}, ball: {mode: 'pass', who: 'Y'},
                coachName: 'SLOT IN', created: 1});
              designs[d.id] = d; saveDesigns(); refreshPlays();
              deck.push(d.id); saveDeck();
            }
            """);
        await PrintAsync(page);

        var first = page.Locator("#printsheet .pcell").First;
        Assert.Equal("<b>X</b> & CO", (await first.Locator(".pname b").TextContentAsync())!.Trim());
        Assert.Contains("<i>KIDS</i>", (await first.Locator(".psub").TextContentAsync())!);
        Assert.Equal(0, await page.Locator("#printsheet .pname b b, #printsheet .psub i").CountAsync());

        var last = page.Locator("#printsheet .pcell").Last;
        Assert.Equal("SLOT IN", (await last.Locator(".pname b").TextContentAsync())!.Trim());
        Assert.Equal(1, await last.Locator(".pstar").CountAsync());
        Assert.Empty(errors);
    }

    [Fact]
    public async Task AnEmptyDeckHasNothingToPrint()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.EvaluateAsync("deck = []; saveDeck(); renderLibrary();");
        await page.WaitForSelectorAsync("#lprint");
        Assert.True(await page.Locator("#lprint").IsDisabledAsync());

        // adding a play from the library turns it on
        await page.Locator(".lrow").First.Locator(".lname").ClickAsync();
        await page.WaitForTimeoutAsync(200);
        Assert.False(await page.Locator("#lprint").IsDisabledAsync());
        Assert.Empty(errors);
    }
}
