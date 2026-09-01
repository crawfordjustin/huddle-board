import pathlib
import sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import harness                                              # noqa: E402
from playwright.sync_api import sync_playwright

# Serve dist/deploy ourselves on an ephemeral port. The old version assumed a
# server was already up on a fixed port, which made this check silently
# unrunnable — and made it flaky when two runs overlapped.
with harness.serve() as origin, sync_playwright() as pw:
    URL = origin + "/index.html"
    b = pw.chromium.launch()
    ctx = b.new_context(viewport={"width":1600,"height":1000})
    pg = ctx.new_page()
    errs=[]; pg.on("pageerror", lambda e: errs.append(str(e)))
    pg.goto(URL); pg.wait_for_timeout(1500)

    reg = pg.evaluate("navigator.serviceWorker.getRegistration().then(r => !!r)")
    print("service worker registered :", reg)
    print("version in app            :", pg.evaluate("APP_BUILD"))
    print("manifest linked           :", pg.locator("link[rel=manifest]").count() == 1)

    # go offline and make sure it still boots from cache
    pg.wait_for_timeout(1200)
    ctx.set_offline(True)
    pg.goto(URL); pg.wait_for_timeout(1200)
    print("OFFLINE reload -> tiles   :", pg.locator(".tile").count())
    print("OFFLINE update button hid :", pg.locator("#upd").count() == 0)
    ctx.set_offline(False)

    # ship a new build and confirm the tablet is offered it
    harness.build(version="9.9.9-test")   # pretend a new build was deployed
    pg.evaluate("navigator.serviceWorker.getRegistration().then(r => r.update())")
    pg.wait_for_timeout(2500)
    has = pg.locator("#upd").count()
    print("NEW BUILD -> update button:", has)
    if has:
        pg.click("#upd"); pg.wait_for_timeout(2500)
        print("after tapping update      :", pg.evaluate("APP_BUILD"))
    print("JS errors                 :", errs or "none")
    ctx.close(); b.close()

harness.build()          # put the real version back in dist/
