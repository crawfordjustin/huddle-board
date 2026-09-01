# -*- coding: utf-8 -*-
"""Rotation and playing-time sheet for an 11-14 player 6-on-6 roster."""
import pathlib as _pathlib
_OUT = _pathlib.Path(__file__).resolve().parent.parent / "dist" / "print"
_OUT.mkdir(parents=True, exist_ok=True)

ROSTER_ROWS = 14
SERIES_ROWS = 12


def name_lines(n, cls="nl"):
    return "".join(f'<div class="{cls}"></div>' for _ in range(n))


def unit_box(letter, tint):
    rows = "".join(
        f'<tr><td class="sp">{s}</td><td class="wr"></td></tr>'
        for s in ["THROWER", "SNAPPER", "PLAYER 3", "PLAYER 4", "PLAYER 5", "PLAYER 6"])
    return (f'<div class="unit" style="--tint:{tint}"><h4>UNIT {letter}</h4>'
            f'<table>{rows}</table></div>')


def page1():
    return f"""
<section class="page">
  <header class="rhead">
    <div><h1>Rotation &amp; playing time</h1>
      <p>Fill this in before kickoff. Deciding who sits while you are down a score is how
      the same four kids end up playing every snap.</p></div>
    <div class="stamp">6-on-6 &middot; 8U</div>
  </header>

  <div class="cols3">
    <div class="span2">
      <h3>Build two units</h3>
      <p class="body">With 11&ndash;14 kids, the simplest fair system is two units that alternate by
      <b>series</b>, not by play. A kid who changes spots mid-drive is a kid who lines up wrong.</p>
      <div class="units">{unit_box("A", "#127a4d")}{unit_box("B", "#1b6ba8")}
        <div class="unit" style="--tint:#c9700d"><h4>FLOATERS</h4>
          <table>{"".join(f'<tr><td class="sp">SUB {j}</td><td class="wr"></td></tr>' for j in range(1, 5))}</table>
        </div>
      </div>
      <p class="body small">Two units of six covers 12 kids. With 13&ndash;14, the extras go in
      FLOATERS and rotate in one per series &mdash; write who they replace. With 11, one kid plays
      both units; make it a different kid every game and write their name here:
      <span class="inline-line"></span></p>

      <h3 class="mt">Depth chart at the two hard spots</h3>
      <p class="body small">These are the only spots that need training. Everything else is
      interchangeable, which is exactly what makes the rotation work.</p>
      <div class="depth">
        <div><h5>THROWER</h5>
          <table><tr><td class="rk">1</td><td class="wr"></td></tr>
          <tr><td class="rk">2</td><td class="wr"></td></tr>
          <tr><td class="rk">3</td><td class="wr"></td></tr></table></div>
        <div><h5>SNAPPER</h5>
          <table><tr><td class="rk">1</td><td class="wr"></td></tr>
          <tr><td class="rk">2</td><td class="wr"></td></tr>
          <tr><td class="rk">3</td><td class="wr"></td></tr></table></div>
        <div><h5>LEARNING &mdash; GET THEM REPS IN PRACTICE</h5>
          <table><tr><td class="rk">&nbsp;</td><td class="wr"></td></tr>
          <tr><td class="rk">&nbsp;</td><td class="wr"></td></tr>
          <tr><td class="rk">&nbsp;</td><td class="wr"></td></tr></table></div>
      </div>
    </div>
    <div>
      <h3>How to fill it in</h3>
      <ol class="steps">
        <li><b>Thrower and snapper first.</b> Train two or three at each and put one of each in
          every unit. These are the only two spots that need a trained kid.</li>
        <li><b>Split the rest evenly by speed.</b> Do not stack Unit A. Two roughly equal units
          means you never feel tempted to leave one on the field.</li>
        <li><b>Do not assign spots here.</b> Players 3 through 6 can play <i>any</i> spot &mdash;
          you point them where to line up in the huddle. That is the whole point of the spot
          system.</li>
        <li><b>Swap on change of possession.</b> Whoever is not on the field is the next unit.
          No thinking required.</li>
      </ol>
      <div class="mathbox">
        <b>The fairness math</b>
        <span>A typical 8U game is 16&ndash;22 offensive series. Two units alternating means every
        kid gets 8&ndash;11 series &mdash; roughly 30&ndash;45 snaps. That is more than enough to
        satisfy any parent, and you never have to count in your head.</span>
      </div>
    </div>
  </div>
  <div class="tips">
    <div><b>Give the resting kids a job.</b> Six kids with nothing to do is a discipline problem
      waiting to happen. Rotate three roles each series: <i>spotter</i> (calls out the down and
      distance), <i>card holder</i> (finds the next play card), and <i>water</i>. Kids who have a
      job on the sideline stop asking when they go back in.</div>
    <div><b>Tell the parents the system in week one.</b> &ldquo;We run two units that alternate
      every series, so everybody plays about half. I track carries and targets and even them out
      over the season.&rdquo; Said before the first game, this ends the conversation. Said after a
      loss, it sounds like an excuse.</div>
    <div><b>Decide now when you would break it.</b> Most coaches eventually want their best six on
      the field at the end of a close game. Whether you do that is your call &mdash; but decide
      before the season and say it out loud, so you are not making it up while a parent watches
      their kid stand next to you.</div>
  </div>
  <footer class="rf">Rotation &amp; playing time &middot; page 1 of 3</footer>
</section>"""


