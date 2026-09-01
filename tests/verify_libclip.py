import pathlib
import sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import harness                                              # noqa: E402
from playwright.sync_api import sync_playwright
uri = harness.app_uri()
INJECT = harness.INJECT_PLAYS
CLIP = """els => els.filter(e => e.scrollWidth > e.clientWidth + 1).map(e=>e.textContent.trim())"""
SIZES = harness.SIZES
with sync_playwright() as pw:
    b=pw.chromium.launch()
    for n in (14,100):
        for lbl,w,h in SIZES:
            pg=b.new_page(viewport={"width":w,"height":h})
            errs=[]; pg.on("pageerror", lambda e: errs.append(str(e)))
            pg.goto(uri); pg.wait_for_timeout(300)
            if n!=14: pg.evaluate(INJECT,n)
            pg.evaluate("renderLibrary()"); pg.wait_for_timeout(700)
            clipped = pg.eval_on_selector_all(".lname b", CLIP)
            print(f"{n:4} plays  {lbl:9} {w}x{h}  clipped names: {len(clipped)}"
                  f"  {'OK' if not clipped and not errs else 'FAIL '+str(clipped[:4])}")
            pg.close()
    b.close()
