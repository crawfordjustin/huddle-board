using HuddleBoard.Playbook;
using Microsoft.Playwright;

namespace HuddleBoard.Tests;

public sealed class DeckOrderChecks(AppFixture app) : IDisposable
{
    private readonly StaticSite _site = new(Workspace.Deploy);
    public void Dispose() => _site.Dispose();

    private async Task<(IPage Page, List<string> Errors)> OpenAsync(int width, int height)
    {
        var page = await app.Browser.NewPageAsync(new() { ViewportSize = new() { Width = width, Height = height } });
        await AppFixture.MarkTourSeenAsync(page);
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);
        await page.GotoAsync(_site.Url);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        return (page, errors);
    }

    private static async Task OrderAsync(IPage page)
    {
        await page.ClickAsync("#ham");
        await page.ClickAsync("#order");
        await page.WaitForSelectorAsync("#ordersheet");
    }

    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task WholeDeckCanBeOrderedAtEverySizeAndFormat(string label, int width, int height)
    {
        var (page, errors) = await OpenAsync(width, height);
        foreach (var players in new[] { 5, 6, 7 })
        {
            await page.EvaluateAsync("""
                n => { cfg.players = n; saveCfg(); loadMode(n);
                  deck = DATA.plays.slice(0, 14).map(p => p.id); saveDeck();
                  deckF = {kind:'run', cat:'all'}; renderDeck(); }
                """, players);
            var before = await page.EvaluateAsync<string[]>("deck");
            Assert.Equal(14, before.Length);
            await OrderAsync(page);
            Assert.Equal(before, await page.Locator(".orderrow").EvaluateAllAsync<string[]>("rs => rs.map(r => r.dataset.id)"));
            Assert.True(await page.Locator(".orderrow").First.Locator("[data-move='-1']").IsDisabledAsync());
            Assert.True(await page.Locator(".orderrow").Last.Locator("[data-move='1']").IsDisabledAsync());
            await page.Locator(".orderrow").First.Locator("[data-move='1']").ClickAsync();
            Assert.Equal(before, await page.EvaluateAsync<string[]>("deck"));
            await page.ClickAsync("#order-cancel");
            Assert.Equal(before, await page.EvaluateAsync<string[]>("deck"));

            await OrderAsync(page);
            // Move across a page boundary and bring a play from the end of the scroll list.
            await page.Locator(".orderrow").Nth(5).Locator("[data-move='1']").ClickAsync();
            await page.Locator(".orderrow").Last.Locator("[data-move='-1']").ClickAsync();
            Assert.True(await page.EvaluateAsync<bool>("""
                () => {
                  const w = document.querySelector('#ordersheet');
                  const list = w.querySelector('.orderlist');
                  const rect = list.getBoundingClientRect();
                  const row = list.querySelectorAll('.orderrow')[12].getBoundingClientRect();
                  const controls = [...w.querySelectorAll('.sheetbtns button'),
                    ...list.querySelectorAll('.orderrow:nth-child(13) button')];
                  return row.top >= rect.top - 1 && row.bottom <= rect.bottom + 1 && controls.every(b => {
                    const r = b.getBoundingClientRect();
                    return r.height >= 44 && r.width >= 44 && r.left >= 0 && r.right <= innerWidth
                      && r.top >= 0 && r.bottom <= innerHeight;
                  });
                }
                """), $"{label}, {players}: ordering controls are clipped or too small");
            var expected = before.ToArray();
            (expected[5], expected[6]) = (expected[6], expected[5]);
            (expected[12], expected[13]) = (expected[13], expected[12]);
            await page.ClickAsync("#order-save");
            Assert.Equal(expected, await page.EvaluateAsync<string[]>("deck"));
            Assert.Equal(expected, await page.EvaluateAsync<string[]>("JSON.parse(localStorage.getItem(myKey('hb.deck')))"));
            Assert.Equal(await page.EvaluateAsync<string[]>("inDeck().filter(p => deckMatch(p, deckF)).map(p => p.id)"),
                await page.Locator(".tile").EvaluateAllAsync<string[]>("ts => ts.map(t => t.dataset.id)"));
            await page.ReloadAsync();
            await page.ClickAsync("#start");
            Assert.Equal(expected, await page.Locator(".tile").EvaluateAllAsync<string[]>("ts => ts.map(t => t.dataset.id)"));
        }
        await page.CloseAsync();
        Assert.Empty(errors);
    }

    [Fact]
    public async Task OrderTravelsWithWeeksPrintAndSyncAndCancelDiscardsEdits()
    {
        var (page, errors) = await OpenAsync(1024, 600);
        var original = await page.EvaluateAsync<string[]>("deck");
        await OrderAsync(page);
        await page.Locator(".orderrow").First.Locator("[data-move='1']").ClickAsync();
        await page.Keyboard.PressAsync("Escape");
        Assert.Equal(0, await page.Locator("#ordersheet").CountAsync());
        Assert.Equal(original, await page.EvaluateAsync<string[]>("deck"));
        await OrderAsync(page);
        await page.Locator(".orderrow").First.Locator("[data-move='1']").ClickAsync();
        await page.ClickAsync("#order-save");
        var expected = original.ToArray();
        (expected[0], expected[1]) = (expected[1], expected[0]);
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        Assert.Equal(0, await page.Locator("#packs .pack.on").CountAsync());
        await page.ClickAsync("#psave");
        await page.ClickAsync("#pack-week1");
        Assert.True(await page.EvaluateAsync<bool>("!!packSaves.week1"));
        Assert.Equal(expected, await page.EvaluateAsync<string[]>("packSaves.week1"));
        await page.EvaluateAsync("deck = DATA.defaultDeck.slice(); saveDeck(); renderLibrary()");
        await page.ClickAsync("#pack-week1");
        await page.ClickAsync("#pack-week1");
        Assert.Equal(expected, await page.EvaluateAsync<string[]>("deck"));
        await page.EvaluateAsync("window.print = () => {}; printSheet()");
        Assert.Equal(await page.EvaluateAsync<string[]>("deck.map(id => coachNameOf(DATA.plays.find(p => p.id === id)))"),
            await page.Locator(".pcell .pname b").AllTextContentsAsync());
        await page.EvaluateAsync("document.getElementById('printsheet').remove()");
        var exported = await page.EvaluateAsync<string>("setupBlob()");
        var (other, otherErrors) = await OpenAsync(1024, 600);
        await other.EvaluateAsync("text => { applySetup(readSetup(text)); renderDeck(); }", exported);
        Assert.Equal(expected, await other.EvaluateAsync<string[]>("deck"));
        Assert.Equal(expected, await other.Locator(".tile").EvaluateAllAsync<string[]>("ts => ts.map(t => t.dataset.id)"));
        Assert.Equal(expected, await other.EvaluateAsync<string[]>("packSaves.week1"));
        await page.EvaluateAsync("deck = []; renderDeck()");
        Assert.True(await page.Locator("#order").IsDisabledAsync());
        await page.EvaluateAsync("deck = DATA.defaultDeck.slice(0, 1); renderDeck()");
        Assert.True(await page.Locator("#order").IsDisabledAsync());
        await page.CloseAsync();
        await other.CloseAsync();
        Assert.Empty(errors);
        Assert.Empty(otherErrors);
    }
}
