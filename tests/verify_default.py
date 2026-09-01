import pathlib
import sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import harness                                              # noqa: E402
from playwright.sync_api import sync_playwright
uri = harness.app_uri()
with sync_playwright() as pw:
    b = pw.chromium.launch()
    # 1. fresh install
    pg = b.new_page(viewport={"width":1600,"height":1000})
    pg.goto(uri); pg.wait_for_timeout(500)
    print("fresh: funNames        :", pg.evaluate("cfg.funNames"))
    print("fresh: first tile name :", pg.eval_on_selector(".tile .kid","e=>e.textContent.trim()"))
    print("fresh: play title      :", pg.evaluate("openPlay('p_01')") or
          pg.eval_on_selector(".titlewrap .kid","e=>e.textContent.trim()"))
    pg.screenshot(path="deck_default.png")
    pg.close()
    # 2. a coach who had already chosen Fun keeps it
    ctx = b.new_context(viewport={"width":1600,"height":1000})
    pg = ctx.new_page(); pg.goto(uri); pg.wait_for_timeout(400)
    pg.evaluate("cfg.funNames=true; saveCfg();")
    pg.reload(); pg.wait_for_timeout(500)
    print("returning coach kept   :", pg.evaluate("cfg.funNames"),
          "/", pg.eval_on_selector(".tile .kid","e=>e.textContent.trim()"))
    # toggle still works both ways
    pg.evaluate("cfg.funNames=false; saveCfg(); renderDeck();"); pg.wait_for_timeout(400)
    print("toggled to real        :", pg.eval_on_selector(".tile .kid","e=>e.textContent.trim()"))
    ctx.close(); b.close()
