namespace HuddleBoard.Tests;

/// <summary>
/// No clipped play names anywhere, and every tile in the deck reachable.
/// </summary>
/// <remarks>
/// The webfont is absent offline and the fallback is much wider, so CSS sizing
/// alone will clip — the app measures and shrinks instead. These checks are what
/// catch it when that stops working.
/// </remarks>
public sealed class NameChecks(AppFixture app)
{
    /// <summary>
    /// Single-line, nowrap, ellipsis elements: only width can clip them. Height
    /// is not a usable signal — a 1em line box is shorter than the glyph box, so
    /// scrollHeight always reads a pixel or two over clientHeight.
    /// </summary>
    private const string Clipped = """
        els => els.filter(e => e.scrollWidth > e.clientWidth + 1)
                  .map(e => e.textContent.trim())
        """;

    /// <summary>
    /// Deck names may wrap to two lines, so height maths is not the test. The
    /// real questions are: does a line run past the box, does it take more than
    /// two lines, and does the block spill out of the tile it lives in.
    /// </summary>
    private const string DeckClipped = """
        els => els.filter(e => {
          const r = document.createRange(); r.selectNodeContents(e);
          const rects = [...r.getClientRects()].filter(b => b.height > 0.5);
          const tops = new Set(rects.map(b => Math.round(b.top * 2)));
          const box = e.getBoundingClientRect();
          const wide = rects.some(b => b.right > box.right + 1 || b.left < box.left - 1);
          // measured against the CARD, not .tilemain: the card centres its
          // children, so .tilemain is sized to its own content and is happily
          // taller than the tile it sits in — it never reports the overflow
          const tile = e.closest('.tile'), card = tile.getBoundingClientRect();
          const cs = getComputedStyle(tile);
          const spill = box.bottom > card.bottom - parseFloat(cs.paddingBottom) + 1.5
                     || box.top < card.top + parseFloat(cs.paddingTop) - 1.5;
          return wide || tops.size > 2 || spill;
        }).map(e => e.textContent.trim())
        """;

    private const string EveryTileReachable = """
        () => {
          const strip = document.querySelector('.deckpages');
          const pages = [...strip.querySelectorAll('.tiles')];
          const width = strip.getBoundingClientRect().width;
          const ok = pages.length > 0 && pages.every((g, i) => {
            strip.scrollLeft = i * width;
            const r = strip.getBoundingClientRect();
            const tiles = [...g.querySelectorAll('.tile')];
            return tiles.length > 0 && tiles.length <= 6 && tiles.every(t => {
              const b = t.getBoundingClientRect();
              return b.top >= r.top - 2 && b.bottom <= r.bottom + 2
                && b.left >= r.left - 2 && b.right <= r.right + 2;
            });
          });
          strip.scrollLeft = 0;
          return ok;
        }
        """;

    public static TheoryData<bool, string, int, int> Cases
    {
        get
        {
            var data = new TheoryData<bool, string, int, int>();
            foreach (var fun in new[] { true, false })
            {
                foreach (var s in AppFixture.Sizes)
                    data.Add(fun, s.Label, s.Width, s.Height);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task NoNameIsClippedAndEveryTileIsReachable(
        bool funNames, string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height));

        // put every play in the deck, so the grid is as full as it ever gets
        await page.EvaluateAsync(
            $"cfg.funNames={(funNames ? "true" : "false")}; saveCfg();"
            + "deck = DATA.plays.map(p=>p.id); saveDeck(); renderDeck();");
        await page.WaitForTimeoutAsync(450);

        var deck = await page.EvalOnSelectorAllAsync<string[]>(".tile .kid", DeckClipped);
        var reachable = await page.EvaluateAsync<bool>(EveryTileReachable);

        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForTimeoutAsync(350);
        var library = await page.EvalOnSelectorAllAsync<string[]>(".lname b", Clipped);

        await page.ClickAsync("#done");
        await page.WaitForTimeoutAsync(250);
        await page.EvaluateAsync("openPlay('p_11')");
        await page.WaitForTimeoutAsync(350);
        var titleBar = await page.EvalOnSelectorAllAsync<string[]>(".titlewrap .kid", Clipped);

        await page.CloseAsync();

        Assert.True(deck.Length == 0, "clipped in the deck: " + string.Join(", ", deck));
        Assert.True(library.Length == 0, "clipped in the library: " + string.Join(", ", library));
        Assert.True(titleBar.Length == 0, "clipped in the title bar: " + string.Join(", ", titleBar));
        Assert.True(reachable, "a deck page has an unreachable tile");
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
