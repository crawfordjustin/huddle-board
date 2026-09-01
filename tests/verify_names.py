import pathlib
import sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import harness                                              # noqa: E402
from playwright.sync_api import sync_playwright
uri = harness.app_uri()
SIZES = harness.SIZES
# Single-line, nowrap, ellipsis elements: only width can clip them. Height is
# not a usable signal — a 1em line box is shorter than the glyph box, so
# scrollHeight always reads a pixel or two over clientHeight.
CLIP = """els => els.filter(e => e.scrollWidth > e.clientWidth + 1)
        .map(e => e.textContent.trim())"""

# Deck names may wrap to two lines, so height maths is not the test. The real
# questions are: does a line run past the box, does it take more than two
# lines, and does the block spill out of the tile it lives in.
DECK_CLIP = """els => els.filter(e => {
  const r = document.createRange(); r.selectNodeContents(e);
  const rects = [...r.getClientRects()].filter(b => b.height > 0.5);
  const tops = new Set(rects.map(b => Math.round(b.top * 2)));
  const box = e.getBoundingClientRect();
  const wide = rects.some(b => b.right > box.right + 1 || b.left < box.left - 1);
  const host = e.closest('.tilemain').getBoundingClientRect();
  const spill = box.bottom > host.bottom + 1 || box.top < host.top - 1;
  return wide || tops.size > 2 || spill;
}).map(e => e.textContent.trim())"""

def run(fun):
    with sync_playwright() as pw:
        b = pw.chromium.launch()
        for label, w, h in SIZES:
            pg = b.new_page(viewport={"width":w,"height":h})
            pg.goto(uri); pg.wait_for_timeout(400)
            pg.evaluate(f"cfg.funNames={'true' if fun else 'false'}; saveCfg();"
                        "deck = DATA.plays.map(p=>p.id); saveDeck(); renderDeck();")
            pg.wait_for_timeout(450)
            deck = pg.eval_on_selector_all(".tile .kid", DECK_CLIP)
            # every tile must be reachable
            reach = pg.evaluate("""() => {
              const g = document.querySelector('.tiles');
              g.scrollTop = g.scrollHeight;                 // scroll to the end
              const gr = g.getBoundingClientRect();
              const last = [...g.querySelectorAll('.tile')].pop().getBoundingClientRect();
              const okBottom = last.bottom <= gr.bottom + 2;
              g.scrollTop = 0;
              const first = g.querySelector('.tile').getBoundingClientRect();
              return okBottom && first.top >= gr.top - 2;
            }""")
            pg.click("#edit"); pg.wait_for_timeout(350)
            lib = pg.eval_on_selector_all(".lname b", CLIP)
            pg.click("#done"); pg.wait_for_timeout(250)
            pg.evaluate("openPlay('p_11')"); pg.wait_for_timeout(350)
            bar = pg.eval_on_selector_all(".titlewrap .kid", CLIP)
            tag = "fun " if fun else "real"
            ok = not (deck or lib or bar) and reach
            print(f"{tag} {label:17} {w}x{h}  clipped deck/lib/bar: "
                  f"{len(deck)}/{len(lib)}/{len(bar)}  all tiles reachable: {reach}"
                  f"  {'OK' if ok else 'FAIL ' + str(deck+lib+bar)}")
            pg.close()
        b.close()
run(True); print(); run(False)
