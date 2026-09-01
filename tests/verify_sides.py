import pathlib
import sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import harness                                              # noqa: E402
from playwright.sync_api import sync_playwright
uri = harness.app_uri()
SIZES = harness.SIZES
with sync_playwright() as pw:
    b = pw.chromium.launch()
    for lbl,w,h in SIZES:
        pg=b.new_page(viewport={"width":w,"height":h})
        errs=[]; pg.on("pageerror", lambda e: errs.append(str(e)))
        pg.goto(uri); pg.wait_for_timeout(350)
        pg.evaluate("openPlay('p_01')"); pg.wait_for_timeout(650)
        r = pg.evaluate("""() => {
          const o={}; const wrap=document.querySelector('.fieldwrap').getBoundingClientRect();
          for (const nm of ["blue","orange"]){
            const s=SC.oob[nm].r.getBoundingClientRect();
            const t=SC.oob[nm].btxt.getBoundingClientRect();
            const l=SC.oob[nm].lbl.getBoundingClientRect();
            o[nm]={off:+(((t.left+t.right)/2)-((s.left+s.right)/2)).toFixed(2),
                   loff:+(((l.left+l.right)/2)-((s.left+s.right)/2)).toFixed(2),
                   edge:+(nm==="blue"? s.left-wrap.left : wrap.right-s.right).toFixed(2),
                   vtop:+(s.top-wrap.top).toFixed(2), vbot:+(wrap.bottom-s.bottom).toFixed(2)};
          }
          return o;}""")
        # tapping the far band must reassign our side
        before = pg.evaluate("ourSide")
        pg.evaluate("""() => { const nm = ourSide==='blue'?'orange':'blue';
                    SC.oob[nm].g.dispatchEvent(new MouseEvent('click',{bubbles:true})); }""")
        pg.wait_for_timeout(250)
        after = pg.evaluate("ourSide")
        ok = all(abs(v["off"])<0.6 and abs(v["loff"])<0.6 and abs(v["edge"])<1.5
                 and abs(v["vtop"])<1.5 and abs(v["vbot"])<1.5 for v in r.values())
        print(f"{lbl:9} {w}x{h}  badge/label centred + bleeds to edge: {ok}"
              f"  tap swaps side: {before}->{after}  errors: {errs or 'none'}")
        if not ok: print("   ", r)
        pg.close()
    b.close()
