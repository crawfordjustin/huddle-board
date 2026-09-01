# -*- coding: utf-8 -*-
"""Play definitions for the 8U 6-on-6 flag football playbook.

Coordinate system (yards):
  x = yards left/right of the center (negative = offense's left)
  y = yards downfield from the line of scrimmage (negative = backfield)
"""

# ---------------------------------------------------------------- formations
FORMATIONS = {
    "TWINS RIGHT": {
        "C": (0, 0), "QB": (0, -3), "H": (-2.6, -4.4),
        "X": (-9, 0), "Y": (6, 0), "Z": (11, 0),
    },
    "TRIPS LEFT": {
        "C": (0, 0), "QB": (0, -3), "H": (7, 0),
        "X": (-11, 0), "Y": (-7.5, 0), "Z": (-4, 0),
    },
    "SPREAD": {
        "C": (0, 0), "QB": (0, -3),
        "X": (-10, 0), "Y": (-5, 0), "H": (5, 0), "Z": (10, 0),
    },
    "ACE": {
        "C": (0, 0), "QB": (0, -3), "H": (-1.5, -5),
        "X": (-9, 0), "Y": (3, 0), "Z": (9, 0),
    },
}

FORMATION_NOTES = {
    "TWINS RIGHT": "Your base look. A ball carrier in the backfield plus two receivers stacked to "
                   "the right, so you can run it or throw a two-man combo out of the same picture.",
    "TRIPS LEFT": "Three to the left, one to the right. Overloads one side so the defense has to "
                  "declare who covers whom. Best for flood and staircase-out concepts.",
    "SPREAD": "Two each side, nobody in the backfield. Widest possible spacing — the defense cannot "
              "double anybody. This is your must-pass formation.",
    "ACE": "Tighter set with a BACK behind the thrower. Best run look and best goal-line look, "
           "because everybody is close enough to the ball to get there fast.",
}

# ---------------------------------------------------------------------- plays
# path types: route | run | handoff | motion | qb
# end styles: arrow | bar | none

