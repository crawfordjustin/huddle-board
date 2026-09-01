# -*- coding: utf-8 -*-
"""Builds the 8U 6-on-6 flag football playbook HTML (then printed to PDF)."""
import pathlib as _pathlib
import sys as _sys
_sys.path.insert(0, str(_pathlib.Path(__file__).resolve().parent.parent))
_OUT = _pathlib.Path(__file__).resolve().parent.parent / "dist" / "print"
_OUT.mkdir(parents=True, exist_ok=True)

import math
import html as _html
from plays import PLAYS, FORMATIONS, FORMATION_NOTES
from spots import SPOT_MAP, SPOT_GLOSSARY, SHAPES, DEFAULT_RULE, PLAY_TEXT

# ----------------------------------------------------------------- geometry
SX = SY = 15.0            # px per yard
CX = 244.0                # x=0 (the center) in px
LOS = 240.0               # y=0 (line of scrimmage) in px
W, H = 488, 342

def px(x): return CX + x * SX
def py(y): return LOS - y * SY

COLORS = {
    "route": "#1b3c6e",
    "run": "#127a4d",
    "handoff": "#5b6472",
    "motion": "#c9700d",
    "fake": "#127a4d",
    "rush": "#c0392b",
    "field": "#f7f6f2",
    "line": "#cfd6dd",
    "los": "#7b8794",
}

CATEGORY_COLORS = {
    "RUN ZONE": "#127a4d",
    "QUICK GAME": "#1b6ba8",
    "SHOT PLAY": "#8a3ab0",
    "NO-RUN ZONE": "#c9700d",
    "GOAL LINE": "#c0392b",
}


def _trim(p0, p1, dist):
    """Move p0 toward p1 by `dist` pixels."""
    dx, dy = p1[0] - p0[0], p1[1] - p0[1]
    d = math.hypot(dx, dy)
    if d < dist or d == 0:
        return p0
    return (p0[0] + dx / d * dist, p0[1] + dy / d * dist)


def _bar(p_prev, p_end, length=13):
    dx, dy = p_end[0] - p_prev[0], p_end[1] - p_prev[1]
    d = math.hypot(dx, dy) or 1
    nx, ny = -dy / d, dx / d
    h = length / 2
    return (p_end[0] - nx * h, p_end[1] - ny * h,
            p_end[0] + nx * h, p_end[1] + ny * h)


def defs():
    out = ['<defs>']
    for key in ("route", "run", "handoff", "motion", "fake", "rush"):
        c = COLORS[key]
        out.append(
            f'<marker id="ah-{key}" viewBox="0 0 10 10" refX="8.5" refY="5" '
            f'markerWidth="5.2" markerHeight="5.2" orient="auto-start-reverse">'
            f'<path d="M 0 0 L 10 5 L 0 10 z" fill="{c}"/></marker>')
    out.append('</defs>')
    return "".join(out)


def field_bg(show_rush=True):
    s = [f'<rect x="0" y="0" width="{W}" height="{H}" rx="10" fill="{COLORS["field"]}"/>']
    # sidelines
    for xv in (-15.6, 15.6):
        s.append(f'<line x1="{px(xv):.1f}" y1="10" x2="{px(xv):.1f}" y2="{H-10}" '
                 f'stroke="#b9c2cb" stroke-width="2.5"/>')
    # yard lines downfield
    for yv in (5, 10, 15):
        s.append(f'<line x1="{px(-15.6):.1f}" y1="{py(yv):.1f}" x2="{px(15.6):.1f}" y2="{py(yv):.1f}" '
                 f'stroke="{COLORS["line"]}" stroke-width="1.2" stroke-dasharray="5 6"/>')
        s.append(f'<text x="{px(-15.6)+7:.1f}" y="{py(yv)-4:.1f}" font-size="10.5" '
                 f'fill="#9aa5b1" font-weight="600">{yv} yd</text>')
    s.append(f'<line x1="{px(-5.5):.1f}" y1="{py(-5):.1f}" x2="{px(5.5):.1f}" y2="{py(-5):.1f}" '
             f'stroke="{COLORS["line"]}" stroke-width="1" stroke-dasharray="3 5"/>')
    # line of scrimmage
    s.append(f'<line x1="{px(-15.6):.1f}" y1="{LOS:.1f}" x2="{px(15.6):.1f}" y2="{LOS:.1f}" '
             f'stroke="{COLORS["los"]}" stroke-width="2.6"/>')
    return "".join(s)


def rusher_mark():
    rx, ry = px(0), py(7)
    return (f'<g><circle cx="{rx}" cy="{ry}" r="14" fill="{COLORS["field"]}" opacity="0.92"/>'
            f'<g stroke="{COLORS["rush"]}" stroke-width="3" stroke-linecap="round">'
            f'<line x1="{rx-8}" y1="{ry-8}" x2="{rx+8}" y2="{ry+8}"/>'
            f'<line x1="{rx+8}" y1="{ry-8}" x2="{rx-8}" y2="{ry+8}"/></g></g>')