def page2():
    heads = ["#", "THROWER", "SNAPPER", "PLAYER 3", "PLAYER 4", "PLAYER 5", "PLAYER 6",
             "SWAPPED IN FOR", "RESULT"]
    th = "".join(f'<th>{h}</th>' for h in heads)
    rows = ""
    for i in range(1, SERIES_ROWS + 1):
        cells = "".join('<td></td>' for _ in range(len(heads) - 1))
        rows += f'<tr><td class="ser">{i}</td>{cells}</tr>'
    return f"""
<section class="page">
  <header class="rhead">
    <div><h1>Game sheet</h1>
      <p>One row per offensive series. Circle the unit you are sending out and note anybody you
      swapped, so you can even it out next week.</p></div>
    <div class="stamp">Opponent &nbsp;<span class="inline-line wide"></span>&nbsp;&nbsp;
      Date &nbsp;<span class="inline-line"></span></div>
  </header>
  <table class="grid"><tr>{th}</tr>{rows}</table>
  <div class="notecols">
    <div><h4>What worked</h4><div class="lines"></div></div>
    <div><h4>What to fix at practice</h4><div class="lines"></div></div>
    <div><h4>Who needs the ball next week</h4><div class="lines"></div></div>
  </div>
  <footer class="rf">Rotation &amp; playing time &middot; page 2 of 3</footer>
</section>"""


def page3():
    rows = ""
    for i in range(1, ROSTER_ROWS + 1):
        rows += (f'<tr><td class="num">{i}</td><td class="wr"></td>'
                 f'<td class="boxes">{"".join(chr(9633) for _ in range(12))}</td>'
                 f'<td class="boxes">{"".join(chr(9633) for _ in range(6))}</td>'
                 f'<td class="boxes">{"".join(chr(9633) for _ in range(8))}</td>'
                 f'<td class="wr"></td></tr>')
    return f"""
<section class="page">
  <header class="rhead">
    <div><h1>Ball touches</h1>
      <p>Kids measure fairness in touches, not snaps. Cross off a box each time it happens.
      Anybody with an empty CARRIES row by halftime gets the next Dive.</p></div>
    <div class="stamp">Season &nbsp;<span class="inline-line"></span></div>
  </header>
  <table class="grid roster">
    <tr><th>#</th><th>PLAYER</th><th>SERIES PLAYED</th><th>CARRIES</th>
      <th>THROWN TO</th><th>NOTES</th></tr>
    {rows}
  </table>
  <div class="tips">
    <div><b>Every kid carries once a game.</b> Put a new name at BACK for 22 Dive or Pitch Right
      each series. It is the single easiest promise to keep, and the one they remember.</div>
    <div><b>Spread the targets, not just the carries.</b> Spacing (12) and Triple Out (14) let you
      pick which depth to throw &mdash; use that to feed a kid who has not been thrown to.</div>
    <div><b>Praise the route, not the catch.</b> The kid who ran a great GO and never saw the ball
      is the reason the play worked. Say so out loud, by name.</div>
  </div>
  <footer class="rf">Rotation &amp; playing time &middot; page 3 of 3</footer>
</section>"""


