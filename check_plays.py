# -*- coding: utf-8 -*-
"""Legality and legibility checker for the play library.

Hand-drawing routes does not scale. Every bug I found by eye on the first 14
plays is in here as a rule, so play 15 through play 150 get the same read.

Rules fall into three groups:

  LEGAL      things that would draw a flag, or that the league forbids outright
  SAFE       things that get 8-year-olds tangled up — converging routes, an
             arrowhead landing on somebody else's stem
  TEACHABLE  things that break the spot-and-shape vocabulary, which is the part
             a 7-year-old actually holds in his head

Run it with no arguments to check every play. Exit code is non-zero if any
ERROR fired, so it can gate a build.
"""
import math
import sys
from collections import defaultdict

from plays import PLAYS, FORMATIONS
from spots import SPOT_MAP, PLAY_TEXT, SHAPES

EDGE = 15.7          # sideline, yards from the middle of the field
GOAL = 15.0          # goal line, yards downfield
BACKFIELD = -7.0     # deepest a play may go behind the line

SHAPE_WORDS = {s[0] for s in SHAPES}
# words that legitimately open an assignment without being one of the 9 shapes
EXTRA_VERBS = {"MOTION", "FAKE", "SNAP", "BLOCK-FREE"}

MIN_END_GAP = 3.0    # two arrowheads finishing this close read as a collision
MIN_LANE = 1.6       # closest two downfield routes may come to each other
DOWNFIELD = 1.5      # only judge separation past here; everyone is tight at the line


# ------------------------------------------------------------------ geometry
def seg_dist(a, b, c, d):
    """Shortest distance between segment ab and segment cd."""
    def sub(p, q): return (p[0] - q[0], p[1] - q[1])
    def dot(p, q): return p[0] * q[0] + p[1] * q[1]

    u, v, w = sub(b, a), sub(d, c), sub(a, c)
    A, B, C, D, E = dot(u, u), dot(u, v), dot(v, v), dot(u, w), dot(v, w)
    den = A * C - B * B
    sN, sD, tN, tD = 0.0, den or 1.0, 0.0, den or 1.0
    if den < 1e-9:
        sN, sD, tN, tD = 0.0, 1.0, E, C or 1.0
    else:
        sN, tN = B * E - C * D, A * E - B * D
        if sN < 0:   sN, tN, tD = 0.0, E, C or 1.0
        elif sN > sD: sN, tN, tD = sD, E + B, C or 1.0
    if tN < 0:
        tN = 0.0
        sN = min(max(-D, 0.0), A) if A else 0.0
        sD = A or 1.0
    elif tN > tD:
        tN = tD
        sN = min(max(B - D, 0.0), A) if A else 0.0
        sD = A or 1.0
    sc = sN / sD if abs(sD) > 1e-9 else 0.0
    tc = tN / tD if abs(tD) > 1e-9 else 0.0
    dx = w[0] + sc * u[0] - tc * v[0]
    dy = w[1] + sc * u[1] - tc * v[1]
    return math.hypot(dx, dy)


def clip_downfield(pts):
    """Keep only the part of a path at or past DOWNFIELD, so the crowded line
    of scrimmage does not register as everybody colliding with everybody."""
    out = []
    for i in range(len(pts) - 1):
        a, b = pts[i], pts[i + 1]
        if a[1] >= DOWNFIELD or b[1] >= DOWNFIELD:
            out.append((a, b))
    return out


def path_dist(p1, p2):
    best = 99.0
    for a, b in clip_downfield(p1):
        for c, d in clip_downfield(p2):
            best = min(best, seg_dist(a, b, c, d))
    return best


# ------------------------------------------------------- where everyone is, when
# Two routes crossing on paper is not a collision if the players are there at
# different moments — which is most of the time. So the collision rule runs on
# position-at-time, and the pure-geometry rule below it only judges whether the
# DRAWING is readable. Model: every kid runs the same speed (they roughly do),
# each starts at the snap, and pre-snap motion is a head start.
SPEED = 6.0          # yards per second — an honest 8-year-old sprint
STEP = 0.06          # sampling interval, seconds


def arc(pts):
    out, total = [0.0], 0.0
    for i in range(len(pts) - 1):
        total += math.hypot(pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1])
        out.append(total)
    return out, total