def draw_path(p):
    kind = p["type"]
    pts = [(px(x), py(y)) for x, y in p["pts"]]
    pts[0] = _trim(pts[0], pts[1], 14)
    end = p.get("end", "arrow")
    style = {
        "route": f'stroke="{COLORS["route"]}" stroke-width="2.6"',
        "run": f'stroke="{COLORS["run"]}" stroke-width="3.4"',
        "handoff": f'stroke="{COLORS["handoff"]}" stroke-width="2.2" stroke-dasharray="6 4"',
        "motion": f'stroke="{COLORS["motion"]}" stroke-width="2.2" stroke-dasharray="7 5"',
        "fake": f'stroke="{COLORS["run"]}" stroke-width="2.4" stroke-dasharray="8 5"',
    }[kind]
    marker = "" if end == "bar" else f' marker-end="url(#ah-{kind})"'
    d = " ".join(f"{x:.1f},{y:.1f}" for x, y in pts)
    out = [f'<polyline points="{d}" fill="none" {style} stroke-linejoin="round" '
           f'stroke-linecap="round"{marker}/>']
    if end == "bar":
        x1, y1, x2, y2 = _bar(pts[-2], pts[-1])
        out.append(f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" '
                   f'{style.split(" stroke-dasharray")[0]} stroke-linecap="round"/>')
    if p.get("delay"):
        out.append(f'<text x="{pts[0][0]-20:.1f}" y="{pts[0][1]-4:.1f}" font-size="10" '
                   f'text-anchor="end" fill="{COLORS["route"]}" font-weight="700">count 1-2</text>')
    return "".join(out)


def draw_player(label, x, y, formation=None):
    if formation:
        label = SPOT_MAP[formation][label][0]
    cx_, cy_ = px(x), py(y)
    if label == "QB":
        return (f'<g><rect x="{cx_-12.5:.1f}" y="{cy_-12.5:.1f}" width="25" height="25" rx="5" '
                f'fill="#ffffff" stroke="#1f2933" stroke-width="2.2"/>'
                f'<text x="{cx_:.1f}" y="{cy_+4.2:.1f}" text-anchor="middle" font-size="11.5" '
                f'font-weight="800" fill="#1f2933">QB</text></g>')
    fill = "#eef2f6" if label == "SN" else "#ffffff"
    fs = 12.5 if len(label) < 2 else 10.8
    return (f'<g><circle cx="{cx_:.1f}" cy="{cy_:.1f}" r="12.5" fill="{fill}" '
            f'stroke="#1f2933" stroke-width="2.2"/>'
            f'<text x="{cx_:.1f}" y="{cy_+4.2:.1f}" text-anchor="middle" font-size="{fs}" '
            f'font-weight="800" fill="#1f2933">{label}</text></g>')


def play_svg(play, show_rush=True, vb=None):
    pos = FORMATIONS[play["formation"]]
    vb = vb or f"0 0 {W} {H}"
    s = [f'<svg viewBox="{vb}" xmlns="http://www.w3.org/2000/svg" class="diagram">',
         defs(), field_bg(show_rush)]
    for p in play.get("paths", []):
        s.append(draw_path(p))
    if show_rush:
        s.append(rusher_mark())
    for label, (x, y) in pos.items():
        s.append(draw_player(label, x, y, play["formation"]))
    s.append('</svg>')
    return "".join(s)


def formation_svg(name):
    top = py(6.5)
    return play_svg({"formation": name, "paths": []}, show_rush=False,
                    vb=f"0 {top:.0f} {W} {H - top:.0f}")


# ------------------------------------------------------------------- markup
def esc(t): return _html.escape(t)


def legend_html():
    items = [("route", "Pass route"), ("run", "Ball carrier / run"),
             ("fake", "Fake (no ball)"), ("handoff", "Handoff or pitch"),
             ("motion", "Pre-snap motion")]
    out = ['<div class="legend">']
    for k, label in items:
        dash = ' stroke-dasharray="6 4"' if k in ("handoff", "motion", "fake") else ''
        out.append(
            f'<span class="lg"><svg width="34" height="12" viewBox="0 0 34 12">'
            f'{defs()}<line x1="1" y1="6" x2="27" y2="6" stroke="{COLORS[k]}" stroke-width="2.8"'
            f'{dash} marker-end="url(#ah-{k})"/></svg>{esc(label)}</span>')
    out.append(f'<span class="lg"><svg width="20" height="14" viewBox="0 0 20 14">'
               f'<g stroke="{COLORS["rush"]}" stroke-width="2.6" stroke-linecap="round">'
               f'<line x1="4" y1="3" x2="15" y2="11"/><line x1="15" y1="3" x2="4" y2="11"/>'
               f'</g></svg>Rusher (starts 7 yds back)</span>')
    out.append('<span class="lg"><svg width="26" height="14" viewBox="0 0 26 14">'
               '<line x1="4" y1="7" x2="18" y2="7" stroke="#1b3c6e" stroke-width="2.6"/>'
               '<line x1="18" y1="2" x2="18" y2="12" stroke="#1b3c6e" stroke-width="2.6"/>'
               '</svg>Stop &amp; face the thrower</span>')
    out.append('</div>')
    return "".join(out)