PLAYS = [
    # ============================================================ RUN ZONE
    {
        "num": 1,
        "mistake": 'H bouncing outside instead of hitting the hole. Straight ahead beats pretty, every single time.',
        "name": "22 DIVE",
        "formation": "TWINS RIGHT",
        "category": "RUN ZONE",
        "tagline": "Simple downhill handoff. Your first-down play.",
        "paths": [
            {"who": "QB", "type": "handoff", "pts": [(0, -3), (-1.4, -3.7)]},
            {"who": "H", "type": "run", "pts": [(-2.6, -4.4), (-1.4, -3.7), (2.2, 0.5), (3, 8)]},
            {"who": "X", "type": "route", "pts": [(-9, 0), (-9, 10)]},
            {"who": "Y", "type": "route", "pts": [(6, 0), (6, 9)]},
            {"who": "Z", "type": "route", "pts": [(11, 0), (11, 10)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (-2.5, 5)]},
        ],
        "assign": [
            ("QB", "Catch the snap, turn immediately, hand the ball to H with both hands. Do not watch him run."),
            ("H", "Take one step toward the QB, take the ball, run north-south past the center's right hip. Get vertical."),
            ("X / Y / Z", "Sprint straight downfield 9-10 yards. You are not blocking — you are taking your defender away from the run."),
            ("C", "Snap it, then release straight up the field. Stay out of H's running lane."),
        ],
        "notes": [
            "Call this on 1st down in a run zone to set the tone and get the offense moving forward.",
            "The rusher starts 7 yards back, so a downhill run hits the line before he can get there. Speed matters more than fancy footwork.",
            "Coach the receivers hard on this: at 8U they want to stand and watch. Their sprint downfield is what opens the hole.",
        ],
    },
    {
        "num": 2,
        "mistake": 'Y slowing down to take the handoff. If he catches it standing still, the play is already dead.',
        "name": "JET SWEEP",
        "formation": "TRIPS LEFT",
        "category": "RUN ZONE",
        "tagline": "Full-speed motion handoff to the edge. Beats slow defenses every time.",
        "paths": [
            {"who": "Y", "type": "motion", "pts": [(-7.5, 0), (-4.5, -1.6), (-1.5, -1.6)]},
            {"who": "QB", "type": "handoff", "pts": [(0, -3), (-1, -1.8)]},
            {"who": "Y", "type": "run", "pts": [(-1.5, -1.6), (3, -1.4), (8, -0.6), (11.5, 6.5)]},
            {"who": "X", "type": "route", "pts": [(-11, 0), (-11, 8)]},
            {"who": "Z", "type": "route", "pts": [(-4, 0), (-5.5, 9)]},
            {"who": "H", "type": "route", "pts": [(7, 0), (6, 9)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 5)]},
        ],
        "assign": [
            ("Y", "Start moving on the coach's signal. Be at FULL SPEED when you pass the QB. Take the ball and keep running to the sideline, then turn up."),
            ("QB", "Catch the snap and hold the ball out at belly height. Y takes it from you — do not throw it or push it at him."),
            ("H", "Sprint straight downfield. Take the corner with you and leave the sideline empty for Y."),
            ("Z / X", "Sprint downfield. Take your defender with you."),
            ("C", "Snap and release straight up the middle."),
        ],
        "notes": [
            "Timing is everything: Y must already be moving before the snap. Practice the motion 20 times before you ever practice the handoff.",
            "If the defense starts chasing the motion, that is when you call Play 3 (QB Counter) — the fake becomes the whole play.",
            "Legal check: only one player may be in motion at the snap and he must be moving parallel to or away from the line, not toward it.",
        ],
    },
    {
        "num": 3,
        "mistake": 'The QB pulling it out too early. Let H clear the fake, count two, then go.',
        "name": "COUNTER KEEP",
        "formation": "SPREAD",
        "category": "RUN ZONE",
        "tagline": "Fake the sweep one way, keep it the other. The counter-punch to Jet Sweep.",
        "paths": [
            {"who": "H", "type": "motion", "pts": [(5, 0), (2.4, -2.0)]},
            {"who": "H", "type": "fake", "pts": [(2.4, -2.0), (-2, -2.2), (-6.5, -2.0)]},
            {"who": "QB", "type": "run", "pts": [(0, -3), (2, -3.9), (5.5, -3.4), (7.8, 0.5), (8.2, 7)]},
            {"who": "X", "type": "route", "pts": [(-10, 0), (-10, 8)]},
            {"who": "Y", "type": "route", "pts": [(-5, 0), (-5, 8)]},
            {"who": "Z", "type": "route", "pts": [(10, 0), (10, 9)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (-1, 6)]},
        ],
        "assign": [
            ("H", "Motion left and run the sweep fake all the way to the sideline. SELL IT — arms up like you have the ball."),
            ("QB", "Fake the handoff to H, then step back to the right and run. Two counts of patience, then go."),
            ("Z", "Sprint downfield and to the sideline to clear the QB's running lane."),
            ("X / Y", "Sprint straight downfield 8 yards."),
            ("C", "Snap and release up the middle, away from the QB's path."),
        ],
        "notes": [
            "Only call this after the defense has seen Jet Sweep at least twice. The fake does not work on a defense that has not been burned yet.",
            "Rule check: some leagues do not let the QB run the ball. If yours does not, run this exact play with H taking a real handoff going right instead — the fake is the H motion left with a second back, or simply pitch it.",
            "Tell the QB the sideline is a friend. Get what is there and get out of bounds.",
        ],
    },
    {
        "num": 4,
        "mistake": 'Pitching the ball behind H. Toss it to where he is going, not where he is.',
        "name": "PITCH RIGHT",
        "formation": "ACE",
        "category": "RUN ZONE",
        "tagline": "Get outside fast. The rusher is 7 yards away — beat him to the edge.",
        "paths": [
            {"who": "QB", "type": "handoff", "pts": [(0, -3), (3.5, -4.2)]},
            {"who": "H", "type": "run", "pts": [(-1.5, -5), (2, -4.5), (6, -3), (9.5, 1), (10.5, 8)]},
            {"who": "Z", "type": "route", "pts": [(9, 0), (12.6, 3), (13.4, 8)]},
            {"who": "Y", "type": "route", "pts": [(3, 0), (5, 9)]},
            {"who": "X", "type": "route", "pts": [(-9, 0), (-9, 8)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (-1, 6)]},
        ],
        "assign": [
            ("QB", "Catch the snap and pitch it underhanded, out in front of H so he runs onto it. Soft toss, chest high."),
            ("H", "Start running to the sideline BEFORE you catch it. Catch the ball on the move, get to the edge, then turn up the field."),
            ("Z", "Release straight for the sideline, then turn up. Take the corner with you and leave the edge empty for H."),
            ("Y", "Sprint downfield at an angle toward the sideline."),
            ("X / C", "Sprint downfield on the backside."),
        ],
        "notes": [
            "This is a race to the sideline. If H tries to turn upfield too early he runs into traffic — coach him to get width first.",
            "If the pitch is dropped it is a live ball. Practice it slow before you practice it fast.",
            "Great call when the defense has been crowding the middle to stop 22 Dive.",
        ],
    },
    # ============================================================ QUICK GAME
    {
        "num": 5,
        "mistake": 'Both slants breaking at the same depth. Y at 4, H at 6 — say the numbers out loud in practice.',
        "name": "DOUBLE SLANT",
        "formation": "SPREAD",
        "category": "QUICK GAME",
        "tagline": "Ball out in two seconds. Your answer to a hard rush.",
        "paths": [
            {"who": "X", "type": "route", "pts": [(-10, 0), (-10, 5.5)], "end": "bar"},
            {"who": "Y", "type": "route", "pts": [(-5, 0), (-4, 1.5), (-0.5, 4.5)]},
            {"who": "H", "type": "route", "pts": [(5, 0), (4, 2), (0.5, 6.5)]},
            {"who": "Z", "type": "route", "pts": [(10, 0), (10, 5.5)], "end": "bar"},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 2.2)], "end": "bar"},
        ],
        "assign": [
            ("Y", "Three hard steps upfield, then break inside at an angle. Look for the ball right away — it is coming at 4 yards."),
            ("H", "Same slant, but take FIVE steps first so you cross behind Y at 6 yards. Never run at the same depth as Y."),
            ("X / Z", "Run 5 yards, stop, turn back to the QB and show your hands. Sit down in the open grass."),
            ("QB", "Pick your slant before the snap. Catch, one step, throw. Do not hold this ball."),
            ("C", "Snap, release 2 yards straight up, and turn around. You are the emergency dump-off."),
        ],
        "notes": [
            "This is the play you call when the rusher is getting home. It should be out of the QB's hand in about two seconds.",
            "Throw it low and in front. A ball at the receiver's chest is a catch; a ball behind him is an interception.",
            "The staggered depths are on purpose — no blocking or screening is allowed, so receivers must never run into each other's defender.",
        ],
    },
    {
        "num": 6,
        "mistake": 'Z drifting past 5 yards. His whole job is to stop short and be the easy completion.',
        "name": "SMASH",
        "formation": "TWINS RIGHT",
        "category": "QUICK GAME",
        "tagline": "High-low the corner. One defender, two receivers, he cannot be right.",
        "paths": [
            {"who": "Z", "type": "route", "pts": [(11, 0), (11, 5)], "end": "bar"},
            {"who": "Y", "type": "route", "pts": [(6, 0), (6, 7), (12, 12)]},
            {"who": "X", "type": "route", "pts": [(-9, 0), (-9, 7), (-4, 7)]},
            {"who": "H", "type": "route", "pts": [(-2.6, -4.4), (-5, -2.4), (-8, -1.8)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 4)], "end": "bar"},
        ],
        "assign": [
            ("Z", "Run 5 yards, stop, come back one step toward the QB. You are the easy completion."),
            ("Y", "Sprint straight up 7 yards, then break out toward the corner of the field. Run away from the sideline defender."),
            ("X", "Run 7 yards and break in across the middle."),
            ("H", "Swing out to the left flat as the safety valve."),
            ("QB", "Look at the deep defender on the right. If he stays deep, throw the Z hitch. If he comes up, throw Y over his head."),
        ],
        "notes": [
            "This is a real read, so it is a good one for a returning QB. If your QB is new, just tell him 'throw the hitch to Z unless nobody is near Y.'",
            "The hitch alone is a 5-yard gain. Take it. Five-yard gains win 8U games.",
            "Y must run his corner route AWAY from Z, never through him — no screening allowed.",
        ],
    },
    {
        "num": 7,
        "mistake": 'Y rounding off his break. Plant hard and run flat to the sideline.',
        "name": "STICK",
        "formation": "TRIPS LEFT",
        "category": "QUICK GAME",
        "tagline": "Two easy answers on the same side. Almost impossible to cover.",
        "paths": [
            {"who": "X", "type": "route", "pts": [(-11, 0), (-13, 3), (-13.6, 10)]},
            {"who": "Y", "type": "route", "pts": [(-7.5, 0), (-8, 4.5), (-11.4, 4.5)]},
            {"who": "Z", "type": "route", "pts": [(-4, 0), (-2, 1.5), (4, 2.5)]},
            {"who": "H", "type": "route", "pts": [(7, 0), (7, 6), (10.5, 6)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 3.5)], "end": "bar"},
        ],
        "assign": [
            ("X", "Release toward the sideline, then sprint straight downfield. Run the deep defender off and stay outside of Y."),
            ("Y", "Run 4-5 yards, plant, and break out toward the sideline. Show your hands the moment you turn."),
            ("Z", "Run a shallow crossing route about 2 yards deep, all the way across the field. Keep moving."),
            ("H", "Run 6 yards and break out to the right sideline — your backside answer."),
            ("QB", "Read Y first. If the defender is sitting on the out, throw the Z crosser underneath. Two answers, one look."),
        ],
        "notes": [
            "The out route to the sideline is the safest throw in flag football — worst case it goes out of bounds.",
            "Z's crossing route is the pressure valve. If he keeps running, he will eventually be wide open.",
            "This is a great third-and-short call. Set Y's break just past the sticks.",
        ],
    },
    {
        "num": 8,
        "mistake": 'The center releasing on the snap. Count it out — the pause IS the play.',
        "name": "SNAPPER DELAY",
        "formation": "ACE",
        "category": "QUICK GAME",
        "tagline": "Nobody covers the snapper. Nobody. Ever.",
        "paths": [
            {"who": "X", "type": "route", "pts": [(-9, 0), (-9, 12)]},
            {"who": "Z", "type": "route", "pts": [(9, 0), (9, 12)]},
            {"who": "Y", "type": "route", "pts": [(3, 0), (3, 6), (7, 7)]},
            {"who": "H", "type": "route", "pts": [(-1.5, -5), (-4, -4), (-8, -3)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 1), (-1.8, 6.5)], "delay": True},
        ],
        "assign": [
            ("C", "Snap the ball. Count 'one-thousand-one, one-thousand-two' while standing still. THEN release straight up the middle to 6 yards and turn around."),
            ("X / Z", "Sprint as deep as you can. Take everybody with you."),
            ("Y", "Run 6 yards and break out to the right."),
            ("H", "Swing out to the left as the checkdown."),
            ("QB", "Look deep for one count to hold the defense, then come back to the center. He will be alone."),
        ],
        "notes": [
            "Confirm your league makes the center an eligible receiver — nearly all 6-on-6 leagues do. If yours does not, run this with Y delaying instead.",
            "The count is what makes it work. If the center leaves immediately, a defender goes with him.",
            "Save this one. It is a change-up, not a base play — it stops working the third time you call it in a game.",
        ],
    },
    # ============================================================ SHOT PLAYS
    {
        "num": 9,
        "mistake": 'The QB locking onto the flat route. Eyes go deep first, then work down the ladder.',
        "name": "FLOOD",
        "formation": "TRIPS LEFT",
        "category": "SHOT PLAY",
        "tagline": "Three receivers, three different depths, one side of the field.",
        "paths": [
            {"who": "X", "type": "route", "pts": [(-11, 0), (-13.8, 4.5), (-15, 13.5)]},
            {"who": "Y", "type": "route", "pts": [(-7.5, 0), (-7.5, 8), (-11.8, 8)]},
            {"who": "Z", "type": "route", "pts": [(-4, 0), (-7, 1.2), (-10.8, 1.8)]},
            {"who": "H", "type": "route", "pts": [(7, 0), (5, 3.8), (1.6, 4.8)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (-0.5, 2.4)], "end": "bar"},
        ],
        "assign": [
            ("Z", "Sprint to the left flat, about 2 yards deep. You are the shallow answer — get width fast."),
            ("Y", "Run 8 yards straight up, then break out to the sideline. You are the middle answer."),
            ("X", "Release straight toward the sideline, then sprint up the sideline. You are the shot \u2014 stay outside of everybody."),
            ("H", "Work back across the middle at 4 yards as the backside checkdown."),
            ("QB", "Count them off deep-to-short: X, then Y, then Z. Take the first one open and get rid of it."),
        ],
        "notes": [
            "Three receivers at three depths on one side means the two defenders over there have to pick two. One of yours is always free.",
            "Teach the QB to look deep FIRST. If he looks short first he will never come back to the big play.",
            "Best on 2nd-and-long or any time you want a chunk without a risky throw across the middle.",
        ],
    },
    {
        "num": 10,
        "mistake": 'H turning up the sideline too early. Get all the way to the numbers first, then turn straight up.',
        "name": "POST / WHEEL",
        "formation": "TWINS RIGHT",
        "category": "SHOT PLAY",
        "tagline": "The back sneaks up the sideline while everyone watches the deep routes.",
        "paths": [
            {"who": "H", "type": "route", "pts": [(-2.6, -4.4), (2, -4), (8, -3), (13, -0.5), (13.5, 12)]},
            {"who": "Z", "type": "route", "pts": [(11, 0), (9.5, 4), (5, 12)]},
            {"who": "Y", "type": "route", "pts": [(6, 0), (6, 6), (1.5, 6)]},
            {"who": "X", "type": "route", "pts": [(-9, 0), (-9, 6), (-12.5, 6)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (-1.5, 3)], "end": "bar"},
        ],
        "assign": [
            ("H", "Run out toward the sideline behind the line of scrimmage. Get all the way wide, THEN turn straight up the sideline at full speed. Look back over your outside shoulder."),
            ("Z", "Sprint 4 yards, then angle inside toward the middle of the field on a post. Stay inside \u2014 you are clearing the deep defender away from H."),
            ("Y", "Run 6 yards and break in across the middle. Second option."),
            ("X", "Run 6 yards and break out to the sideline. Backside answer."),
            ("QB", "Peek at the post to move the deep defender, then throw the wheel to H up the sideline. Lead him toward the sideline, never inside."),
        ],
        "notes": [
            "This is the touchdown call. Nobody at 8U covers a running back who leaves the backfield sideways.",
            "H stays BEHIND the line until he is wide, so he never runs into Z. No screening is allowed and, just as important, 8U receivers who cross paths collide.",
            "Underthrown deep balls get intercepted. Coach the QB: if in doubt, throw it toward the sideline and out of bounds. Call this once a half, on 1st down, after the run has been working.",
        ],
    },
    {
        "num": 11,
        "mistake": 'A lazy run fake. If H does not sell it with his arms, no defender bites and the cross is covered.',
        "name": "PLAY-ACTION CROSS",
        "formation": "TWINS RIGHT",
        "category": "SHOT PLAY",
        "tagline": "Fake the dive, throw it deep across the middle.",
        "paths": [
            {"who": "QB", "type": "handoff", "pts": [(0, -3), (-1.4, -3.7)]},
            {"who": "H", "type": "run", "pts": [(-2.6, -4.4), (-1.4, -3.7), (1.5, -2.8), (5.5, -1.8), (8.5, -1.2)]},
            {"who": "X", "type": "route", "pts": [(-9, 0), (-9, 3), (-2, 10), (5, 12)]},
            {"who": "Z", "type": "route", "pts": [(11, 0), (11, 13)]},
            {"who": "Y", "type": "route", "pts": [(6, 0), (4.2, 2.4), (1.9, 3.2)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (-2.6, 5)]},
        ],
        "assign": [
            ("QB", "Sell the dive fake with both hands, then pull it and set up. Eyes stay on the fake for one full count."),
            ("H", "Run the dive fake hard, then slide out to the right flat as the checkdown."),
            ("X", "Take 3 hard steps upfield, then cross the field on a climbing angle — you should be 12 yards deep by the far hash."),
            ("Z", "Sprint straight downfield. Clear the deep middle out for X."),
            ("Y", "Shallow route across at 3 yards — the safety valve if the deep cross is covered."),
        ],
        "notes": [
            "Call this right after 22 Dive has worked. The fake only sells if the defense already respects the run.",
            "X's route takes time to develop. The QB has to trust it and count — this is not a quick-throw play, so avoid it if the rush is winning.",
            "Lead X toward the sideline he is running to. Throw it where he is going, not where he is.",
        ],
    },
    # ============================================================ NO-RUN ZONE
    {
        "num": 12,
        "mistake": 'Receivers sitting down with their backs to the QB. Stop, turn, hands up — every time.',
        "name": "SPACING",
        "formation": "SPREAD",
        "category": "NO-RUN ZONE",
        "tagline": "Five receivers, five depths. Somebody is always open.",
        "paths": [
            {"who": "X", "type": "route", "pts": [(-10, 0), (-10, 12)]},
            {"who": "Y", "type": "route", "pts": [(-5, 0), (-5, 6), (-7.2, 6)], "end": "bar"},
            {"who": "H", "type": "route", "pts": [(5, 0), (5, 4), (7, 4)], "end": "bar"},
            {"who": "Z", "type": "route", "pts": [(10, 0), (10, 12)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 2)], "end": "bar"},
        ],
        "assign": [
            ("X / Z", "Sprint straight downfield as fast as you can. Clear out the deep defenders."),
            ("Y", "Run 6 yards, drift 2 yards toward the sideline, and sit down in the open space. Face the QB."),
            ("H", "Run 4 yards, drift toward the sideline, and sit down. Face the QB."),
            ("C", "Snap, release 2 yards straight up, turn around. You are the last resort — always available."),
            ("QB", "Everybody is at a different depth on purpose. Scan Y, then H, then C. Somebody will be standing in grass."),
        ],
        "notes": [
            "This is your no-run-zone base call: when the offense cannot run, you need a play with zero chance of a sack-for-loss and four easy targets.",
            "Every receiver turns and FACES the QB when they stop. At 8U, half of all incompletions are receivers who never looked back.",
            "Also a good call after a penalty, or any time your QB looks rattled and needs an easy completion.",
        ],
    },
    {
        "num": 13,
        "mistake": 'Throwing the fade low or inside. High and toward the sideline, or do not throw it at all.',
        "name": "PYLON FADE",
        "formation": "ACE",
        "category": "GOAL LINE",
        "tagline": "Two-level attack at the corner of the end zone.",
        "paths": [
            {"who": "Z", "type": "route", "pts": [(9, 0), (11, 3), (12.5, 6.5)]},
            {"who": "Y", "type": "route", "pts": [(3, 0), (3, 1.5), (8, 2.5)]},
            {"who": "X", "type": "route", "pts": [(-9, 0), (-11, 3), (-12.5, 6.5)]},
            {"who": "H", "type": "route", "pts": [(-1.5, -5), (-3.5, -3), (-6.5, -1.5)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (-2, 2), (-5, 2)]},
        ],
        "assign": [
            ("Z", "Sprint on an angle toward the back corner of the end zone (the back pylon). Look over your outside shoulder."),
            ("Y", "Run 2 yards and break flat to the front corner. You are the quick, easy score."),
            ("X", "Same fade on the backside — the answer if the defense overloads the right."),
            ("H", "Swing left as the checkdown if nothing opens up."),
            ("QB", "Y first — if the front corner is open it is a walk-in. If not, throw Z high and toward the back pylon where only he can get it."),
        ],
        "notes": [
            "Inside the 5-yard line you are almost certainly in a no-run zone, so plan on throwing it.",
            "On the fade, high and outside is the only safe miss. A low or inside throw is an interception.",
            "Y's flat route and Z's fade go opposite directions on purpose — no screening allowed, so they must never cross.",
        ],
    },
    {
        "num": 14,
        "mistake": 'Two receivers ending up at the same depth. 1, 3, 5 — drill it with cones until it is automatic.',
        "name": "TRIPLE OUT",
        "formation": "TRIPS LEFT",
        "category": "GOAL LINE",
        "tagline": "Three staircase outs plus a shot. Your extra-point and short-yardage call.",
        "paths": [
            {"who": "X", "type": "route", "pts": [(-11, 0), (-12.4, 2.5), (-12.4, 5), (-14.4, 5)]},
            {"who": "Y", "type": "route", "pts": [(-7.5, 0), (-7.5, 3), (-11, 3)]},
            {"who": "Z", "type": "route", "pts": [(-4, 0), (-4, 1.8), (-6.6, 1.8)]},
            {"who": "H", "type": "route", "pts": [(7, 0), (5, 7), (0, 9)]},
            {"who": "QB", "type": "run", "pts": [(0, -3), (-3.5, -4), (-6.5, -3.5)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (1, 3)], "end": "bar"},
        ],
        "assign": [
            ("Z", "Break out at about 1 yard \u2014 the shallowest step of the staircase. You are the fastest, shortest answer."),
            ("Y", "Break out at 3 yards. Never drift into Z's depth."),
            ("X", "Release wide right away, get to 5 yards, then break out. Stay on top of and outside of both of them."),
            ("H", "Backside post across the middle — the shot if everyone chases the outs."),
            ("QB", "Roll to the left. Three receivers are breaking out in a staircase in front of you. Throw the highest open one and get to the sideline if nobody is."),
        ],
        "notes": [
            "The staircase is the whole point: 1 yard, 3 yards, 5 yards. Drill the depths — if two receivers end up at the same depth, they cover each other.",
            "Perfect for a 1-point (5-yard) or 2-point (10-yard) conversion — pick the receiver whose depth matches the line to gain.",
            "Rolling the QB out buys time against the 7-yard rusher and turns a stationary throw into an easy one.",
        ],
    },
]

# ---------------------------------------------------------- second batch
# Kept in its own module so the original fourteen stay easy to read and diff.
from plays_more import NEW_PLAYS          # noqa: E402
PLAYS.extend(NEW_PLAYS)
PLAYS.sort(key=lambda p: p["num"])
