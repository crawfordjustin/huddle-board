# -*- coding: utf-8 -*-
"""Second batch of concepts, plays 15-24.

Chosen to fill the gaps in the first fourteen rather than to hit a number:

  NO-RUN ZONE went from 1 concept to 4. If the league makes you throw inside
  its zones, one answer is not a plan.
  RUN ZONE gained the two actions 6-on-6 flag actually has left once you own
  dive / sweep / keep / pitch: the DRAW and the REVERSE.
  SHOT PLAY gained FOUR VERTS, which is the default rule made into a play —
  every kid already knows his job before you call it.

Every route here decomposes into the nine shapes. Nothing needed a tenth.
Coordinates are yards: x = left/right of the snapper, y = downfield.
"""

NEW_PLAYS = [
    # ======================================================== NO-RUN ZONE
    {
        "num": 15,
        "name": "ALL SIT",
        "formation": "SPREAD",
        "category": "NO-RUN ZONE",
        "tagline": "Everybody runs five and turns around. The simplest play in the book.",
        "mistake": "Drifting upfield after the turn. Stop means stop — if he keeps drifting, "
                   "the throw goes behind him.",
        "paths": [
            {"who": "X", "type": "route", "pts": [(-10, 0), (-10, 5)], "end": "bar"},
            {"who": "Y", "type": "route", "pts": [(-5, 0), (-5, 5)], "end": "bar"},
            {"who": "H", "type": "route", "pts": [(5, 0), (5, 5)], "end": "bar"},
            {"who": "Z", "type": "route", "pts": [(10, 0), (10, 5)], "end": "bar"},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 4)], "end": "bar"},
        ],
        "assign": [
            ("X / Y / H / Z", "Sprint 5 yards, stop, turn around and face the thrower. Hands up."),
            ("C", "Snap it, run 4 yards, turn around. You are the safety valve."),
            ("QB", "Throw to whoever turned around first with nobody near him. Any of the five is right."),
        ],
        "notes": [
            "This is the play to call when you are out of timeouts, out of ideas, or the kids "
            "are rattled. Nobody can run it wrong.",
            "Five yards apart across the whole formation means no two kids can collide even if "
            "they run the wrong way.",
            "Great first no-run-zone install: it teaches the SIT shape to all six at once.",
        ],
    },
    {
        "num": 16,
        "name": "SLANT FLAT",
        "formation": "TWINS RIGHT",
        "category": "NO-RUN ZONE",
        "tagline": "One in, one out, same side. Whichever way the defender goes, he is wrong.",
        "mistake": "The slot rounding his break. Plant the outside foot and cut flat across, "
                   "or the defender stays on top of him.",
        "paths": [
            {"who": "Y", "type": "route", "pts": [(6, 0), (6, 6), (2.2, 6)]},
            {"who": "Z", "type": "route", "pts": [(11, 0), (11, 3), (14.2, 3)]},
            {"who": "X", "type": "route", "pts": [(-9, 0), (-9, 10)]},
            {"who": "H", "type": "route", "pts": [(-2.6, -4.4), (-5.6, -3.4), (-9.4, -2.7)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 4)], "end": "bar"},
        ],
        "assign": [
            ("Y", "Run 6 yards, plant, and break flat across the middle. You are the first look."),
            ("Z", "Run 3 yards and break straight out to the sideline. Stay shallow — you are "
                  "pulling the corner down and away from Y."),
            ("X", "Sprint straight downfield. Take the backside defender with you."),
            ("H", "Loop out of the backfield to the left sideline and look back. Late outlet."),
            ("C", "Snap, 4 yards, turn around."),
            ("QB", "Look at Y first. If the middle is crowded, come back to Z in the flat."),
        ],
        "notes": [
            "The two routes are on the same side but 3 yards apart in depth, so they never cross "
            "and never rub — which is what makes it legal.",
            "If the defense starts jumping the flat, this is when Play 20 (Double Post) hits.",
        ],
    },
    {
        "num": 19,
        "name": "HIGH LOW",
        "formation": "TRIPS LEFT",
        "category": "NO-RUN ZONE",
        "tagline": "Two outs to the same sideline, one deep and one shallow. Pick a level.",
        "mistake": "Both receivers breaking at the same depth. If they stack up, the play is "
                   "dead and it looks like a screen.",
        "paths": [
            {"who": "Z", "type": "route", "pts": [(-4, 0), (-4, 3), (-7.6, 3)]},
            {"who": "X", "type": "route", "pts": [(-11, 0), (-11, 8), (-14.3, 8)]},
            {"who": "Y", "type": "route", "pts": [(-7.5, 0), (-8.8, 2.0), (-8.8, 12)]},
            {"who": "H", "type": "route", "pts": [(7, 0), (7, 10)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 5)], "end": "bar"},
        ],
        "assign": [
            ("Z", "Run 3 yards and break out toward the sideline. You are the LOW. First look."),
            ("X", "Run 8 yards and break out toward the sideline. You are the HIGH."),
            ("Y", "Take one step toward the sideline, then sprint straight up. You clear the window the low route breaks into."),
            ("H", "Sprint downfield on the far side. Take your man out of the picture."),
            ("C", "Snap, 5 yards, turn around."),
            ("QB", "One defender cannot cover both. Throw to the one he leaves."),
        ],
        "notes": [
            "Five yards of separation between the two break points is what makes this safe to "
            "run with 8-year-olds. Do not let them creep together.",
            "Teach the read out loud on the sideline: 'if he comes up, throw over him.'",
        ],
    },
    {
        "num": 22,
        "name": "REVERSE",
        "formation": "SPREAD",
        "category": "RUN ZONE",
        "tagline": "Hand it one way, hand it back the other. Beats a defense that chases.",
        "mistake": "The second runner taking off before he has the ball. Take it first, then go.",
        "paths": [
            {"who": "H", "type": "motion", "pts": [(5, 0), (2.2, -1.6), (-0.4, -1.8)]},
            {"who": "QB", "type": "handoff", "pts": [(0, -3), (-1.1, -2.0)]},
            {"who": "H", "type": "run", "pts": [(-0.4, -1.8), (-3.4, -2.1), (-6.2, -1.8)]},
            {"who": "Y", "type": "run", "pts": [(-5, 0), (-6.6, -1.6), (-3.2, -2.4),
                                                (2.5, -1.6), (8.2, 0.6), (10, 8)]},
            {"who": "X", "type": "route", "pts": [(-10, 0), (-10, 9)]},
            {"who": "Z", "type": "route", "pts": [(10, 0), (10, 5), (5.4, 5)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (-1.8, 4.5)]},
        ],
        "assign": [
            ("H", "Start moving before the snap. Take the ball going left, run two steps, then "
                  "hand it to Y coming the other way. Keep running left after you give it up."),
            ("Y", "Let H get past you, take the ball with both hands, and run all the way to the "
                  "right sideline before you turn up."),
            ("QB", "Hand it to H on the move. Do not watch the rest — turn and look left."),
            ("Z", "Break inside at 5 yards. You are clearing the right sideline for Y."),
            ("X", "Sprint downfield and take the corner with you."),
            ("C", "Snap and release up the middle, away from both exchanges."),
        ],
        "notes": [
            "Only call this after Jet Sweep has worked. The reverse is the punishment for "
            "chasing; with nothing to chase it is just a slow run.",
            "Two exchanges means two chances to fumble. Practise it at walking speed first.",
            "Z breaking IN is not decoration — without it he is standing in the exact spot Y "
            "is trying to reach.",
        ],
    },
    # ========================================================= QUICK GAME
    {
        "num": 17,
        "name": "BUBBLE",
        "formation": "TRIPS LEFT",
        "category": "QUICK GAME",
        "tagline": "Catch it wide with room to run. The answer to a hard rush.",
        "mistake": "Throwing it backwards. The catch has to be in front of the thrower or it is "
                   "a live ball if it drops.",
        "paths": [
            {"who": "Z", "type": "route", "pts": [(-4, 0), (-6.6, -0.8), (-9.6, 0.6)]},
            {"who": "Y", "type": "route", "pts": [(-7.5, 0), (-7.5, 9)]},
            {"who": "X", "type": "route", "pts": [(-11, 0), (-11, 10)]},
            {"who": "H", "type": "route", "pts": [(7, 0), (7, 10)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 4)], "end": "bar"},
        ],
        "assign": [
            ("Z", "Open out toward the sideline and get width fast, staying level with the line. "
                  "Look back for the ball immediately."),
            ("Y", "Sprint straight downfield. Take your defender away from Z."),
            ("X", "Sprint straight downfield and stay wide."),
            ("H", "Sprint downfield on the far side."),
            ("C", "Snap, 4 yards, turn around."),
            ("QB", "Catch and throw in one motion. This is the fastest ball you will throw all game."),
        ],
        "notes": [
            "The whole play is over in two seconds, which is exactly why it beats a rusher who "
            "starts 7 yards away.",
            "Coach the catch point: Z must be even with or in front of the thrower. A backward "
            "pass is a lateral, and at 8U a loose ball on the ground is chaos.",
        ],
    },
    {
        "num": 24,
        "name": "SNAG",
        "formation": "TRIPS LEFT",
        "category": "QUICK GAME",
        "tagline": "Three receivers, three different depths, one side of the field.",
        "mistake": "The sit drifting toward the sideline. He should settle in the window and "
                   "stay there.",
        "paths": [
            {"who": "Z", "type": "route", "pts": [(-4, 0), (-5.6, 5)], "end": "bar"},
            {"who": "X", "type": "route", "pts": [(-11, 0), (-11, 5), (-14.6, 9.5)]},
            {"who": "Y", "type": "route", "pts": [(-7.5, 0), (-9.6, -1.4), (-12.4, -1.1)]},
            {"who": "H", "type": "route", "pts": [(7, 0), (7, 10)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (0.6, 6)]},
        ],
        "assign": [
            ("Z", "Run 5 yards, drift slightly outside, and sit down facing the thrower. "
                  "First look."),
            ("X", "Run 5 yards and break at an angle for the deep corner."),
            ("Y", "Loop behind the line toward the sideline and look back. You are the checkdown."),
            ("H", "Sprint downfield on the far side."),
            ("C", "Snap and release straight up the middle."),
            ("QB", "Z first. If he is covered, the corner is behind it and the loop is underneath it."),
        ],
        "notes": [
            "Three levels on one side — deep, medium, behind the line. Whoever the defense "
            "takes away, the other two are open.",
            "This is the most grown-up concept in the book and it still uses only CORNER, SIT "
            "and SWING. No new vocabulary.",
        ],
    },
    # ========================================================== SHOT PLAY
    {
        "num": 18,
        "name": "FOUR VERTS",
        "formation": "SPREAD",
        "category": "SHOT PLAY",
        "tagline": "Everybody goes. The default rule, called on purpose.",
        "mistake": "Drifting together downfield. They start 5 yards apart and they must finish "
                   "5 yards apart.",
        "paths": [
            {"who": "X", "type": "route", "pts": [(-10, 0), (-10.5, 12)]},
            {"who": "Y", "type": "route", "pts": [(-5, 0), (-5, 12)]},
            {"who": "H", "type": "route", "pts": [(5, 0), (5, 12)]},
            {"who": "Z", "type": "route", "pts": [(10, 0), (10.5, 12)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 8)]},
        ],
        "assign": [
            ("X / Z", "Sprint straight up the sideline. Do not drift inside."),
            ("Y / H", "Sprint straight up your lane. Split the middle of the field."),
            ("C", "Snap and sprint 8 yards up the middle."),
            ("QB", "Take the deepest one who has nobody behind him. If they are all covered, "
                   "throw it away — this is a shot, not a scramble."),
        ],
        "notes": [
            "Every kid already knows this play, because it is the default rule: if the coach did "
            "not give you a job, run GO. Call it when the huddle is a mess.",
            "Five lanes, five yards apart, nobody crosses anybody. It is the safest deep call "
            "in the book.",
            "Best used right after a run has worked. The defense creeps up, and there is nobody home.",
        ],
    },
    {
        "num": 20,
        "name": "DOUBLE POST",
        "formation": "TWINS RIGHT",
        "category": "SHOT PLAY",
        "tagline": "Two receivers attack the deep middle at different depths. Somebody is open.",
        "mistake": "Both breaking at the same yard line. The staggered depth is the whole play.",
        "paths": [
            {"who": "Y", "type": "route", "pts": [(6, 0), (6, 8), (1.2, 13.6)]},
            {"who": "Z", "type": "route", "pts": [(11, 0), (11, 4.5), (6.6, 9.5)]},
            {"who": "X", "type": "route", "pts": [(-9, 0), (-9, 5), (-12.6, 5)]},
            {"who": "H", "type": "route", "pts": [(-2.6, -4.4), (-5.6, -3.6), (-8.8, -3.0)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (0, 3)], "end": "bar"},
        ],
        "assign": [
            ("Y", "Run 8 yards, then break at an angle for the deep middle. You are the deep one."),
            ("Z", "Run 4 yards, then break at an angle inside. You are underneath Y — stay under him."),
            ("X", "Run 5 yards and break out. Backside answer if they take both posts."),
            ("H", "Loop out of the backfield to the left and look back."),
            ("C", "Snap, 3 yards, turn around."),
            ("QB", "One deep defender cannot take both. Throw over him or in front of him."),
        ],
        "notes": [
            "The two posts break 3.5 yards apart in depth and finish 5 yards apart. Drill the "
            "stagger — if they end up side by side it is a collision, not a concept.",
            "Call it after Slant Flat has pulled the defense up.",
        ],
    },
    # ============================================================ RUN ZONE
    {
        "num": 21,
        "name": "DRAW",
        "formation": "TWINS RIGHT",
        "category": "RUN ZONE",
        "tagline": "Everybody runs pass routes. Then you hand it off anyway.",
        "mistake": "Handing it off too early. Let the rusher commit upfield first — count one, "
                   "then give it.",
        "paths": [
            {"who": "QB", "type": "handoff", "pts": [(0, -3), (-1.5, -4.9)]},
            {"who": "H", "type": "run", "pts": [(-2.6, -4.4), (-3.3, -5.5), (-1.5, -4.9),
                                                (0.9, -1.6), (2.0, 7.5)]},
            {"who": "X", "type": "route", "pts": [(-9, 0), (-9, 6), (-12.5, 6)]},
            {"who": "Y", "type": "route", "pts": [(6, 0), (6, 6), (9.6, 6)]},
            {"who": "Z", "type": "route", "pts": [(11, 0), (11, 11)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (-2.6, 5)]},
        ],
        "assign": [
            ("QB", "Catch the snap and take one step back like you are going to throw. Let the "
                   "rusher come. Then hand it to H."),
            ("H", "Take a small step back, wait one count, take the ball and run straight up "
                  "past the snapper's right hip."),
            ("X / Y", "Run 6 yards and break out. Sell it — you are pulling defenders sideways."),
            ("Z", "Sprint straight downfield."),
            ("C", "Snap, then release left and stay out of the running lane."),
        ],
        "notes": [
            "The rusher has to start 7 yards back, so he arrives with a full head of steam. "
            "This play uses that against him — he runs himself out of the play.",
            "Only works if the receivers sell it. If they jog, the defenders never leave.",
            "Call it the down after a pass, never the down after a run.",
        ],
    },
    # =========================================================== GOAL LINE
    {
        "num": 23,
        "name": "FLAT DUMP",
        "formation": "ACE",
        "category": "GOAL LINE",
        "tagline": "Clear everybody out of the end zone and dump it to the back.",
        "mistake": "The back turning upfield before he catches it. Catch first, then turn.",
        "paths": [
            {"who": "H", "type": "route", "pts": [(-1.5, -5), (1.8, -4.2), (5.4, -3.4), (7.6, -2.4)]},
            {"who": "Z", "type": "route", "pts": [(9, 0), (9, 4), (13.4, 8.5)]},
            {"who": "Y", "type": "route", "pts": [(3, 0), (3, 9)]},
            {"who": "X", "type": "route", "pts": [(-9, 0), (-9, 8)]},
            {"who": "C", "type": "route", "pts": [(0, 0), (-3, 3)], "end": "bar"},
        ],
        "assign": [
            ("H", "Loop out of the backfield toward the right sideline. Stay behind everybody, "
                  "look back, catch it, THEN turn upfield."),
            ("Z", "Run 4 yards and break for the corner. You are taking the corner defender "
                  "out of the end zone."),
            ("Y", "Sprint straight into the end zone. Take the middle defender with you."),
            ("X", "Sprint into the end zone on the far side."),
            ("C", "Snap and drift left. Stay out of the throwing lane."),
            ("QB", "Everyone else is running away from the flat. Turn and dump it to H."),
        ],
        "notes": [
            "Inside the 5, every defender's eyes go to the end zone. The flat is the emptiest "
            "grass on the field.",
            "Pairs with Pylon Fade out of the same formation — same picture, opposite answer.",
        ],
    },
]