def play_page(play):
    color = CATEGORY_COLORS[play["category"]]
    txt = PLAY_TEXT[play["num"]]
    rows = "".join(
        f'<li><b>{esc(w)}</b>{esc(t)}</li>' for w, t in txt["assign"])
    notes = "".join(f'<li>{esc(n)}</li>' for n in txt["notes"])
    callrow = "".join(f'<span><b>{esc(a)}</b>{esc(b)}</span>' for a, b in txt["calls"])
    return f"""
<section class="page play">
  <header class="playhead" style="--accent:{color}">
    <div class="pnum">{play['num']}</div>
    <div class="ptitle">
      <h2>{esc(play['name'])}</h2>
      <p>{esc(play['tagline'])}</p>
    </div>
    <div class="ptags">
      <span class="tag cat">{esc(play['category'])}</span>
      <span class="tag form">{esc(play['formation'])}</span>
    </div>
  </header>
  <div class="callstrip">{callrow}</div>
  <div class="diagwrap">{play_svg(play)}</div>
  {legend_html()}
  <div class="cols">
    <div class="assign">
      <h3>Who does what</h3>
      <ul class="jobs">{rows}</ul>
    </div>
    <div class="coach">
      <h3>Coaching it</h3>
      <ul>{notes}</ul>
    </div>
  </div>
  <div class="mistake" style="--accent:{color}">
    <b>Watch for</b><span>{esc(txt['mistake'])}</span>
  </div>
  <footer class="pf">8U Flag Football Playbook &middot; 6-on-6 &middot; Play {play['num']} of {len(PLAYS)}</footer>
</section>"""


def cover():
    cats = {}
    for p in PLAYS:
        cats.setdefault(p["category"], []).append(p)
    blocks = ""
    for cat, ps in cats.items():
        lis = "".join(f'<li><b>{p["num"]}</b> {esc(p["name"])}<span>{esc(p["formation"])}</span></li>'
                      for p in ps)
        blocks += (f'<div class="tocblock"><h4 style="--accent:{CATEGORY_COLORS[cat]}">'
                   f'{esc(cat)}</h4><ul>{lis}</ul></div>')
    return f"""
<section class="page cover">
  <div class="coverhero">
    <p class="kicker">6-ON-6 &middot; AGES 8 &amp; UNDER</p>
    <h1>Flag Football<br>Playbook</h1>
    <p class="sub">14 plays &middot; 4 formations &middot; every job named by a spot on the field,
    so any six kids can run any play and everybody gets their snaps</p>
  </div>
  <div class="coverrules">
    <h3>Built around these league rules</h3>
    <div class="rulegrid">
      <div><b>6 on 6, and everybody rotates</b><span>Nobody is X or Y. Jobs belong to spots &mdash;
      SNAPPER, THROWER, WIDE, SLOT, TIGHT, BACK &mdash; and you fill them from whoever is in the
      game.</span></div>
      <div><b>Rusher starts 7 yards back</b><span>You have roughly two to three seconds of clean
      pocket. Quick-game plays get the ball out before the rusher arrives.</span></div>
      <div><b>No blocking or screening</b><span>Nobody blocks and nobody sets a pick. Receivers
      create space by <i>running away</i> from each other, never into each other.</span></div>
      <div><b>Run and no-run zones</b><span>Plays 1&ndash;4 are run-zone calls. Plays 12&ndash;14 are
      designed for no-run zones and the goal line, where you must throw it.</span></div>
    </div>
  </div>
  <div class="starthere">
    <h3>Start here &mdash; four plays for game one</h3>
    <div class="startgrid">
      <div><span class="sn">1</span><b>22 Dive</b><i>Your run</i></div>
      <div><span class="sn">5</span><b>Double Slant</b><i>Your quick pass</i></div>
      <div><span class="sn">7</span><b>Stick</b><i>Your third down</i></div>
      <div><span class="sn">13</span><b>Pylon Fade</b><i>Your goal line</i></div>
    </div>
    <p>Master these four before you add anything else. A team that runs four plays well beats a team
    that runs fourteen plays badly &mdash; and at 8U it is not close. Two companion sheets go with
    this book: a deck of field cards to hold up in the huddle, and a rotation sheet so playing time
    is decided before kickoff instead of during a close game.</p>
  </div>
  <div class="toc">{blocks}</div>
  <footer class="pf">Print double-sided, three-hole punch, and keep it on the sideline.</footer>
</section>"""


MASTER_SPOTS = {"WL": (-13, 0), "SL": (-8, 0), "TL": (-3.5, 0), "SN": (0, 0),
                "TR": (3.5, 0), "SR": (8, 0), "WR": (13, 0), "QB": (0, -3),
                "B": (-2.6, -4.4)}


def spots_svg():
    top = py(3.0)
    s = [f'<svg viewBox="0 {top:.0f} {W} {H - top:.0f}" xmlns="http://www.w3.org/2000/svg" '
         f'class="diagram">', defs(), field_bg(False)]
    for label, (x, y) in MASTER_SPOTS.items():
        s.append(draw_player(label, x, y))
    s.append('</svg>')
    return "".join(s)


def shape_svg(pts, end):
    pad, sc = 15, 9.0
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    w = (max(xs) - min(xs)) * sc + pad * 2
    h = (max(ys) - min(ys)) * sc + pad * 2
    w = max(w, 46)
    def fx(x): return pad + (x - min(xs)) * sc + (w - ((max(xs)-min(xs))*sc + pad*2)) / 2
    def fy(y): return h - pad - (y - min(ys)) * sc
    P = [(fx(a), fy(b)) for a, b in pts]
    st = f'stroke="{COLORS["route"]}" stroke-width="2.6"'
    mk = "" if end == "bar" else ' marker-end="url(#ah-route)"'
    body = [f'<polyline points="{" ".join(f"{a:.1f},{b:.1f}" for a, b in P)}" fill="none" {st} '
            f'stroke-linejoin="round" stroke-linecap="round"{mk}/>']
    if end == "bar":
        x1, y1, x2, y2 = _bar(P[-2], P[-1])
        body.append(f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" {st} '
                    f'stroke-linecap="round"/>')
    body.append(f'<circle cx="{P[0][0]:.1f}" cy="{P[0][1]:.1f}" r="5.5" fill="#fff" '
                f'stroke="#1f2933" stroke-width="2"/>')
    return (f'<svg viewBox="0 0 {w:.0f} {h:.0f}" xmlns="http://www.w3.org/2000/svg" '
            f'class="shapesvg">{defs()}{"".join(body)}</svg>')


