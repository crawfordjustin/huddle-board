"""Regenerate the README screenshots."""
import pathlib, sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent / "tests"))
import harness
from playwright.sync_api import sync_playwright
OUT = pathlib.Path(__file__).resolve().parent
with sync_playwright() as pw:
    b = pw.chromium.launch()
    pg, _ = harness.open_app(b, 1600, 1000)
    pg.wait_for_timeout(600); pg.screenshot(path=OUT / "deck.png")
    pg.evaluate("openPlay('p_18')"); pg.wait_for_timeout(700)
    pg.screenshot(path=OUT / "play.png")
    pg.evaluate("renderLibrary()"); pg.wait_for_timeout(700)
    pg.screenshot(path=OUT / "library.png")
    b.close()
print("wrote docs screenshots")
