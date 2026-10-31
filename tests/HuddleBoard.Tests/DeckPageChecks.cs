using Microsoft.Playwright;

namespace HuddleBoard.Tests;

public sealed class DeckPageChecks(AppFixture app)
{
    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task SixPerPageAndEveryPlayReachable(string label, int width, int height)
    {
        foreach (var players in new[] { 5, 6, 7 })
        {
            var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height), players: players);
            await page.EvaluateAsync("deck = DATA.plays.map(p => p.id); renderDeck()");
            var expected = await page.EvaluateAsync<string[]>("deck");
            var seen = new List<string>();
            var count = (expected.Length + 5) / 6;
            for (var i = 0; i < count; i++)
            {
                await page.WaitForFunctionAsync($"deckPage === {i} && Math.abs(document.getElementById('deckpages').scrollLeft - {i} * document.getElementById('deckpages').getBoundingClientRect().width) < 2");
                var active = page.Locator(".tiles:not([inert])");
                Assert.Equal(1, await active.CountAsync());
                var ids = await active.Locator(".tile").EvaluateAllAsync<string[]>("ts => ts.map(t => t.dataset.id)");
                Assert.InRange(ids.Length, 1, 6);
                seen.AddRange(ids);
                Assert.True(await active.EvaluateAsync<bool>("""
                    g => {
                      const r = g.getBoundingClientRect();
                      return [...g.querySelectorAll('.tile')].every(t => {
                        const b = t.getBoundingClientRect();
                        return b.top >= r.top - 1 && b.bottom <= r.bottom + 1
                          && b.left >= r.left - 1 && b.right <= r.right + 1;
                      });
                    }
                    """), $"{label}, {players}: a tile falls outside its page");
                Assert.Equal($"Page {i + 1} of {count}", await page.InnerTextAsync("#dpage"));
                if (i < count - 1) await page.ClickAsync("#dnext");
            }
            Assert.Equal(expected, seen);
            Assert.True(await page.IsDisabledAsync("#dnext"));
            await page.Locator(".tiles:not([inert]) .tile").First.ClickAsync();
            await page.EvaluateAsync("renderDeck()");
            Assert.Equal(count - 1, await page.EvaluateAsync<int>("deckPage"));
            await page.SetViewportSizeAsync(height, width);
            await page.WaitForTimeoutAsync(250);
            Assert.Equal(count - 1, await page.EvaluateAsync<int>("deckPage"));
            await page.FocusAsync("#deckpages");
            await page.Keyboard.PressAsync("ArrowLeft");
            await page.WaitForFunctionAsync($"deckPage === {count - 2}");
            await page.ClickAsync("#dfilt [data-kind=run]");
            Assert.Equal(0, await page.EvaluateAsync<int>("deckPage"));
            await page.EvaluateAsync("deck = DATA.defaultDeck.slice(); renderDeck()");
            Assert.Equal(0, await page.Locator(".decknav").CountAsync());
            await page.CloseAsync();
            Assert.Empty(errors);
        }
    }

    [Fact]
    public async Task NativeTouchSwipeChangesPagesWithoutOpeningAPlay()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0]);
        await page.EvaluateAsync("deck = DATA.plays.slice(0, 14).map(p => p.id); renderDeck()");
        var cdp = await page.Context.NewCDPSessionAsync(page);
        await cdp.SendAsync("Emulation.setTouchEmulationEnabled", new() { ["enabled"] = true });
        var box = (await page.Locator("#deckpages").BoundingBoxAsync())!;
        var y = box.Y + box.Height / 2;
        async Task Touch(string type, float x) => await cdp.SendAsync("Input.dispatchTouchEvent", new()
        {
            ["type"] = type,
            ["touchPoints"] = type == "touchEnd" ? Array.Empty<object>() : new object[] { new { x, y } }
        });
        await Touch("touchStart", box.X + box.Width * .8f);
        for (var i = 1; i <= 12; i++)
        {
            await Touch("touchMove", box.X + box.Width * (.8f - i * .05f));
            await page.WaitForTimeoutAsync(20);
        }
        await Touch("touchEnd", 0);
        await page.WaitForFunctionAsync("deckPage === 1");
        Assert.Equal("deck", await page.EvaluateAsync<string>("S.screen"));
        await page.WaitForTimeoutAsync(400);
        await page.ClickAsync("#dprev");
        await page.WaitForFunctionAsync("deckPage === 0");
        Assert.True(await page.IsDisabledAsync("#dprev"));
        await page.CloseAsync();
        Assert.Empty(errors);
    }
}
