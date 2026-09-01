import pathlib
import sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import harness                                              # noqa: E402
from playwright.sync_api import sync_playwright
uri = harness.app_uri()
with sync_playwright() as pw:
    b = pw.chromium.launch()
    ctx = b.new_context(viewport={"width":1600,"height":1000}, offline=True)
    pg = ctx.new_page(); errs=[]; pg.on("pageerror", lambda e: errs.append(str(e)))
    pg.goto(uri); pg.wait_for_timeout(900)
    print("file:// offline tiles     :", pg.locator(".tile").count())
    print("file:// version           :", pg.evaluate("APP_BUILD"))
    print("file:// sw skipped safely :", pg.evaluate("!location.protocol.startsWith('http')"))
    pg.click(".tile[data-id='p_01']"); pg.click("#stage"); pg.wait_for_timeout(1500)
    print("file:// routes drawn      :", pg.locator("#field polyline").count())
    pg.click("#back"); pg.click("#setup"); pg.wait_for_timeout(300)
    print("JS errors                 :", errs or "none")
    ctx.close(); b.close()