CSS = """
@page { size: Letter landscape; margin: 0; }
* { box-sizing: border-box; }
body { margin:0; font-family:"Helvetica Neue", Helvetica, Arial, sans-serif; color:#1f2933;
       -webkit-print-color-adjust:exact; print-color-adjust:exact; }
.page { width:11in; height:8.5in; padding:0.42in 0.5in 0.36in; page-break-after:always;
        display:flex; flex-direction:column; position:relative; overflow:hidden; }
.page:last-child { page-break-after:auto; }
.rhead { display:flex; align-items:flex-start; justify-content:space-between; gap:24px;
         border-bottom:2.5px solid #1f2933; padding-bottom:9px; margin-bottom:14px; }
.rhead h1 { font-size:22pt; margin:0; letter-spacing:-.015em; }
.rhead p { font-size:9pt; color:#616e7c; margin:6px 0 0; max-width:6.4in; line-height:1.45; }
.stamp { font-size:8.4pt; color:#7b8794; font-weight:700; letter-spacing:.06em; white-space:nowrap;
         padding-top:5px; }
.rf { position:absolute; bottom:0.24in; left:0.5in; right:0.5in; font-size:7.6pt; color:#9aa5b1;
      border-top:1px solid #e4e8ec; padding-top:5px; }
h3 { font-size:8.4pt; letter-spacing:.13em; text-transform:uppercase; color:#127a4d;
     margin:0 0 8px; }
.body { font-size:9pt; line-height:1.5; color:#3e4c59; margin:0 0 10px; }
.body.small { font-size:8.2pt; color:#616e7c; }
.cols3 { display:grid; grid-template-columns:1fr 1fr 1fr; gap:26px; }
.span2 { grid-column:span 2; }
.units { display:grid; grid-template-columns:1fr 1fr 1fr; gap:12px; }
.unit { border:1px solid #e4e8ec; border-top:4px solid var(--tint); border-radius:8px;
        padding:9px 10px 6px; }
.unit h4 { font-size:8.4pt; letter-spacing:.13em; margin:0 0 7px; color:var(--tint); }
.unit table { width:100%; border-collapse:collapse; }
.unit td { padding:0 0 6px; }
.unit .sp { font-size:6.9pt; letter-spacing:.06em; color:#9aa5b1; font-weight:800; width:52px;
            vertical-align:bottom; padding-bottom:7px; }
.wr { border-bottom:1px solid #9aa5b1; height:25px; min-width:1in; }
.inline-line { display:inline-block; border-bottom:1px solid #b9c2cb; width:1.1in; height:11px; }
.inline-line.wide { width:1.9in; }
.steps { margin:0; padding-left:16px; }
.steps li { font-size:8.4pt; line-height:1.45; margin-bottom:8px; color:#3e4c59; }
.steps b { color:#1f2933; }
.mathbox { margin-top:auto; background:#0f1b2b; color:#fff; border-radius:9px; padding:12px 14px; }
.mathbox b { display:block; font-size:7.4pt; letter-spacing:.14em; text-transform:uppercase;
             color:#5fbf8f; margin-bottom:6px; }
.mathbox span { font-size:8.4pt; line-height:1.5; color:#c3cedb; }
h3.mt { margin-top:16px; }
.depth { display:grid; grid-template-columns:1fr 1fr 1.35fr; gap:14px; }
.depth h5 { font-size:6.9pt; letter-spacing:.11em; margin:0 0 6px; color:#7b8794; }
.depth table { width:100%; border-collapse:collapse; }
.depth td { padding:0 0 5px; }
.depth .rk { font-size:7.4pt; font-weight:800; color:#c3cedb; width:14px; vertical-align:bottom;
             padding-bottom:7px; }
.cols3 > div:last-child { display:flex; flex-direction:column; }
.grid { width:100%; border-collapse:collapse; }
.grid th { font-size:7pt; letter-spacing:.09em; color:#7b8794; text-align:left; padding:0 6px 6px;
           border-bottom:2px solid #1f2933; white-space:nowrap; }
.grid td { border-bottom:1px solid #cfd6dd; border-right:1px solid #eef1f4; height:0.375in;
           padding:0 6px; }
.grid td:last-child { border-right:0; }
.grid .ser, .grid .num { width:24px; text-align:center; font-weight:800; color:#9aa5b1;
                         font-size:8.4pt; background:#f7f9fa; }
.roster th:nth-child(2) { width:1.7in; }
.boxes { font-size:12pt; color:#b9c2cb; letter-spacing:3px; white-space:nowrap; }
.notecols { display:grid; grid-template-columns:repeat(3,1fr); gap:18px; margin-top:14px; flex:1; }
.notecols h4 { font-size:7.2pt; letter-spacing:.12em; text-transform:uppercase; color:#7b8794;
               margin:0 0 6px; }
.lines { height:1.05in; background:repeating-linear-gradient(to bottom, transparent 0,
         transparent 21px, #e4e8ec 21px, #e4e8ec 22px); }
.tips { display:grid; grid-template-columns:repeat(3,1fr); gap:22px; margin-top:auto;
        margin-bottom:0.32in; border-top:1px solid #e4e8ec; padding-top:11px; }
.tips div { font-size:8.2pt; line-height:1.45; color:#616e7c; }
.tips b { color:#1f2933; }
.tips i { font-style:italic; color:#3e4c59; }
"""


def build():
    return (f'<!doctype html><html><head><meta charset="utf-8">'
            f'<title>Rotation Sheet</title><style>{CSS}</style></head>'
            f'<body>{page1()}{page2()}{page3()}</body></html>')


if __name__ == "__main__":
    open(_OUT / "rotation.html", "w").write(build())
    print("wrote rotation.html")