def system_page():
    glos = "".join(
        f'<tr><td class="tagcell">{esc(tag)}</td><td class="spotname">{esc(name)}</td>'
        f'<td>{esc(desc)}</td></tr>' for tag, name, desc in SPOT_GLOSSARY)
    shapes = "".join(
        f'<div class="shapecard">{shape_svg(pts, end)}<b>{esc(name)}</b>'
        f'<span>{esc(desc)}</span></div>' for name, desc, pts, end in SHAPES)
    return f"""
<section class="page">
  <header class="secthead"><h2>How this playbook works</h2>
    <p>Nobody in here is a &ldquo;receiver&rdquo; or a &ldquo;running back.&rdquo; Every job belongs
    to a <b>spot on the field</b>, and you put whichever six kids are in the game into those spots.
    That is what lets you rotate freely without re-teaching anything.</p></header>

  <h3>The spots</h3>
  <div class="spotwrap">
    <div class="spotdiag">{spots_svg()}
      <p class="cap">All nine spots shown at once. Only six are on the field at a time &mdash; each
      formation uses the SNAPPER, the THROWER, and four of the rest.</p></div>
    <table class="glos">{glos}</table>
  </div>

  <h3>The shapes &mdash; this is the whole route tree</h3>
  <div class="shapegrid">{shapes}</div>
  <p class="body">Every one of the 14 plays is just these shapes handed out to spots. A shape with a
  number after it means the depth: <b>OUT at 5</b> means run 5 yards, then break to the sideline.</p>

  <div class="defaultbox">
    <b>The default rule</b>
    <span>{esc(DEFAULT_RULE)}</span>
    <i>This is the one that saves you. It means you only have to tell the one or two kids who
    actually matter on that play &mdash; everybody else already knows what to do.</i>
  </div>
  <footer class="pf">8U Flag Football Playbook &middot; 6-on-6</footer>
</section>

<section class="page">
  <header class="secthead"><h2>Calling a play on the field</h2>
    <p>You are allowed out there with them &mdash; use it. The goal is to get six rotating kids
    lined up and moving in under fifteen seconds, without anybody having to remember what letter
    they are.</p></header>
  <div class="two">
    <div>
      <h3>The four-step huddle</h3>
      <ol class="steps">
        <li><b>Name the play, hold up the card.</b> Say the number and the name once
          (&ldquo;Twelve &mdash; Spacing&rdquo;) and show them the picture. At this age the picture
          teaches faster than any words you will say.</li>
        <li><b>Point each kid to a spot.</b> Tap a shoulder, say the spot: &ldquo;Wide left.&rdquo;
          &ldquo;Slot right.&rdquo; &ldquo;You're snapping.&rdquo; Two words each, six kids,
          five seconds.</li>
        <li><b>Give jobs only to the kids who have one.</b> On most plays that is one or two of
          them. Everybody else falls under the default rule and runs GO.</li>
        <li><b>Walk to the ball with them and stand where the play goes.</b> If it is a run, stand
          in the hole. If it is a pass, stand where the ball is going. They will look at you, and
          that is fine &mdash; it is the fastest coaching you will ever do.</li>
      </ol>
      <h3>Say it the same way every time</h3>
      <p class="body">Spot first, then shape, then number: <b>&ldquo;Slot right &mdash; out at
      five.&rdquo;</b> Never vary the order. Kids this age lock onto the pattern long before they
      understand the football.</p>
    </div>
    <div>
      <h3>Why not wristbands?</h3>
      <p class="body">Wristbands work at 12U and up, but at 8U they add a reading step under
      pressure and they still do not tell a kid where to stand. A picture card in your hand does
      both jobs at once and never gets lost in a sleeve.</p>
      <h3>Put the newest kids in the safest spots</h3>
      <table class="script">
        <tr><th>Spot</th><th>How hard is it?</th></tr>
        <tr><td>WIDE LEFT / WIDE RIGHT</td><td>Easiest. Usually just &ldquo;GO.&rdquo;
          Perfect first spot.</td></tr>
        <tr><td>BACK</td><td>Easy on runs, and it is where the ball touches are. Great for
          getting a quiet kid involved.</td></tr>
        <tr><td>SLOT / TIGHT</td><td>Medium. Real route depths and inside traffic.</td></tr>
        <tr><td>SNAPPER</td><td>Harder than it looks. A bad snap kills the play &mdash; train
          three of them.</td></tr>
        <tr><td>THROWER</td><td>Hardest. Train two or three, rotate them by series, not
          by play.</td></tr>
      </table>
      <h3>The one thing to protect</h3>
      <p class="body">Rotation is about playing time, but kids measure fairness in <b>ball
      touches</b>, not snaps. Track carries and targets, not just series. The rotation sheet has a
      column for exactly that.</p>
    </div>
  </div>
  <div class="sayingbox">
    <h3>Five things worth saying out loud</h3>
    <div class="sayings">
      <div><b>&ldquo;Find your spot, not your friend.&rdquo;</b><span>The single most common 8U
        alignment error is copying the kid next to you.</span></div>
      <div><b>&ldquo;You don't have a job &mdash; so GO.&rdquo;</b><span>Reinforces the default rule
        every single time it comes up.</span></div>
      <div><b>&ldquo;Run to the number.&rdquo;</b><span>Depth is the thing that breaks first when a
        new kid rotates in.</span></div>
      <div><b>&ldquo;Turn around and find the ball.&rdquo;</b><span>Say it as they break the huddle,
        not after the incompletion.</span></div>
      <div><b>&ldquo;Great route&rdquo; &mdash; to the kid who didn't get the ball.</b><span>This is
        how you keep nine kids running hard for a ball that goes to one of them.</span></div>
    </div>
  </div>
  <footer class="pf">8U Flag Football Playbook &middot; 6-on-6</footer>
</section>"""