def at(pts, cum, dist):
    if dist <= 0:
        return pts[0]
    if dist >= cum[-1]:
        return pts[-1]
    for i in range(len(cum) - 1):
        if dist <= cum[i + 1]:
            span = cum[i + 1] - cum[i]
            f = 0.0 if span < 1e-9 else (dist - cum[i]) / span
            return (pts[i][0] + f * (pts[i + 1][0] - pts[i][0]),
                    pts[i][1] + f * (pts[i + 1][1] - pts[i][1]))
    return pts[-1]


def player_track(paths):
    """Stitch one player's segments into a single walked path, plus how much of
    it he covers before the snap (motion)."""
    order = {"motion": 0, "handoff": 1, "run": 2, "route": 2, "fake": 2, "qb": 2}
    segs = sorted(paths, key=lambda q: order.get(q["type"], 2))
    pts, head = [], 0.0
    for s in segs:
        chunk = list(s["pts"])
        if pts and math.hypot(chunk[0][0] - pts[-1][0], chunk[0][1] - pts[-1][1]) < 0.4:
            chunk = chunk[1:]
        if s["type"] == "motion":
            _, ln = arc(list(s["pts"]))
            head += ln
        pts.extend(chunk)
    return pts, head


def closest_in_time(t1, h1, t2, h2):
    """Smallest gap between two players while the play is live."""
    c1, L1 = arc(t1)
    c2, L2 = arc(t2)
    dur = max((L1 - h1), (L2 - h2)) / SPEED + 0.4
    best, when = 99.0, 0.0
    t = 0.0
    while t <= dur:
        a = at(t1, c1, h1 + SPEED * t)
        b = at(t2, c2, h2 + SPEED * t)
        d = math.hypot(a[0] - b[0], a[1] - b[1])
        if d < best:
            best, when = d, t
        t += STEP
    return best, when


# --------------------------------------------------------------------- rules
class Report:
    def __init__(self):
        self.rows = []

    def add(self, level, num, rule, msg):
        self.rows.append((level, num, rule, msg))

    def err(self, *a):  self.add("ERROR", *a)
    def warn(self, *a): self.add("WARN", *a)

    @property
    def errors(self):
        return [r for r in self.rows if r[0] == "ERROR"]


