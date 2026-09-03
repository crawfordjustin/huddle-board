using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuddleBoard.Tests;

/// <summary>
/// The deck menu: one hamburger in the top right holding Change plays, Setup and Exit.
/// </summary>
/// <remarks>
/// A dropdown is the one piece of chrome in this app that is drawn outside its
/// own box, so the failures worth catching are geometric — a panel that runs
/// off the edge of a narrow tablet, or one the play tiles paint over because
/// they are <c>position:relative</c> and it has no stacking order of its own.
/// Both are invisible to any check that only asks whether the buttons exist.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class MenuChecks(AppFixture app)
{
    private sealed record Panel(
        [property: JsonPropertyName("items")] int Items,
        [property: JsonPropertyName("minItemH")] double MinItemHeight,
        [property: JsonPropertyName("left")] double Left,
        [property: JsonPropertyName("right")] double Right,
        [property: JsonPropertyName("bottom")] double Bottom,
        [property: JsonPropertyName("onTop")] bool OnTop,
        [property: JsonPropertyName("hamRight")] double HamRight,
        [property: JsonPropertyName("rightmost")] bool Rightmost,
        [property: JsonPropertyName("w")] double Width,
        [property: JsonPropertyName("h")] double Height);

    private const string Measure = """
        () => {
          const menu = document.getElementById('menu');
          const ham = document.getElementById('ham');
          const m = menu.getBoundingClientRect(), hb = ham.getBoundingClientRect();
          const items = [...menu.querySelectorAll('button')];
          // the panel is drawn over the tiles, so ask the browser who is
          // actually painted at the middle of the first item
          const f = items[0].getBoundingClientRect();
          const hit = document.elementFromPoint((f.left + f.right) / 2, (f.top + f.bottom) / 2);
          const sibs = [...document.querySelectorAll('.dh-right > *')];
          return JSON.stringify({
            items: items.length,
            minItemH: Math.min(...items.map(b => +b.getBoundingClientRect().height.toFixed(1))),
            left: +m.left.toFixed(1), right: +m.right.toFixed(1), bottom: +m.bottom.toFixed(1),
            onTop: menu.contains(hit),
            hamRight: +hb.right.toFixed(1),
            rightmost: sibs.at(-1) === ham.parentElement && sibs.slice(0, -1)
                       .every(s => s.getBoundingClientRect().right <= hb.right),
            w: innerWidth, h: innerHeight});
        }
        """;

    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task TheMenuOpensInsideTheScreenAndOverTheTiles(string label, int w, int h)
    {
        var size = new Viewport(label, w, h);
        var (page, errors) = await app.OpenAppAsync(size);

        // Setup and Change plays left the header; they are in the menu, and the menu starts shut
        Assert.Equal(0, await page.Locator(".dh-right > #setup, .dh-right > #edit").CountAsync());
        Assert.False(await page.Locator("#menu").IsVisibleAsync());

        await page.ClickAsync("#ham");
        await page.WaitForSelectorAsync("#menu:visible");

        var panel = JsonSerializer.Deserialize<Panel>(
            await page.EvaluateAsync<string>(Measure))!;

        Assert.Equal(3, panel.Items);
        Assert.True(panel.Rightmost, $"{size}: the hamburger is not the last thing in the header");
        Assert.True(panel.Left >= 0 && panel.Right <= panel.Width + 0.5,
            $"{size}: the panel runs off the side ({panel.Left} to {panel.Right} of {panel.Width})");
        Assert.True(panel.Bottom <= panel.Height,
            $"{size}: the panel runs off the bottom ({panel.Bottom} of {panel.Height})");
        Assert.True(panel.OnTop, $"{size}: the tiles are painting over the menu");
        Assert.True(panel.MinItemHeight >= 44,
            $"{size}: a menu item is only {panel.MinItemHeight}px tall — too small for a thumb");

        // a tap anywhere else puts it away
        await page.Mouse.ClickAsync((float)(panel.Width / 2), (float)(panel.Height - 12));
        Assert.False(await page.Locator("#menu").IsVisibleAsync());

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Every row in Setup has to be reachable and thumb-sized on every tablet.
    /// Eight rows do not fit 600px of landscape, so the screen scrolls — and a
    /// scroller centred with justify-content puts its first row permanently
    /// above the top, which is the failure this is really watching for.
    /// </summary>
    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task EverySettingRowIsReachable(string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height));
        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await page.WaitForSelectorAsync(".setrows");
        await page.WaitForTimeoutAsync(300);

        var rows = await page.Locator(".setrow").CountAsync();

        var short_ = await page.EvalOnSelectorAllAsync<string[]>(".setrow", """
            els => els.filter(e => e.getBoundingClientRect().height < 44)
                      .map(e => e.querySelector('.setlab b').textContent.trim())
            """);

        var reachable = await page.EvaluateAsync<bool>("""
            () => {
              const g = document.querySelector('.setrows');
              const rows = [...g.querySelectorAll('.setrow')];
              g.scrollTop = g.scrollHeight;
              const bottom = rows.at(-1).getBoundingClientRect().bottom
                           <= g.getBoundingClientRect().bottom + 2;
              g.scrollTop = 0;
              const top = rows[0].getBoundingClientRect().top
                        >= g.getBoundingClientRect().top - 2;
              return top && bottom;
            }
            """);

        await page.CloseAsync();

        Assert.True(rows >= 8, $"{label}: only {rows} rows in Setup");
        Assert.True(short_.Length == 0, $"{label}: too small for a thumb: {string.Join(", ", short_)}");
        Assert.True(reachable, $"{label}: a row in Setup cannot be scrolled to");
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public async Task SetupAndExitBothGoWhereTheySay()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0]);

        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await page.WaitForSelectorAsync(".setrows");
        Assert.Equal("settings", await page.EvaluateAsync<string>("S.screen"));
        await page.ClickAsync("#sdone");
        await page.WaitForSelectorAsync(".tiles");

        await page.ClickAsync("#ham");
        await page.ClickAsync("#exit");
        await page.WaitForSelectorAsync("#start");
        Assert.Equal("intro", await page.EvaluateAsync<string>("S.screen"));

        // and back in again, because leaving has to be reversible in one tap
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".tiles");
        Assert.True(await page.Locator(".tile").CountAsync() > 0);

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