def basics_page():
    return f"""
<section class="page">
  <header class="secthead"><h2>Before the first play</h2></header>
  <div class="two">
    <div>
      <h3>Rotating without losing the plot</h3>
      <p class="body">With 11&ndash;14 kids you are subbing roughly half the field every series.
      Two things make that painless: <b>rotate by series, not by play</b> (a kid who changes spots
      mid-drive is a kid who lines up wrong), and <b>fill the sheet in before kickoff</b> so you are
      never making fairness decisions while losing.</p>
      <p class="body">Keep the THROWER and SNAPPER on their own rotation &mdash; train two or three
      at each and swap them by series. Everything else can be genuinely random.</p>
      <h3>Five rules the kids can remember</h3>
      <ol class="bigrules">
        <li><b>Run your route all the way.</b> The ball may not come to you, but your route is what
          gets someone else open.</li>
        <li><b>Turn and find the ball.</b> When you stop, face the quarterback with your hands up.</li>
        <li><b>Never run into a teammate.</b> No screening is allowed, and it also gets you both
          covered by one defender.</li>
        <li><b>Two seconds.</b> Quarterback, if nobody is open in two seconds, throw it away toward
          the sideline. A punt beats an interception.</li>
        <li><b>Protect your flags.</b> Ball carriers: no spinning, no stiff-arming, no guarding the
          flag with your hand. Run to open grass.</li>
      </ol>
      <h3>A 60-minute practice</h3>
      <table class="script">
        <tr><th>Time</th><th>What you do</th></tr>
        <tr><td>0:00 &ndash; 0:10</td><td>Warm up, then snap-and-catch in pairs</td></tr>
        <tr><td>0:10 &ndash; 0:20</td><td>Flag pulling: two lines, live one-on-one</td></tr>
        <tr><td>0:20 &ndash; 0:35</td><td>Routes on cones at 3, 5, and 8 yards. No defense</td></tr>
        <tr><td>0:35 &ndash; 0:50</td><td>Run this week's plays against air, then vs. a walk-through defense</td></tr>
        <tr><td>0:50 &ndash; 1:00</td><td>Live reps of the four game-day calls, then finish on a completion</td></tr>
      </table>
    </div>
    <div>
      <h3>How to call a game</h3>
      <p class="body">Do not try to use all fourteen plays. Pick four for the first game: one run
      (<b>22 Dive</b>), one quick pass (<b>Double Slant</b>), one out (<b>Stick</b>), and one goal-line
      call (<b>Pylon Fade</b>). Add a play a week once those four are automatic.</p>
      <h3>A simple script that works</h3>
      <table class="script">
        <tr><th>Situation</th><th>Call</th></tr>
        <tr><td>1st down, run zone</td><td>22 Dive (1) or Jet Sweep (2)</td></tr>
        <tr><td>2nd and short</td><td>Stick (7) or Pitch Right (4)</td></tr>
        <tr><td>2nd and long</td><td>Flood (9)</td></tr>
        <tr><td>3rd and short</td><td>Stick (7) &mdash; set the break past the sticks</td></tr>
        <tr><td>3rd and long</td><td>Spacing (12) or Smash (6)</td></tr>
        <tr><td>Rusher is winning</td><td>Double Slant (5)</td></tr>
        <tr><td>Defense chases motion</td><td>Counter Keep (3)</td></tr>
        <tr><td>Defense crowds the middle</td><td>Pitch Right (4) or Post / Wheel (10)</td></tr>
        <tr><td>Run has been working</td><td>Play-Action Cross (11)</td></tr>
        <tr><td>No-run zone</td><td>Spacing (12)</td></tr>
        <tr><td>Inside the 5</td><td>Pylon Fade (13)</td></tr>
        <tr><td>Extra point</td><td>Triple Out (14)</td></tr>
        <tr><td>Need a change-up</td><td>Snapper Delay (8) &mdash; once per game</td></tr>
      </table>
      <h3>Practice this, not that</h3>
      <p class="body">Fifteen minutes of snap-and-catch beats an hour of new plays. The three drills
      that move the needle at this age: <b>snap to thrower</b> (50 reps), <b>route depth on a marked
      line</b> (cones at 3, 5, and 8 yards), and <b>catch and turn upfield</b>. Everything in this
      book fails if the snap is bad or the route is the wrong depth.</p>
      <p class="body small">One more thing: read your own league's rulebook before the first game.
      Rules on thrower runs, snapper eligibility, motion, and zone sizes vary between leagues, and a few
      plays here have notes about exactly that.</p>
    </div>
  </div>
  <footer class="pf">8U Flag Football Playbook &middot; 6-on-6</footer>
</section>"""


