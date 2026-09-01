import pathlib
import sys
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import harness                                              # noqa: E402
from playwright.sync_api import sync_playwright
uri = harness.app_uri()
SIZES = harness.SIZES
CHECK = """() => {
  // every name must stay inside the playing surface, in both stages and both
  // mirror states, and no letters may survive anywhere on a marker
  const svg = document.getElementById('field');
  const f = svg.getBoundingClientRect();
  const bad = [];
  for (const k in SC.players){
    const P = SC.players[k];
    if (P.tag) bad.push(k + ': still has a letter tag');
    for (const t of [P.l1, P.l2]){
      if (!t.textContent) continue;
      const b = t.getBoundingClientRect();
      if (b.left < f.left - 1 || b.right > f.right + 1 ||
          b.top < f.top - 1 || b.bottom > f.bottom + 1)
        bad.push(k + ': "' + t.textContent + '" escapes the field');
    }
  }
  return bad;
}"""
with sync_playwright() as pw:
    b=pw.chromium.launch()
    fails=0; checked=0
    for lbl,w,h in SIZES:
        pg=b.new_page(viewport={"width":w,"height":h})
        errs=[]; pg.on("pageerror", lambda e: errs.append(str(e)))
        pg.goto(uri); pg.wait_for_timeout(350)
        ids = pg.evaluate("DATA.plays.map(p=>p.id)")
        bad=[]
        for pid in ids:
            pg.evaluate(f"openPlay('{pid}')"); pg.wait_for_timeout(90)
            for mirror in (False, True):
                if mirror: pg.evaluate("mTarget=1;mAnim=1")
                for stage in ("lineup","run"):
                    pg.evaluate(f"S.stage='{stage}'; S.t0=performance.now()-S.tl.tEnd*0.5")
                    pg.wait_for_timeout(60)
                    r = pg.evaluate(CHECK); checked+=1
                    bad += [f"{pid} {stage} {'mir' if mirror else '   '} {x}" for x in r]
                pg.evaluate("mTarget=0;mAnim=0")
        fails += len(bad)
        print(f"{lbl:9} {w}x{h}  {len(ids)} plays x 2 mirrors x 2 stages  "
              f"{'OK' if not bad and not errs else 'FAIL'}  {bad[:3]}  errs:{errs or 'none'}")
        pg.close()
    print(f"\n{checked} states checked, {fails} problems")
    b.close()