def check_play(p, rep):
    num, fm = p["num"], p["formation"]
    spots = FORMATIONS[fm]

    # ---- every spot on the field has exactly one job, and it starts where he stands
    seen = defaultdict(list)
    for path in p["paths"]:
        seen[path["who"]].append(path)
    throws = not any(q["type"] in ("run", "handoff") for q in p["paths"])
    for key in spots:
        if key not in seen:
            # the thrower having no drawn path is correct on a pass — he throws
            if key == "QB" and throws:
                continue
            rep.err(num, "SAFE/no-job",
                    "%s has no path — he will stand still while five kids run" % key)
    for key, paths in seen.items():
        if key not in spots:
            rep.err(num, "LEGAL/ghost", "%s is not in formation %s" % (key, fm))
            continue
        first = min(paths, key=lambda q: 0 if q["type"] in ("motion", "handoff") else 1)
        sx, sy = first["pts"][0]
        ox, oy = spots[key]
        if math.hypot(sx - ox, sy - oy) > 0.35:
            rep.err(num, "SAFE/start",
                    "%s's route starts at (%.1f, %.1f) but he lines up at (%.1f, %.1f)"
                    % (key, sx, sy, ox, oy))

    # ---- everything stays on the field
    for path in p["paths"]:
        for (x, y) in path["pts"]:
            if abs(x) > EDGE:
                rep.err(num, "LEGAL/out",
                        "%s runs out of bounds at x=%.1f (sideline is %.1f)" % (path["who"], x, EDGE))
            if y > GOAL + 4:
                rep.warn(num, "field/deep",
                         "%s runs to %.1f yards — past the back of the end zone" % (path["who"], y))
            if y < BACKFIELD:
                rep.err(num, "field/deep",
                        "%s drops to %.1f yards behind the line" % (path["who"], y))

    # ---- ball handling pairs are allowed to be close; nobody else is
    exchange = set()
    for path in p["paths"]:
        if path["type"] in ("handoff", "motion"):
            exchange.add(path["who"])
    carriers = {path["who"] for path in p["paths"] if path["type"] in ("run", "handoff")}
    exempt = exchange | carriers | {"QB", "C"}

    # who is where, when — the collision rule
    tracks = {}
    for key, paths in seen.items():
        if key in spots:
            tracks[key] = player_track(paths)
    keys = sorted(tracks)
    for i in range(len(keys)):
        for j in range(i + 1, len(keys)):
            ka, kb = keys[i], keys[j]
            if ka in exempt and kb in exempt:
                continue
            (pa, ha), (pb, hb) = tracks[ka], tracks[kb]
            d, when = closest_in_time(pa, ha, pb, hb)
            if d < MIN_LANE:
                rep.err(num, "SAFE/collide",
                        "%s and %s are %.1f yd apart %.1fs into the play (needs %.1f) — "
                        "at 8U that is a pileup, and a rub is illegal"
                        % (ka, kb, d, when, MIN_LANE))

    routes = [q for q in p["paths"] if q["type"] in ("route", "run")]
    for i in range(len(routes)):
        for j in range(i + 1, len(routes)):
            a, b = routes[i], routes[j]
            if a["who"] == b["who"]:
                continue
            if a["who"] in exempt and b["who"] in exempt:
                continue

            # an arrowhead landing on somebody else's stem reads as one route
            for lead, other in ((a, b), (b, a)):
                tip = lead["pts"][-1]
                if tip[1] < DOWNFIELD:
                    continue
                segs = clip_downfield(other["pts"])
                if not segs:
                    continue
                dd = min(seg_dist(tip, tip, c, d2) for c, d2 in segs)
                if dd < MIN_END_GAP and lead.get("end", "arrow") == "arrow":
                    rep.warn(num, "SAFE/arrowhead",
                             "%s's arrowhead finishes %.1f yd off %s's line — "
                             "hard to tell whose route is whose"
                             % (lead["who"], dd, other["who"]))

    # ---- the vocabulary: every job must be one of the nine shapes
    txt = PLAY_TEXT.get(num)
    if not txt:
        rep.err(num, "TEACHABLE/text", "no spot-language text — the app has nothing to say")
        return
    for label, job in txt["calls"]:
        words = {w.strip(" .,—-").upper() for w in job.replace("→", " ").split()}
        if not (words & SHAPE_WORDS) and not (words & EXTRA_VERBS):
            if label not in ("THROWER", "SNAPPER"):
                rep.warn(num, "TEACHABLE/vocab",
                         '%s is told "%s" — not one of the nine shapes' % (label, job))

    # ---- the call strip must name real spots in this formation
    names = {SPOT_MAP[fm][k][1] for k in spots}
    for label, _ in txt["calls"]:
        if label in ("EVERYONE ELSE", "BOTH WIDES"):
            continue
        for part in [s.strip() for s in label.split("/")]:
            if part not in names:
                rep.err(num, "LEGAL/label",
                        '%s is not a spot in %s' % (part, fm))

    # ---- category promises
    ball_is_run = any(q["type"] in ("run", "handoff") for q in p["paths"])
    if p["category"] == "NO-RUN ZONE" and ball_is_run:
        rep.err(num, "LEGAL/zone",
                "categorised NO-RUN ZONE but the ball is handed off — that is a dead ball")


def main(quiet=False):
    rep = Report()
    nums = [p["num"] for p in PLAYS]
    dupes = {n for n in nums if nums.count(n) > 1}
    if dupes:
        rep.err(0, "LEGAL/dupe", "play numbers used twice: %s" % sorted(dupes))
    names = [p["name"] for p in PLAYS]
    for n in {x for x in names if names.count(x) > 1}:
        rep.err(0, "TEACHABLE/dupe", "two plays are both called %s" % n)

    for p in sorted(PLAYS, key=lambda q: q["num"]):
        check_play(p, rep)

    by_play = defaultdict(list)
    for level, num, rule, msg in rep.rows:
        by_play[num].append((level, rule, msg))

    title = {p["num"]: p["name"] for p in PLAYS}
    # when called as a build gate, stay silent unless something is actually wrong
    show = not quiet or rep.errors
    for num in (sorted(by_play) if show else []):
        print("\n%-3s %s" % (num, title.get(num, "")))
        for level, rule, msg in by_play[num]:
            print("   %-5s %-20s %s" % (level, rule, msg))

    errs = len(rep.errors)
    warns = len(rep.rows) - errs
    if show:
        print("\n%d plays checked — %d errors, %d warnings" % (len(PLAYS), errs, warns))
    return 1 if errs else 0


if __name__ == "__main__":
    sys.exit(main())