def formations_page():
    cards = ""
    order = ["QB", "C", "X", "Y", "Z", "H"]
    for name in FORMATIONS:
        chips = "".join(
            f'<i>{esc(SPOT_MAP[name][k][1])}</i>' for k in order if k in FORMATIONS[name])
        cards += (f'<div class="fcard"><h4>{esc(name)}</h4>{formation_svg(name)}'
                  f'<div class="chips">{chips}</div>'
                  f'<p>{esc(FORMATION_NOTES[name])}</p></div>')
    return f"""
<section class="page">
  <header class="secthead"><h2>The four formations</h2>
    <p>Everything in this playbook comes out of these four looks. Each one uses the SNAPPER, the
    THROWER, and four of the other spots &mdash; the spots each formation needs are listed under
    its picture. Teach the formations first: kids who line up correctly are already halfway to
    running the play right.</p></header>
  <div class="fgrid">{cards}</div>
  <div class="two mirrorbox">
    <div>
      <h3>Every formation flips</h3>
      <p class="body">Each of these four looks has a mirror image, and so does every play in this book.
      Call &ldquo;Trips <b>Right</b> &mdash; Flood&rdquo; and the whole thing runs to the other side of
      the field. That turns 14 plays into 28 without teaching anything new.</p>
      <p class="body">Flip toward the wide side of the field, or away from the defense's best player.
      At 8U, one kid is usually the entire defense &mdash; find him and run away from him.</p>
    </div>
    <div>
      <h3>Getting lined up</h3>
      <ul class="bigrules">
        <li><b>Line up fast.</b> Sprint to your spot. Time spent standing around is time the defense
          uses to figure you out.</li>
        <li><b>Know your spot by name, not by neighbor.</b> If a teammate lines up wrong, do not follow
          him.</li>
        <li><b>Only one player moves before the snap.</b> Everyone else must be set and still.</li>
        <li><b>Check the sideline.</b> Receivers on the outside need at least a couple of yards of
          room to run their route without stepping out.</li>
      </ul>
    </div>
  </div>
  <footer class="pf">8U Flag Football Playbook &middot; 6-on-6</footer>
</section>"""


def callsheet():
    rows = ""
    for p in PLAYS:
        rows += (f'<tr><td class="n" style="--accent:{CATEGORY_COLORS[p["category"]]}">{p["num"]}</td>'
                 f'<td class="nm">{esc(p["name"])}</td>'
                 f'<td>{esc(p["formation"])}</td>'
                 f'<td>{esc(p["category"])}</td>'
                 f'<td class="tl">{esc(p["tagline"])}</td></tr>')
    return f"""
<section class="page">
  <header class="secthead"><h2>Game plan sheet</h2>
    <p>The field cards are what you carry in the huddle &mdash; this page is for planning. Before
    the game, circle the five or six plays you actually intend to call, and pull only those cards
    off the ring. A short deck you can find fast beats a complete one you have to search.</p></header>
  <table class="sheet">
    <tr><th>#</th><th>Play</th><th>Formation</th><th>Type</th><th>What it is</th></tr>
    {rows}
  </table>
  <div class="notesbox"><h4>Game notes</h4><div class="lines"></div></div>
  <footer class="pf">8U Flag Football Playbook &middot; 6-on-6</footer>
</section>"""


