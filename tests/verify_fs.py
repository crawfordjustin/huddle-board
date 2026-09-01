import pathlib
import sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import harness                                              # noqa: E402
from playwright.sync_api import sync_playwright
with harness.serve() as origin, sync_playwright() as pw:
    b = pw.chromium.launch()

    # 1. ordinary tab -> button offered
    pg = b.new_page(viewport={"width":1400,"height":900})
    errs=[]; pg.on("pageerror", lambda e: errs.append(str(e)))
    pg.goto(origin + "/index.html"); pg.wait_for_timeout(1200)
    print("tab: Full screen button shown  :", pg.locator("#fs").count() == 1)
    print("tab: isImmersive()             :", pg.evaluate("isImmersive()"))
    pg.click("#fs"); pg.wait_for_timeout(600)
    print("tab: no error on activation    :", errs or "none")
    pg.close()

    # 2. simulate an installed launch -> button suppressed
    pg2 = b.new_page(viewport={"width":1400,"height":900})
    pg2.add_init_script("""
      const mm = window.matchMedia;
      window.matchMedia = q => q.includes('display-mode: fullscreen')
        ? {matches:true, addEventListener(){}, removeEventListener(){}, media:q}
        : mm(q);
    """)
    pg2.goto(origin + "/index.html"); pg2.wait_for_timeout(1000)
    print("installed: button hidden       :", pg2.locator("#fs").count() == 0)
    print("installed: isImmersive()       :", pg2.evaluate("isImmersive()"))
    pg2.close()

    # 3. local file -> still offered (no install possible there)
    pg3 = b.new_page(viewport={"width":1400,"height":900})
    pg3.goto(harness.app_uri()); pg3.wait_for_timeout(700)
    print("file://: button shown          :", pg3.locator("#fs").count() == 1)
    pg3.close()
    b.close()
