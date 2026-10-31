using Microsoft.Playwright;

namespace HuddleBoard.Tests;

public sealed class LibraryPageChecks(AppFixture app)
{
    [Fact]
    public async Task FullLengthCoachNamesStillFitWithEightCardsAndTheOfflineFont()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[4]);
        await page.RouteAsync("https://fonts.googleapis.com/**", r => r.AbortAsync());
        await page.ReloadAsync();
        await page.ClickAsync("#start");
        foreach (var size in new[] { 4, 6, 8 })
        {
            await page.EvaluateAsync("""
                size => {
                  cfg.libraryPageSize = size;
                  DATA.plays.forEach((p, i) => setNames(p.id, i % 2 ? 'WWWWWWWWWWWWWWWWWWWWWW' : 'ABCDEFGHIJKLMNOPQRSTUV', 'FAVORITE PLAY'));
                  renderLibrary();
                }
                """, size);
            Assert.True(await page.EvaluateAsync<bool>("""
                () => {
                  const names = [...document.querySelectorAll('.lname b')];
                  return names.length > 0 && names.every(e => {
                    const r = e.getBoundingClientRect(), b = e.closest('.lbody').getBoundingClientRect();
                    return lineCount(e) <= 2 && e.scrollWidth <= e.clientWidth + 1
                      && r.top >= b.top - 1 && r.bottom <= b.bottom + 1;
                  });
                }
                """), $"{size} cards: a full-length coach name clipped");
        }
        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public async Task RemovingTheLastFilteredPageClampsToTheRemainingPlays()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0]);
        await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await page.EvaluateAsync("deck = DATA.plays.map(p => p.id); renderLibrary()");
        await page.ClickAsync("#lonly");
        for (var i = 1; i <= 4; i++)
        {
            await page.ClickAsync("#lnext");
            await page.WaitForFunctionAsync("i => document.querySelector('#lpage').textContent === `Page ${i + 1} of 5`", i);
        }
        Assert.Equal(3, await page.Locator(".lib:not([inert]) .lrow").CountAsync());
        for (var i = 0; i < 3; i++)
            await page.Locator(".lib:not([inert]) .lrow .lname").First.ClickAsync();
        Assert.Equal("Page 4 of 4", await page.InnerTextAsync("#lpage"));
        Assert.Equal(6, await page.Locator(".lib:not([inert]) .lrow").CountAsync());
        Assert.Equal(24, await page.EvaluateAsync<int>("deck.length"));
        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public async Task EveryPlayIsReachableInOrderAtAHundredPlays()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0]);
        await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await AppFixture.InjectPlaysAsync(page, 100);
        await page.EvaluateAsync("renderLibrary()");
        var expected = await page.EvaluateAsync<string[]>("shownPlays().sort(byCat).map(p => p.id)");
        var seen = new List<string>();
        for (var i = 0; i < 17; i++)
        {
            Assert.Equal($"Page {i + 1} of 17", await page.InnerTextAsync("#lpage"));
            var rows = page.Locator(".lib:not([inert]) .lrow");
            var ids = await rows.EvaluateAllAsync<string[]>("rs => rs.map(r => r.dataset.id)");
            Assert.InRange(ids.Length, 1, 6);
            seen.AddRange(ids);
            if (i < 16)
            {
                await page.ClickAsync("#lnext");
                await page.WaitForFunctionAsync("""
                    i => document.querySelector('#lpage').textContent === `Page ${i + 1} of 17`
                    """, i + 1);
            }
        }
        Assert.Equal(expected, seen);
        Assert.True(await page.IsDisabledAsync("#lnext"));
        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public async Task RowActionsKeepThePageAndFiltersSearchTheWholeLibrary()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[4]);
        await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await page.EvaluateAsync("renderLibrary()");
        await page.ClickAsync("#lnext");
        await page.WaitForFunctionAsync("document.querySelector('#lpage').textContent === 'Page 2 of 5'");
        var row = page.Locator(".lib:not([inert]) .lrow").First;
        var id = await row.GetAttributeAsync("data-id");
        var before = await page.EvaluateAsync<bool>("id => deck.includes(id)", id);
        await row.Locator(".lmark").ClickAsync();
        Assert.NotEqual(before, await page.EvaluateAsync<bool>("id => deck.includes(id)", id));
        Assert.Equal(before ? "false" : "true", await row.GetAttributeAsync("aria-pressed"));
        Assert.Equal("Page 2 of 5", await page.InnerTextAsync("#lpage"));
        await row.Locator(".lthumb").ClickAsync();
        await page.ClickAsync("#prev-close");
        Assert.Equal("Page 2 of 5", await page.InnerTextAsync("#lpage"));
        await row.Locator(".lren").ClickAsync();
        await page.FillAsync("#rn-real", "PAGED PLAY");
        await page.ClickAsync("#rn-save");
        Assert.Equal("Page 2 of 5", await page.InnerTextAsync("#lpage"));
        await page.SetViewportSizeAsync(600, 1024);
        await page.WaitForTimeoutAsync(250);
        Assert.Equal("Page 2 of 5", await page.InnerTextAsync("#lpage"));
        await page.FillAsync("#lq", "PAGED PLAY");
        Assert.Equal(1, await page.Locator(".lrow").CountAsync());
        Assert.Equal(id, await page.Locator(".lrow").GetAttributeAsync("data-id"));
        Assert.Equal(0, await page.Locator("#lpage").CountAsync());
        Assert.True(await page.EvaluateAsync<bool>("document.activeElement.id === 'lq'"));
        await page.FillAsync("#lq", "no such play");
        Assert.Equal(1, await page.Locator("#lclear").CountAsync());
        await page.ClickAsync("#lclear");
        Assert.Equal("Page 1 of 5", await page.InnerTextAsync("#lpage"));
        await page.ClickAsync("#lfilt [data-kind=pass]");
        Assert.True(await page.EvaluateAsync<bool>("""
            () => {
              const kinds = [...document.querySelectorAll('.lkind')];
              return kinds.length > 6 && kinds.every(k => k.textContent === 'Pass')
                && document.querySelector('#lpage').textContent.startsWith('Page 1 of ');
            }
            """));
        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
