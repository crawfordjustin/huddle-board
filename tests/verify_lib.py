import pathlib
import sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import harness                                              # noqa: E402
from playwright.sync_api import sync_playwright
uri = harness.app_uri()
INJECT = harness.INJECT_PLAYS

def check(pg, label):
    errs = pg.evaluate("() => document.querySelectorAll('.lrow').length")
    return errs

with sync_playwright() as pw:
    b = pw.chromium.launch()
    pg = b.new_page(viewport={"width":1600,"height":1000})
    errs=[]; pg.on("pageerror", lambda e: errs.append(str(e)))
    pg.goto(uri); pg.wait_for_timeout(400)
    pg.click("#edit"); pg.wait_for_timeout(500)
    print("14 plays, rows shown      :", check(pg,"all"))
    pg.click('[data-kind="run"]'); pg.wait_for_timeout(300)
    print("filter Run                :", check(pg,"run"),
          "| all rows are runs:", pg.eval_on_selector_all(".lkind","e=>e.every(x=>x.textContent=='Run')"))
    pg.click('[data-kind="pass"]'); pg.wait_for_timeout(300)
    print("filter Pass               :", check(pg,"pass"))
    pg.click('[data-kind="all"]'); pg.click('[data-cat="GOAL LINE"]'); pg.wait_for_timeout(300)
    print("filter Goal line          :", check(pg,"goal"),
          "|", pg.eval_on_selector_all(".lname b","e=>e.map(x=>x.textContent)"))
    pg.click('[data-cat="all"]'); pg.wait_for_timeout(200)
    pg.fill("#lq", "wheel"); pg.wait_for_timeout(350)
    print("search 'wheel'            :", check(pg,"q"),
          "|", pg.eval_on_selector_all(".lname b","e=>e.map(x=>x.textContent)"))
    print("  focus kept in search box:", pg.evaluate("document.activeElement.id === 'lq'"))
    pg.fill("#lq", "zzzz"); pg.wait_for_timeout(350)
    print("search no match           :", check(pg,"none"),
          "| empty state:", pg.locator(".lempty").count() == 1)
    pg.click("#lclear"); pg.wait_for_timeout(350)
    print("after Clear filters       :", check(pg,"cleared"))
    pg.click("#lonly"); pg.wait_for_timeout(300)
    print("In deck only              :", check(pg,"deck"))
    # toggling a play while filtered must not drop the filter
    pg.click("#lonly"); pg.click('[data-cat="RUN ZONE"]'); pg.wait_for_timeout(300)
    before = check(pg,"x")
    pg.eval_on_selector(".lrow", "e=>e.click()"); pg.wait_for_timeout(350)
    print("add a play while filtered : rows before/after", before, check(pg,"y"),
          "| filter still on:", pg.eval_on_selector('[data-cat="RUN ZONE"]', "e=>e.classList.contains('on')"))
    print("errors:", errs or "none")
    pg.screenshot(path="lib14.png")

    # 100 plays
    pg2 = b.new_page(viewport={"width":1600,"height":1000})
    e2=[]; pg2.on("pageerror", lambda e: e2.append(str(e)))
    pg2.goto(uri); pg2.wait_for_timeout(300)
    pg2.evaluate(INJECT, 100); pg2.evaluate("renderLibrary()"); pg2.wait_for_timeout(600)
    print()
    print("100 plays, rows           :", check(pg2,"all"))
    pg2.click('[data-cat="GOAL LINE"]'); pg2.wait_for_timeout(400)
    print("100 plays, Goal line only :", check(pg2,"goal"))
    pg2.screenshot(path="lib100b.png")
    pg2.click('[data-cat="all"]'); pg2.wait_for_timeout(300)
    t = pg2.evaluate("""() => {const t=performance.now();
        document.querySelector('[data-kind=\\"pass\\"]').click(); return performance.now()-t;}""")
    print("100 plays, one filter tap : %.1f ms" % t, " errors:", e2 or "none")
    b.close()
