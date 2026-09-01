# -*- coding: utf-8 -*-
"""Export the full play library to JSON for the tablet app, in color-side language."""
import json
import pathlib

DIST = pathlib.Path(__file__).resolve().parent / "dist"
DIST.mkdir(exist_ok=True)
from plays import PLAYS, FORMATIONS
from spots import SPOT_MAP, PLAY_TEXT

KID_NAMES = {
    1: "BULLDOZER", 2: "ROCKET", 3: "BOOMERANG", 4: "RACECAR", 5: "LIGHTNING",
    6: "HAMMER", 7: "ZIPPER", 8: "NINJA", 9: "WATERFALL", 10: "MOONSHOT",
    11: "MAGIC TRICK", 12: "SPIDERWEB", 13: "RAINBOW", 14: "STAIRCASE",
    15: "STOP SIGN", 16: "SLINGSHOT", 17: "PINBALL", 18: "ELEVATOR", 19: "SEESAW",
    20: "FIREWORKS", 21: "MOUSETRAP", 22: "PINWHEEL", 23: "DRAWBRIDGE", 24: "FISHHOOK",
}

# how the ball gets there, and to whom. the target is always the thrower's
# FIRST read, or the ball carrier — one rule for every play, no judgement calls.
BALL = {
    1:  ("carry", "H"),   2:  ("carry", "Y"),   3:  ("carry", "QB"),
    4:  ("carry", "H"),   5:  ("pass",  "Y"),   6:  ("pass",  "Z"),
    7:  ("pass",  "Y"),   8:  ("pass",  "C"),   9:  ("pass",  "X"),
    10: ("pass",  "H"),   11: ("pass",  "X"),   12: ("pass",  "Y"),
    13: ("pass",  "Y"),   14: ("pass",  "X"),
    15: ("pass",  "Y"),   16: ("pass",  "Y"),   17: ("pass",  "Z"),
    18: ("pass",  "Z"),   19: ("pass",  "Z"),   20: ("pass",  "Y"),
    21: ("carry", "H"),   22: ("carry", "Y"),   23: ("pass",  "H"),
    24: ("pass",  "Z"),
}

# the four a new team starts with, straight from the playbook's "start here" page
DEFAULT_DECK = [1, 5, 7, 13]

NAME_MAP = {
    "WIDE LEFT":   ("W", "blue",   "WIDE BLUE"),
    "SLOT LEFT":   ("S", "blue",   "SLOT BLUE"),
    "TIGHT LEFT":  ("T", "blue",   "TIGHT BLUE"),
    "TIGHT RIGHT": ("T", "orange", "TIGHT ORANGE"),
    "SLOT RIGHT":  ("S", "orange", "SLOT ORANGE"),
    "WIDE RIGHT":  ("W", "orange", "WIDE ORANGE"),
    "BACK":        ("B", "none",   "BACK"),
    "SNAPPER":     ("SN", "none",  "SNAPPER"),
    "THROWER":     ("QB", "none",  "THROWER"),
}

RECOLOR = [
    ("WIDE LEFT", "WIDE BLUE"), ("SLOT LEFT", "SLOT BLUE"), ("TIGHT LEFT", "TIGHT BLUE"),
    ("TIGHT RIGHT", "TIGHT ORANGE"), ("SLOT RIGHT", "SLOT ORANGE"),
    ("WIDE RIGHT", "WIDE ORANGE"),
    ("right sideline", "orange sideline"), ("left sideline", "blue sideline"),
    ("right flat", "orange flat"), ("left flat", "blue flat"),
    ("MOTION left", "MOTION to blue"), ("SWING left", "SWING to blue"),
    ("CARRY right", "CARRY to orange"), ("Roll to the left", "Roll to the blue side"),
    ("to the right sideline", "to the orange sideline"),
    ("to the left flat", "to the blue flat"),
]


def recolor(text):
    for a, b in RECOLOR:
        text = text.replace(a, b)
    return text


def keys_for_label(label, fm):
    """Resolve a call-strip label to the spot keys it covers.

    Deriving this rather than hand-listing it is what stops the call strip and
    the highlighted routes from drifting apart.
    """
    if label == "EVERYONE ELSE":
        return []
    spots = {k: SPOT_MAP[fm][k][1] for k in FORMATIONS[fm]}
    if label == "BOTH WIDES":
        return [k for k, n in spots.items() if n.startswith("WIDE")]
    parts = [p.strip() for p in label.split("/")]
    out = [k for k, n in spots.items() if n in parts]
    if not out:
        raise ValueError("unmapped call label %r in %s" % (label, fm))
    return out


# --------------------------------------------------- gate: never export a bad play
import check_plays                                                   # noqa: E402
if check_plays.main(quiet=True):
    raise SystemExit("check_plays found errors — fix them before exporting")

out = []
for p in PLAYS:
    fm, num = p["formation"], p["num"]
    spots = {}
    for key, (x, y) in FORMATIONS[fm].items():
        tag, side, spoken = NAME_MAP[SPOT_MAP[fm][key][1]]
        spots[key] = {"tag": tag, "side": side, "name": spoken, "x": x, "y": y}

    txt = PLAY_TEXT[num]
    jobs = sorted({k for lab, _ in txt["calls"] for k in keys_for_label(lab, fm)})
    mode, who = BALL[num]
    assert who in FORMATIONS[fm], "play %d: ball target %s not in %s" % (num, who, fm)

    rec = {
        "id": "p_%02d" % num, "num": num,
        "coachName": p["name"], "kidName": KID_NAMES[num],
        "category": p["category"], "formation": fm,
        "tagline": recolor(p["tagline"]),
        "spots": spots, "paths": p["paths"],
        "calls": [[recolor(a), recolor(b)] for a, b in txt["calls"]],
        "assign": {}, "jobs": jobs, "primary": who,
        "ball": {"mode": mode, "who": who},
    }
    for key in FORMATIONS[fm]:
        full = SPOT_MAP[fm][key][1]
        line = next((t for w, t in txt["assign"] if full in w),
                    "GO. Sprint straight downfield and take your defender with you.")
        rec["assign"][key] = recolor(line)

    idx = next((i for i, pa in enumerate(rec["paths"])
                if pa["who"] == who and (mode != "carry" or pa["type"] in ("run", "route"))), None)
    assert idx is not None, "play %d: no path for ball target %s" % (num, who)
    out.append(rec)

doc = {"schemaVersion": 2, "defaultDeck": ["p_%02d" % n for n in DEFAULT_DECK], "plays": out}
open(DIST / "proto_data.json", "w").write(json.dumps(doc, separators=(",", ":")))
print("wrote proto_data.json —", len(out), "plays,",
      round(len((DIST / "proto_data.json").read_text()) / 1024, 1), "KB")
for r in out:
    print("  %-2d %-12s %-15s ball->%-3s jobs:%d" %
          (r["num"], r["kidName"], r["coachName"], r["primary"], len(r["jobs"])))
