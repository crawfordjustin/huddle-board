using HuddleBoard.Playbook;
using Microsoft.Playwright;

namespace HuddleBoard.Tests;

public sealed class PageSizeChecks(AppFixture app)
{
    public static TheoryData<bool, string, int, int> LayoutCases
    {
        get
        {
            var cases = new TheoryData<bool, string, int, int>();
            foreach (var offline in new[] { false, true })
            foreach (var s in AppFixture.Sizes)
                cases.Add(offline, s.Label, s.Width, s.Height);
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(LayoutCases))]
    public async Task BothScreensFitEveryPageSizeAndFormat(bool offline, string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height));
        if (offline)
        {
            await page.RouteAsync("https://fonts.googleapis.com/**", r => r.AbortAsync());
            await page.ReloadAsync();
            await page.ClickAsync("#start");
        }
        foreach (var players in new[] { 5, 6, 7 })
        foreach (var size in new[] { 4, 6, 8 })
        {
            await page.EvaluateAsync("""
                ([players, size]) => {
                  loadMode(players); cfg.deckPageSize = cfg.libraryPageSize = size;
                  deck = DATA.plays.map(p => p.id); renderDeck();
                }
                """, new[] { players, size });
            Assert.True(await page.EvaluateAsync<bool>("""
                () => {
                  const pages = [...document.querySelectorAll('.tiles')];
                  return pages.length > 0 && pages.every(g => {
                  const tiles = [...g.querySelectorAll('.tile')];
                  const r = g.getBoundingClientRect();
                  return tiles.length > 0 && tiles.length <= cfg.deckPageSize && tiles.every(t => {
                    const b = t.getBoundingClientRect();
                    const wideEnough = cfg.deckPageSize !== 4 || b.width > innerWidth * (innerHeight > innerWidth ? .8 : .4);
                    return wideEnough && !tileSpills(t) && b.top >= r.top - 1 && b.bottom <= r.bottom + 1;
                  });
                  });
                }
                """), $"{label}, {players}, {size}: deck clipping");
            await page.EvaluateAsync("renderLibrary()");
            Assert.True(await page.EvaluateAsync<bool>("""
                () => {
                  const pages = [...document.querySelectorAll('.libpages > .lib')];
                  return pages.length > 0 && pages.every(g => {
                  const rows = [...g.querySelectorAll('.lrow')];
                  const r = g.getBoundingClientRect();
                  return rows.length > 0 && rows.length <= cfg.libraryPageSize && rows.every(t => {
                    const b = t.getBoundingClientRect(), name = t.querySelector('.lname b');
                    const pieces = [...t.querySelectorAll('.lname,.lthumb,.lfoot,.lren,.lmark,.lmeta > *')];
                    const inside = pieces.every(e => {
                      const q = e.getBoundingClientRect();
                      return q.top >= b.top - 1 && q.bottom <= b.bottom + 1
                        && q.left >= b.left - 1 && q.right <= b.right + 1;
                    });
                    const controls = [...t.querySelectorAll('.lren,.lmark')];
                    const thumb = t.querySelector('.thumbsvg').getBoundingClientRect();
                    const drawnWidth = Math.min(thumb.width, thumb.height * TH.w / TH.h);
                    return b.height >= 44 && b.top >= r.top - 1 && b.bottom <= r.bottom + 1
                      && name.scrollWidth <= name.clientWidth + 1 && lineCount(name) <= 2 && inside
                      && controls.length === 2 && controls.every(c => c.clientWidth >= 42 && c.clientHeight >= 42)
                      && drawnWidth > 0 && (cfg.libraryPageSize !== 6 || drawnWidth >= b.width * .4);
                  });
                  });
                }
                """), $"{label}, {players}, {size}: library clipping");
        }
        await page.CloseAsync();
        Assert.Empty(errors);
    }

    [Fact]
    public async Task PageSizesPersistSyncValidateAndReset()
    {
        using var site = new StaticSite(Workspace.Deploy);
        await using var context = await app.Browser.NewContextAsync();
        await AppFixture.MarkTourSeenAsync(context);
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);
        await page.GotoAsync(site.Origin + "/index.html");
        await page.ClickAsync("#start");
        await page.EvaluateAsync("renderSettings('game')");
        await page.ClickAsync("[data-key=deckPageSize] [data-v='4']");
        await page.ClickAsync("[data-key=libraryPageSize] [data-v='8']");
        await page.ReloadAsync();
        await page.ClickAsync("#start");
        Assert.Equal(new[] { 4, 8 }, await page.EvaluateAsync<int[]>("[cfg.deckPageSize, cfg.libraryPageSize]"));
        await page.EvaluateAsync("loadMode(5); renderSettings('game')");
        Assert.Equal("4", await page.InnerTextAsync("[data-key=deckPageSize] .on"));
        Assert.Equal("8", await page.InnerTextAsync("[data-key=libraryPageSize] .on"));
        var exported = await page.EvaluateAsync<string>("setupBlob()");
        await page.EvaluateAsync("cfg.deckPageSize = 8; cfg.libraryPageSize = 4");
        await page.EvaluateAsync("text => applySetup(readSetup(text))", exported);
        Assert.Equal(new[] { 4, 8 }, await page.EvaluateAsync<int[]>("[cfg.deckPageSize, cfg.libraryPageSize]"));
        Assert.True(await page.EvaluateAsync<bool>("""
            text => {
              const old = JSON.parse(text);
              delete old.settings.deckPageSize; delete old.settings.libraryPageSize;
              const clean = readSetup(JSON.stringify(old)); applySetup(clean);
              if (clean.dropped || cfg.deckPageSize !== 6 || cfg.libraryPageSize !== 6) return false;
              old.settings.deckPageSize = 0; old.settings.libraryPageSize = '8';
              const bad = readSetup(JSON.stringify(old)); applySetup(bad);
              return bad.dropped === 2 && cfg.deckPageSize === 6 && cfg.libraryPageSize === 6;
            }
            """, exported));
        await page.EvaluateAsync("cfg.deckPageSize = 4; cfg.libraryPageSize = 8; resetEverything()");
        Assert.Equal(new[] { 6, 6 }, await page.EvaluateAsync<int[]>("[cfg.deckPageSize, cfg.libraryPageSize]"));
        // Local storage is also untrusted: invalid counts cannot create a loop or an empty page.
        await page.EvaluateAsync("localStorage.setItem('hb.settings', JSON.stringify({v:1, deckPageSize:0, libraryPageSize:'8'}))");
        await page.ReloadAsync();
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync("#deckpages");
        Assert.Equal(new[] { 6, 6 }, await page.EvaluateAsync<int[]>("[cfg.deckPageSize, cfg.libraryPageSize]"));
        await page.CloseAsync();
        Assert.Empty(errors);
    }
}
