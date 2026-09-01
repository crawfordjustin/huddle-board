# -*- coding: utf-8 -*-
"""7x5 in field cards — two per Letter page, cut and put on a ring."""
import pathlib as _pathlib
import sys as _sys
_sys.path.insert(0, str(_pathlib.Path(__file__).resolve().parent.parent))
_OUT = _pathlib.Path(__file__).resolve().parent.parent / "dist" / "print"
_OUT.mkdir(parents=True, exist_ok=True)

import html as _html
from plays import PLAYS
from spots import SPOT_GLOSSARY, SHAPES, DEFAULT_RULE, PLAY_TEXT
import render as R


def esc(t): return _html.escape(t)


def play_card(play):
    color = R.CATEGORY_COLORS[play["category"]]
    txt = PLAY_TEXT[play["num"]]
    calls = "".join(f'<li><b>{esc(a)}</b><span>{esc(b)}</span></li>' for a, b in txt["calls"])
    return f"""
<div class="card" style="--accent:{color}">
  <div class="chead">
    <div class="cnum">{play['num']}</div>
    <div class="cname"><h2>{esc(play['name'])}</h2>
      <p>{esc(play['formation'])}</p></div>
    <div class="ccat">{esc(play['category'])}</div>
  </div>
  <div class="cbody">
    <div class="cdiag">{R.play_svg(play)}</div>
    <div class="ccalls">
      <ul>{calls}</ul>
      <div class="cdef">Anyone with no job: <b>GO</b></div>
    </div>
  </div>
</div>"""


def spots_card():
    rows = "".join(f'<li><b>{esc(t)}</b><span>{esc(n)}</span></li>' for t, n, _ in SPOT_GLOSSARY)
    return f"""
<div class="card ref" style="--accent:#1f2933">
  <div class="chead">
    <div class="cnum">&#9679;</div>
    <div class="cname"><h2>THE SPOTS</h2><p>Point and say the name</p></div>
    <div class="ccat">REFERENCE</div>
  </div>
  <div class="spotsbody">
    <div class="spotsdiag">{R.spots_svg()}</div>
    <ul class="tight">{rows}</ul>
  </div>
</div>"""


def shapes_card():
    cells = "".join(f'<div>{R.shape_svg(pts, end)}<b>{esc(n)}</b></div>'
                    for n, _, pts, end in SHAPES)
    return f"""
<div class="card ref" style="--accent:#1f2933">
  <div class="chead">
    <div class="cnum">&#9679;</div>
    <div class="cname"><h2>THE SHAPES</h2><p>Spot first, then shape, then number</p></div>
    <div class="ccat">REFERENCE</div>
  </div>
  <div class="shapesrow">{cells}</div>
  <div class="cdef wide"><b>Default rule:</b> {esc(DEFAULT_RULE)}</div>
</div>"""


CSS = """
@page { size: Letter portrait; margin: 0; }
* { box-sizing: border-box; }
body { margin:0; font-family:"Helvetica Neue", Helvetica, Arial, sans-serif; color:#1f2933;
       -webkit-print-color-adjust:exact; print-color-adjust:exact; }
.sheet { width:8.5in; height:11in; padding:0.5in 0.75in; page-break-after:always;
         display:flex; flex-direction:column; justify-content:center; gap:0.5in; }
.sheet:last-child { page-break-after:auto; }
.card { width:7in; height:5in; border:1.5px dashed #b9c2cb; border-radius:10px;
        padding:0.22in 0.26in; display:flex; flex-direction:column; overflow:hidden; }
.chead { display:flex; align-items:center; gap:11px; border-bottom:3px solid var(--accent);
         padding-bottom:8px; flex:none; }
.cnum { background:var(--accent); color:#fff; font-size:20pt; font-weight:800; width:46px;
        height:46px; border-radius:9px; display:flex; align-items:center; justify-content:center;
        flex:none; }
.cname { flex:1; }
.cname h2 { font-size:24pt; margin:0; letter-spacing:-.015em; line-height:1; }
.cname p { font-size:8.4pt; color:#7b8794; margin:4px 0 0; letter-spacing:.12em;
           font-weight:700; }
.ccat { font-size:7pt; letter-spacing:.11em; font-weight:800; color:#fff; background:var(--accent);
        padding:4px 9px; border-radius:4px; flex:none; }
.cbody { display:flex; gap:14px; flex:1; min-height:0; padding-top:10px; }
.cdiag { flex:1.95; min-width:0; display:flex; align-items:center; }
.cdiag svg { width:100%; height:auto; max-height:100%; display:block; }
.ccalls { flex:1; min-width:0; display:flex; flex-direction:column; }
.ccalls ul { list-style:none; margin:0; padding:0; flex:1; }
.ccalls li { margin-bottom:11px; }
.ccalls b { display:block; font-size:10.5pt; letter-spacing:.04em; color:#1f2933; }
.ccalls span { display:block; font-size:10.5pt; color:#3e4c59; line-height:1.25; }
.spotsbody { flex:1; min-height:0; display:flex; flex-direction:column; padding-top:10px; }
.spotsdiag { flex:1; min-height:0; display:flex; align-items:center; justify-content:center; }
.spotsdiag svg { width:100%; height:auto; max-height:100%; }
ul.tight { list-style:none; margin:9px 0 0; padding:0; display:grid;
           grid-template-columns:repeat(3,1fr); gap:5px 14px; flex:none; }
ul.tight li { display:flex; gap:7px; align-items:baseline; }
ul.tight b { font-size:8pt; color:#fff; background:#1f2933; border-radius:3px;
             padding:2px 5px; min-width:26px; text-align:center; flex:none; }
ul.tight span { font-size:9.4pt; font-weight:700; }
.cdef { border-top:1px solid #e4e8ec; padding-top:7px; font-size:8.6pt; color:#7b8794; flex:none; }
.cdef b { color:#127a4d; font-weight:800; }
.cdef.wide { margin-top:8px; font-size:9.4pt; color:#3e4c59; line-height:1.4; }
.shapesrow { display:grid; grid-template-columns:repeat(5,1fr); gap:14px 10px; padding-top:10px;
             flex:1; align-content:center; }
.shapesrow div { text-align:center; }
.shapesrow svg { height:58px; width:auto; max-width:100%; display:block; margin:0 auto 3px; }
.shapesrow b { font-size:9.2pt; letter-spacing:.03em; }
"""


def build():
    cards = [spots_card(), shapes_card()] + [play_card(p) for p in PLAYS]
    sheets = ""
    for i in range(0, len(cards), 2):
        sheets += f'<section class="sheet">{"".join(cards[i:i+2])}</section>'
    return (f'<!doctype html><html><head><meta charset="utf-8"><title>Field Cards</title>'
            f'<style>{CSS}</style></head><body>{sheets}</body></html>')


if __name__ == "__main__":
    open(_OUT / "cards.html", "w").write(build())
    print("wrote cards.html")