CSS = """
@page { size: Letter; margin: 0; }
* { box-sizing: border-box; }
body { margin:0; font-family: "Helvetica Neue", Helvetica, Arial, sans-serif;
       color:#1f2933; -webkit-print-color-adjust:exact; print-color-adjust:exact; }
.page { width:8.5in; height:11in; padding:0.46in 0.5in 0.4in; page-break-after:always;
        position:relative; overflow:hidden; display:flex; flex-direction:column; }
.page:last-child { page-break-after:auto; }
.pf { position:absolute; bottom:0.26in; left:0.5in; right:0.5in; font-size:7.6pt; color:#9aa5b1;
      border-top:1px solid #e4e8ec; padding-top:5px; letter-spacing:.02em; }

/* cover */
.cover { background:#0f1b2b; color:#fff; }
.cover .pf { color:#5b6b80; border-top-color:#22344a; }
.coverhero { padding-top:0.35in; }
.kicker { font-size:10pt; letter-spacing:.22em; color:#5fbf8f; font-weight:800; margin:0 0 12px; }
.cover h1 { font-size:46pt; line-height:1.02; margin:0 0 14px; letter-spacing:-.02em; }
.cover .sub { font-size:11.5pt; color:#a8b6c6; margin:0; max-width:5.6in; line-height:1.5; }
.coverrules { margin-top:auto; border-top:1px solid #22344a; padding-top:14px; }
.coverrules h3 { font-size:9pt; letter-spacing:.16em; color:#5fbf8f; margin:0 0 12px;
                 text-transform:uppercase; }
.rulegrid { display:grid; grid-template-columns:1fr 1fr; gap:12px 22px; }
.rulegrid div { font-size:8.8pt; line-height:1.45; color:#a8b6c6; }
.rulegrid b { display:block; color:#fff; font-size:10pt; margin-bottom:3px; }
.starthere { margin-top:auto; border-top:1px solid #22344a; padding-top:14px; }
.starthere h3 { font-size:9pt; letter-spacing:.16em; color:#5fbf8f; margin:0 0 12px;
                text-transform:uppercase; }
.startgrid { display:grid; grid-template-columns:repeat(4,1fr); gap:10px; }
.startgrid div { background:#17263a; border-radius:8px; padding:11px 12px; }
.startgrid .sn { display:inline-block; background:#5fbf8f; color:#0f1b2b; font-weight:800;
                 font-size:8.5pt; width:19px; height:19px; line-height:19px; text-align:center;
                 border-radius:5px; margin-bottom:7px; }
.startgrid b { display:block; color:#fff; font-size:11pt; }
.startgrid i { display:block; color:#7d90a6; font-size:8pt; font-style:normal; margin-top:2px; }
.starthere p { font-size:8.8pt; color:#a8b6c6; line-height:1.5; margin:12px 0 0; max-width:6.2in; }
.toc { margin-top:auto; padding-top:16px; display:grid; grid-template-columns:repeat(5,1fr);
       gap:14px; border-top:1px solid #22344a; }
.tocblock h4 { font-size:7.6pt; letter-spacing:.1em; margin:0 0 8px; color:var(--accent);
               text-transform:uppercase; border-bottom:2px solid var(--accent); padding-bottom:5px; }
.tocblock ul { list-style:none; margin:0; padding:0; }
.tocblock li { font-size:8.4pt; color:#dbe3ec; margin-bottom:6px; line-height:1.25; }
.tocblock li b { color:#5fbf8f; margin-right:4px; }
.tocblock li span { display:block; font-size:6.8pt; color:#6d7f93; letter-spacing:.04em; }

/* section pages */
.secthead { border-bottom:2.5px solid #1f2933; padding-bottom:9px; margin-bottom:16px; }
.secthead h2 { font-size:21pt; margin:0; letter-spacing:-.01em; }
.secthead p { font-size:9.2pt; color:#616e7c; margin:7px 0 0; max-width:6.4in; line-height:1.45; }
h3 { font-size:8.4pt; letter-spacing:.13em; text-transform:uppercase; color:#127a4d;
     margin:16px 0 8px; }
h3:first-child { margin-top:0; }
.two { display:grid; grid-template-columns:1fr 1fr; gap:26px; }
.body { font-size:9pt; line-height:1.52; color:#3e4c59; margin:0 0 9px; }
.body.small { font-size:8.2pt; color:#616e7c; }
table { width:100%; border-collapse:collapse; }
.spots td, .script td, .script th { font-size:8.6pt; padding:5px 7px; border-bottom:1px solid #e4e8ec;
     line-height:1.4; vertical-align:top; }
.script th { text-align:left; font-size:7.4pt; letter-spacing:.1em; text-transform:uppercase;
     color:#7b8794; border-bottom:2px solid #1f2933; }
.script td:first-child { color:#616e7c; width:44%; }
.script td:last-child { font-weight:600; }
td.who { font-weight:800; color:#127a4d; width:52px; white-space:nowrap; }
.bigrules { margin:0; padding-left:17px; }
.bigrules li { font-size:8.8pt; line-height:1.45; margin-bottom:7px; color:#3e4c59; }
.bigrules b { color:#1f2933; }

/* formations */
.fgrid { display:grid; grid-template-columns:1fr 1fr; gap:14px 20px; }
.fcard { border:1px solid #e4e8ec; border-radius:9px; padding:10px 12px 12px; }
.fcard h4 { font-size:10.5pt; margin:0 0 6px; letter-spacing:.02em; }
.fcard svg { width:100%; height:auto; display:block; }
.chips { display:flex; flex-wrap:wrap; gap:4px; margin:8px 0 0; }
.chips i { font-style:normal; font-size:6.9pt; letter-spacing:.05em; font-weight:700;
           background:#eef2f6; color:#3e4c59; border-radius:4px; padding:3px 6px; }
.fcard p { font-size:8pt; line-height:1.42; color:#616e7c; margin:6px 0 0; }

.mirrorbox { margin-top:auto; margin-bottom:0.36in; border-top:1px solid #e4e8ec;
             padding-top:14px; gap:26px; }

/* play pages */
.playhead { display:flex; align-items:flex-start; gap:13px;
            border-bottom:3px solid var(--accent); padding-bottom:10px; }
.pnum { background:var(--accent); color:#fff; font-size:19pt; font-weight:800; width:44px;
        height:44px; border-radius:9px; display:flex; align-items:center; justify-content:center;
        flex:none; }
.ptitle { flex:1; }
.ptitle h2 { font-size:23pt; margin:0; letter-spacing:-.015em; line-height:1.05; }
.ptitle p { font-size:9.4pt; color:#616e7c; margin:4px 0 0; }
.ptags { text-align:right; flex:none; padding-top:2px; }
.tag { display:block; font-size:7.2pt; letter-spacing:.1em; font-weight:800; padding:3px 8px;
       border-radius:4px; margin-bottom:4px; }
.tag.cat { background:var(--accent); color:#fff; }
.tag.form { background:#eef2f6; color:#52606d; }
.callstrip { display:flex; flex-wrap:wrap; gap:6px; margin:11px 0 2px; }
.callstrip span { flex:1 1 auto; background:#f2f5f7; border-radius:6px; padding:6px 10px;
                  font-size:8.6pt; color:#3e4c59; white-space:nowrap; }
.callstrip b { display:block; font-size:7.2pt; letter-spacing:.09em; color:#127a4d;
               margin-bottom:2px; }
.diagwrap { margin:9px auto 0; width:5.5in; }
.diagram { width:100%; height:auto; display:block; }
.legend { display:flex; flex-wrap:wrap; gap:5px 14px; justify-content:center; margin:9px 0 11px;
          font-size:7.4pt; color:#52606d; font-weight:600; }
.lg { display:inline-flex; align-items:center; gap:5px; }
.cols { display:grid; grid-template-columns:1.15fr 1fr; gap:20px; border-top:1px solid #e4e8ec;
        padding-top:12px; }
.jobs { list-style:none; margin:0; padding:0; }
.jobs li { font-size:8.4pt; line-height:1.42; color:#3e4c59; padding:0 0 7px;
           margin-bottom:7px; border-bottom:1px solid #eef1f4; }
.jobs li:last-child { border-bottom:0; }
.jobs b { display:block; font-size:7.6pt; letter-spacing:.09em; color:#127a4d; margin-bottom:2px; }
.coach ul { margin:0; padding-left:15px; }
.coach li { font-size:8.4pt; line-height:1.45; color:#3e4c59; margin-bottom:8px; }

.mistake { margin-top:auto; margin-bottom:0.34in; border-left:4px solid var(--accent);
           background:#f7f9fa; border-radius:0 8px 8px 0; padding:11px 14px; display:flex;
           gap:14px; align-items:baseline; }
.mistake b { font-size:7.6pt; letter-spacing:.13em; text-transform:uppercase; color:var(--accent);
             flex:none; }
.mistake span { font-size:9pt; line-height:1.45; color:#3e4c59; }

.spotwrap { display:grid; grid-template-columns:1.15fr 1fr; gap:20px; align-items:start; }
.spotdiag svg { width:100%; height:auto; display:block; }
.cap { font-size:7.8pt; color:#7b8794; line-height:1.4; margin:7px 0 0; }
.glos td { font-size:8.4pt; padding:4px 6px; border-bottom:1px solid #eef1f4; line-height:1.35;
           vertical-align:top; }
.tagcell { font-weight:800; color:#fff; background:#1f2933; text-align:center; width:26px;
           border-radius:4px; font-size:7.6pt; }
.spotname { font-weight:800; white-space:nowrap; width:74px; }
.shapegrid { display:grid; grid-template-columns:repeat(5,1fr); gap:10px; margin-bottom:10px; }
.shapecard { border:1px solid #e4e8ec; border-radius:8px; padding:8px 7px 9px; text-align:center; }
.shapesvg { height:64px; width:auto; max-width:100%; display:block; margin:0 auto 5px; }
.shapecard b { display:block; font-size:9pt; letter-spacing:.04em; }
.shapecard span { display:block; font-size:6.9pt; color:#7b8794; line-height:1.32; margin-top:3px; }
.defaultbox { margin-top:auto; margin-bottom:0.36in; background:#0f1b2b; color:#fff;
              border-radius:10px; padding:15px 18px; }
.defaultbox b { display:block; font-size:8pt; letter-spacing:.15em; text-transform:uppercase;
                color:#5fbf8f; margin-bottom:7px; }
.defaultbox span { display:block; font-size:12pt; line-height:1.4; font-weight:600; }
.defaultbox i { display:block; font-style:normal; font-size:8.6pt; color:#a8b6c6; margin-top:9px;
                line-height:1.45; }
.steps { margin:0; padding-left:17px; }
.steps li { font-size:8.8pt; line-height:1.45; margin-bottom:9px; color:#3e4c59; }
.steps b { color:#1f2933; }

.sayingbox { margin-top:auto; margin-bottom:0.36in; border-top:1px solid #e4e8ec; padding-top:14px; }
.sayings { display:grid; grid-template-columns:repeat(5,1fr); gap:12px; }
.sayings div { font-size:7.8pt; line-height:1.4; }
.sayings b { display:block; color:#1f2933; font-size:8.4pt; margin-bottom:4px; line-height:1.3; }
.sayings span { color:#7b8794; }

/* call sheet */
.sheet td, .sheet th { font-size:8.6pt; padding:6px 7px; border-bottom:1px solid #e4e8ec;
                       text-align:left; vertical-align:middle; }
.sheet th { font-size:7.4pt; letter-spacing:.1em; text-transform:uppercase; color:#7b8794;
            border-bottom:2px solid #1f2933; }
.sheet td.n { font-weight:800; color:#fff; background:var(--accent); text-align:center; width:26px;
              border-radius:4px; }
.sheet td.nm { font-weight:800; }
.sheet td.tl { color:#616e7c; font-size:8pt; }
.notesbox { margin-top:20px; margin-bottom:0.36in; border:1px solid #e4e8ec; border-radius:9px; padding:11px 13px 12px; flex:1; display:flex; flex-direction:column; }
.notesbox h4 { font-size:7.6pt; letter-spacing:.12em; text-transform:uppercase; color:#7b8794;
               margin:0 0 8px; }
.lines { flex:1; background:repeating-linear-gradient(to bottom, transparent 0,
         transparent 26px, #e4e8ec 26px, #e4e8ec 27px); }
"""


def build():
    body = cover() + system_page() + basics_page() + formations_page()
    body += "".join(play_page(p) for p in PLAYS)
    body += callsheet()
    return (f'<!doctype html><html><head><meta charset="utf-8">'
            f'<title>8U Flag Football Playbook</title><style>{CSS}</style></head>'
            f'<body>{body}</body></html>')


if __name__ == "__main__":
    with open(_OUT / "playbook.html", "w") as f:
        f.write(build())
    print("wrote playbook.html")
