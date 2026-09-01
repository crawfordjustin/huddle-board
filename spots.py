# -*- coding: utf-8 -*-
"""Spot-based language layer: places on the field instead of player positions.

Nobody is X or Y. Every job belongs to a SPOT, and the coach puts whichever kid
is on the field into that spot. Six spots are on the field at a time.
"""

# Internal geometry key -> (short tag on the diagram, full spot name)
SPOT_MAP = {
    "TWINS RIGHT": {
        "C": ("SN", "SNAPPER"), "QB": ("QB", "THROWER"),
        "X": ("WL", "WIDE LEFT"), "Y": ("SR", "SLOT RIGHT"),
        "Z": ("WR", "WIDE RIGHT"), "H": ("B", "BACK"),
    },
    "TRIPS LEFT": {
        "C": ("SN", "SNAPPER"), "QB": ("QB", "THROWER"),
        "X": ("WL", "WIDE LEFT"), "Y": ("SL", "SLOT LEFT"),
        "Z": ("TL", "TIGHT LEFT"), "H": ("WR", "WIDE RIGHT"),
    },
    "SPREAD": {
        "C": ("SN", "SNAPPER"), "QB": ("QB", "THROWER"),
        "X": ("WL", "WIDE LEFT"), "Y": ("SL", "SLOT LEFT"),
        "H": ("SR", "SLOT RIGHT"), "Z": ("WR", "WIDE RIGHT"),
    },
    "ACE": {
        "C": ("SN", "SNAPPER"), "QB": ("QB", "THROWER"),
        "X": ("WL", "WIDE LEFT"), "Y": ("TR", "TIGHT RIGHT"),
        "Z": ("WR", "WIDE RIGHT"), "H": ("B", "BACK"),
    },
}

SPOT_GLOSSARY = [
    ("SN", "SNAPPER", "Right over the ball. Snaps it, then becomes a receiver."),
    ("QB", "THROWER", "Three yards behind the snapper. Takes the snap."),
    ("WL", "WIDE LEFT", "All the way out by the left sideline."),
    ("SL", "SLOT LEFT", "Halfway between the snapper and the left sideline."),
    ("TL", "TIGHT LEFT", "Just outside the snapper's left shoulder."),
    ("TR", "TIGHT RIGHT", "Just outside the snapper's right shoulder."),
    ("SR", "SLOT RIGHT", "Halfway between the snapper and the right sideline."),
    ("WR", "WIDE RIGHT", "All the way out by the right sideline."),
    ("B", "BACK", "Behind and beside the thrower."),
]

SHAPES = [
    ("GO", "Sprint straight downfield as fast as you can. Do not stop.",
     [(0, 0), (0, 10)], "arrow"),
    ("OUT", "Run to the number, plant, and break toward the sideline.",
     [(0, 0), (0, 5), (4.5, 5)], "arrow"),
    ("IN", "Run to the number, plant, and break toward the middle.",
     [(0, 0), (0, 5), (-4.5, 5)], "arrow"),
    ("SIT", "Run to the number, stop, and turn around facing the thrower.",
     [(0, 0), (0, 5)], "bar"),
    ("CORNER", "Run up, then break at an angle toward the deep corner.",
     [(0, 0), (0, 6), (4.5, 11)], "arrow"),
    ("POST", "Run up, then break at an angle toward the deep middle.",
     [(0, 0), (0, 6), (-4.5, 11)], "arrow"),
    ("WHEEL", "Run out behind the line, get wide, then turn straight up the sideline.",
     [(0, -1), (3.5, -1.5), (5.5, 0), (6, 10)], "arrow"),
    ("SWING", "Loop out of the backfield toward the sideline and look back.",
     [(0, -4), (-3, -2.5), (-6, -2)], "arrow"),
    ("CARRY", "You get the ball. Run where the coach points.",
     [(0, -4), (0.8, -2), (1.5, 7)], "arrow"),
]

DEFAULT_RULE = ("If the coach did not give you a job, run GO — sprint straight downfield "
                "and take your defender with you.")

# num -> spot-based content
PLAY_TEXT = {
1: {
  "calls": [("BACK", "CARRY — straight ahead"), ("THROWER", "hand it off, don't watch"),
            ("EVERYONE ELSE", "GO")],
  "assign": [
    ("BACK", "CARRY. Take one step toward the thrower, take the ball with both hands, and run straight past the snapper's right hip. North and south, not sideways."),
    ("THROWER", "Catch the snap, turn immediately, and put the ball in the Back's belly with both hands. Do not watch him run."),
    ("SNAPPER", "Snap it, then GO straight up the field. Stay out of the Back's running lane."),
    ("WIDE LEFT / SLOT RIGHT / WIDE RIGHT", "GO. Sprint 9-10 yards downfield. You are not blocking — you are taking your defender away from the run."),
  ],
  "mistake": "The Back bouncing outside instead of hitting the hole. Straight ahead beats pretty, every single time.",
  "notes": [
    "Call this on 1st down in a run zone to set the tone and get the offense moving forward.",
    "The rusher starts 7 yards back, so a downhill run hits the line before he can get there. Speed matters more than fancy footwork.",
    "This is the best play in the book for a brand-new kid at BACK: one job, one direction, and you can walk him to the hole before the snap.",
  ]},
2: {
  "calls": [("SLOT LEFT", "MOTION → CARRY right"), ("THROWER", "hold it out at belly height"),
            ("EVERYONE ELSE", "GO")],
  "assign": [
    ("SLOT LEFT", "MOTION, then CARRY. Start running on the coach's signal. Be at FULL SPEED when you pass the thrower. Take the ball and keep running to the right sideline, then turn up."),
    ("THROWER", "Catch the snap and hold the ball out at belly height. He takes it from you — do not throw it or push it at him."),
    ("WIDE RIGHT", "GO. Sprint straight downfield and leave the sideline empty for him."),
    ("WIDE LEFT / TIGHT LEFT", "GO. Sprint downfield and take your defender with you."),
    ("SNAPPER", "Snap and GO straight up the middle."),
  ],
  "mistake": "The motion man slowing down to take the handoff. If he catches it standing still, the play is already dead.",
  "notes": [
    "Timing is everything: he must already be moving before the snap. Practice the motion 20 times before you ever practice the handoff.",
    "If the defense starts chasing the motion, that is when you call Counter Keep (3) — the fake becomes the whole play.",
    "Legal check: only one player may be in motion at the snap, and he must be moving parallel to or away from the line, not toward it.",
  ]},
3: {
  "calls": [("SLOT RIGHT", "MOTION left → FAKE"), ("THROWER", "fake, count two, CARRY right"),
            ("EVERYONE ELSE", "GO")],
  "assign": [
    ("SLOT RIGHT", "MOTION left and run the sweep FAKE all the way to the sideline. SELL IT — arms up like you have the ball."),
    ("THROWER", "Fake the handoff, count two, then step back to the right and CARRY. Patience first, speed second."),
    ("WIDE RIGHT", "GO toward the sideline to clear the thrower's running lane."),
    ("WIDE LEFT / SLOT LEFT", "GO. Sprint straight downfield 8 yards."),
    ("SNAPPER", "Snap and GO up the middle, away from the thrower's path."),
  ],
  "mistake": "The thrower pulling it out too early. Let the fake clear, count two, then go.",
  "notes": [
    "Only call this after the defense has seen Jet Sweep at least twice. The fake does not work on a defense that has not been burned yet.",
    "Rule check: some leagues do not let the thrower run the ball. If yours does not, put a kid at BACK and give him a real handoff going right while Slot Right fakes left.",
    "Tell the thrower the sideline is a friend. Get what is there and get out of bounds.",
  ]},
4: {
  "calls": [("BACK", "CARRY — get wide first"), ("THROWER", "soft pitch out in front"),
            ("EVERYONE ELSE", "GO")],
  "assign": [
    ("BACK", "CARRY. Start running toward the right sideline BEFORE you catch it. Catch it on the move, get to the edge, then turn up."),
    ("THROWER", "Catch the snap and pitch it underhanded, out in front of him so he runs onto it. Soft toss, chest high."),
    ("WIDE RIGHT", "GO straight for the sideline, then turn up. Take the corner with you and leave the edge empty."),
    ("TIGHT RIGHT", "GO downfield at an angle toward the sideline."),
    ("WIDE LEFT / SNAPPER", "GO. Sprint downfield on the backside."),
  ],
  "mistake": "Pitching the ball behind him. Toss it to where he is going, not where he is.",
  "notes": [
    "This is a race to the sideline. If he turns upfield too early he runs into traffic — coach him to get width first.",
    "A dropped pitch is a live ball. Practice it slow before you practice it fast.",
    "Great call when the defense has been crowding the middle to stop the Dive.",
  ]},
5: {
  "calls": [("SLOT LEFT", "IN at 4"), ("SLOT RIGHT", "IN at 6"),
            ("BOTH WIDES", "SIT at 5"), ("SNAPPER", "SIT at 2")],
  "assign": [
    ("SLOT LEFT", "IN at 4. Three hard steps upfield, then break inside at an angle. Look for the ball right away."),
    ("SLOT RIGHT", "IN at 6. Same shape, but take FIVE steps first so you cross behind him. Never run at the same depth as the other slot."),
    ("WIDE LEFT / WIDE RIGHT", "SIT at 5. Run 5 yards, stop, turn back to the thrower and show your hands."),
    ("THROWER", "Pick your slant before the snap. Catch, one step, throw. Do not hold this ball."),
    ("SNAPPER", "SIT at 2. Snap, release 2 yards straight up, turn around. You are the emergency dump-off."),
  ],
  "mistake": "Both slants breaking at the same depth. Four and six — say the numbers out loud in the huddle.",
  "notes": [
    "This is the play you call when the rusher is getting home. It should be out of the thrower's hand in about two seconds.",
    "Throw it low and in front. A ball at the chest is a catch; a ball behind him is an interception.",
    "The staggered depths are on purpose — no blocking or screening is allowed, so two receivers must never arrive in the same place.",
  ]},
6: {
  "calls": [("WIDE RIGHT", "SIT at 5"), ("SLOT RIGHT", "CORNER"),
            ("WIDE LEFT", "IN at 7"), ("BACK", "SWING left")],
  "assign": [
    ("WIDE RIGHT", "SIT at 5. Run 5 yards, stop, come back one step toward the thrower. You are the easy completion."),
    ("SLOT RIGHT", "CORNER. Sprint straight up 7 yards, then break out toward the corner of the field, over the top of him."),
    ("WIDE LEFT", "IN at 7. Run 7 yards and break in across the middle."),
    ("BACK", "SWING left. Loop out to the left flat as the safety valve."),
    ("THROWER", "Watch the deep defender on the right. If he stays deep, throw the SIT. If he comes up, throw the CORNER over his head."),
  ],
  "mistake": "The SIT drifting past 5 yards. His whole job is to stop short and be the easy completion.",
  "notes": [
    "This is a real read, so put a returning kid at THROWER for it. If your thrower is new, just tell him 'throw the SIT unless nobody is near the corner.'",
    "The SIT alone is a 5-yard gain. Take it. Five-yard gains win 8U games.",
    "The corner route must run AWAY from the SIT, never through him — no screening allowed.",
  ]},
7: {
  "calls": [("SLOT LEFT", "OUT at 5"), ("TIGHT LEFT", "IN at 2, keep going"),
            ("WIDE LEFT", "GO"), ("WIDE RIGHT", "OUT at 6")],
  "assign": [
    ("SLOT LEFT", "OUT at 5. Run 4-5 yards, plant, and break out toward the sideline. Show your hands the moment you turn."),
    ("TIGHT LEFT", "IN at 2 and keep going. Cross the whole field about 2 yards deep and do not stop moving."),
    ("WIDE LEFT", "GO. Release toward the sideline, then sprint deep. Run the deep defender off and stay outside."),
    ("WIDE RIGHT", "OUT at 6. Your backside answer."),
    ("THROWER", "Look at the OUT first. If the defender is sitting on it, throw the crosser underneath. Two answers, one look."),
    ("SNAPPER", "SIT at 3."),
  ],
  "mistake": "Rounding off the OUT. Plant hard and run flat to the sideline.",
  "notes": [
    "The out route to the sideline is the safest throw in flag football — worst case it goes out of bounds.",
    "The crosser is the pressure valve. If he keeps running, he will eventually be wide open.",
    "Great third-and-short call. Set the OUT's break just past the sticks.",
  ]},
8: {
  "calls": [("SNAPPER", "count 1-2, then GO"), ("BOTH WIDES", "GO deep"),
            ("TIGHT RIGHT", "OUT at 6"), ("BACK", "SWING left")],
  "assign": [
    ("SNAPPER", "Snap the ball. Count 'one-thousand-one, one-thousand-two' standing still. THEN GO straight up the middle to 6 yards and turn around."),
    ("WIDE LEFT / WIDE RIGHT", "GO. Sprint as deep as you can and take everybody with you."),
    ("TIGHT RIGHT", "OUT at 6."),
    ("BACK", "SWING left as the checkdown."),
    ("THROWER", "Look deep for one count to hold the defense, then come back to the snapper. He will be alone."),
  ],
  "mistake": "The snapper releasing on the snap. Count it out — the pause IS the play.",
  "notes": [
    "Confirm your league makes the snapper an eligible receiver — nearly all 6-on-6 leagues do. If yours does not, have TIGHT RIGHT do the delay instead.",
    "Put a kid who can count and stay calm at SNAPPER for this one. It is a great spot for a quiet kid to be the hero.",
    "Save it. It is a change-up, not a base play — it stops working the third time you call it in a game.",
  ]},
9: {
  "calls": [("TIGHT LEFT", "OUT at 2"), ("SLOT LEFT", "OUT at 8"),
            ("WIDE LEFT", "CORNER"), ("WIDE RIGHT", "IN at 5")],
  "assign": [
    ("TIGHT LEFT", "OUT at 2. Sprint to the left flat and get width fast. You are the shallow answer."),
    ("SLOT LEFT", "OUT at 8. Run 8 yards straight up, then break out. You are the middle answer."),
    ("WIDE LEFT", "CORNER. Release straight toward the sideline, then sprint up it. You are the shot — stay outside of everybody."),
    ("WIDE RIGHT", "IN at 5. Work back across the middle as the backside checkdown."),
    ("THROWER", "Count them off deep to short: CORNER, then OUT at 8, then OUT at 2. Take the first one open and get rid of it."),
  ],
  "mistake": "The thrower locking onto the shallow route. Eyes go deep first, then work down the ladder.",
  "notes": [
    "Three receivers at three depths on one side means the two defenders over there have to pick two. One of yours is always free.",
    "Teach the thrower to look deep FIRST. If he looks short first he will never come back to the big play.",
    "Best on 2nd-and-long, or any time you want a chunk without a risky throw across the middle.",
  ]},
10: {
  "calls": [("BACK", "WHEEL up the right sideline"), ("WIDE RIGHT", "POST"),
            ("SLOT RIGHT", "IN at 6"), ("WIDE LEFT", "OUT at 6")],
  "assign": [
    ("BACK", "WHEEL. Run out toward the sideline BEHIND the line of scrimmage. Get all the way wide, THEN turn straight up the sideline. Look back over your outside shoulder."),
    ("WIDE RIGHT", "POST. Sprint 4 yards, then angle inside toward the middle. Stay inside — you are clearing the deep defender away from the wheel."),
    ("SLOT RIGHT", "IN at 6. Second option."),
    ("WIDE LEFT", "OUT at 6. Backside answer."),
    ("THROWER", "Peek at the post to move the deep defender, then throw the wheel up the sideline. Lead him toward the sideline, never inside."),
  ],
  "mistake": "Turning up the sideline too early. Get all the way to the numbers first, then turn straight up.",
  "notes": [
    "This is the touchdown call. Nobody at 8U covers a kid who leaves the backfield sideways.",
    "The BACK stays BEHIND the line until he is wide, so he never runs into Wide Right. No screening is allowed, and 8U kids who cross paths collide.",
    "Underthrown deep balls get intercepted. If in doubt, throw it toward the sideline and out of bounds.",
  ]},
11: {
  "calls": [("THROWER", "fake the dive, count one"), ("BACK", "FAKE → SWING right"),
            ("WIDE LEFT", "CROSS deep"), ("WIDE RIGHT", "GO"), ("SLOT RIGHT", "IN at 3")],
  "assign": [
    ("THROWER", "Sell the dive fake with both hands, then pull it and set up. Eyes stay on the fake for one full count."),
    ("BACK", "Run the dive FAKE hard, then slide out to the right flat as the checkdown."),
    ("WIDE LEFT", "CROSS. Three hard steps upfield, then cross the field on a climbing angle — you should be 12 yards deep by the far side."),
    ("WIDE RIGHT", "GO. Sprint straight downfield and clear the deep middle out."),
    ("SLOT RIGHT", "IN at 3. The safety valve if the deep cross is covered."),
  ],
  "mistake": "A lazy run fake. If the Back does not sell it with his arms, nobody bites and the cross is covered.",
  "notes": [
    "Call this right after the Dive has worked. The fake only sells if the defense already respects the run.",
    "The cross takes time to develop. The thrower has to trust it and count — avoid this one if the rush is winning.",
    "Lead him toward the sideline he is running to. Throw it where he is going, not where he is.",
  ]},
12: {
  "calls": [("BOTH WIDES", "GO deep"), ("SLOT LEFT", "SIT at 6"),
            ("SLOT RIGHT", "SIT at 4"), ("SNAPPER", "SIT at 2")],
  "assign": [
    ("WIDE LEFT / WIDE RIGHT", "GO. Sprint straight downfield as fast as you can and clear out the deep defenders."),
    ("SLOT LEFT", "SIT at 6. Run 6 yards, drift 2 yards toward the sideline, and sit down in the open space. Face the thrower."),
    ("SLOT RIGHT", "SIT at 4. Run 4 yards, drift toward the sideline, and sit down. Face the thrower."),
    ("SNAPPER", "SIT at 2. Snap, release 2 yards, turn around. You are the last resort — always available."),
    ("THROWER", "Everybody is at a different depth on purpose. Scan 6, then 4, then 2. Somebody will be standing in grass."),
  ],
  "mistake": "Sitting down with your back to the thrower. Stop, turn, hands up — every time.",
  "notes": [
    "This is your no-run-zone base call: four easy targets and no chance of a run for a loss.",
    "It is also the most forgiving play in the book for a new group — every job is 'run to a number and turn around.'",
    "Good call after a penalty, or any time your thrower looks rattled and needs an easy completion.",
  ]},
13: {
  "calls": [("TIGHT RIGHT", "OUT at 2 — front pylon"), ("WIDE RIGHT", "CORNER — back pylon"),
            ("WIDE LEFT", "CORNER"), ("BACK", "SWING left")],
  "assign": [
    ("TIGHT RIGHT", "OUT at 2. Run 2 yards and break flat to the front corner of the end zone. You are the quick, easy score."),
    ("WIDE RIGHT", "CORNER. Sprint on an angle toward the back corner of the end zone. Look over your outside shoulder."),
    ("WIDE LEFT", "CORNER on the backside — the answer if the defense overloads the right."),
    ("BACK", "SWING left as the checkdown if nothing opens up."),
    ("THROWER", "Front pylon first — if it is open it is a walk-in. If not, throw the back-corner fade high and outside where only he can get it."),
  ],
  "mistake": "Throwing the fade low or inside. High and toward the sideline, or do not throw it at all.",
  "notes": [
    "Inside the 5-yard line you are almost certainly in a no-run zone, so plan on throwing it.",
    "The flat route and the fade go opposite directions on purpose — no screening allowed, so they must never cross.",
    "Two easy jobs plus two hard ones: put newer kids at TIGHT RIGHT and BACK, and your best hands at WIDE RIGHT.",
  ]},
14: {
  "calls": [("TIGHT LEFT", "OUT at 1"), ("SLOT LEFT", "OUT at 3"),
            ("WIDE LEFT", "OUT at 5"), ("WIDE RIGHT", "POST"), ("THROWER", "roll left")],
  "assign": [
    ("TIGHT LEFT", "OUT at 1. The shallowest step of the staircase. Fastest, shortest answer."),
    ("SLOT LEFT", "OUT at 3. Never drift down into the 1-yard route."),
    ("WIDE LEFT", "OUT at 5. Release wide right away, get to 5, then break out. Stay on top of and outside of both of them."),
    ("WIDE RIGHT", "POST across the middle — the shot if everyone chases the outs."),
    ("THROWER", "Roll to the left. Three receivers break out in a staircase in front of you. Throw the highest open one, and get to the sideline if nobody is."),
  ],
  "mistake": "Two receivers ending up at the same depth. One, three, five — drill it with cones until it is automatic.",
  "notes": [
    "The staircase is the whole point: 1 yard, 3 yards, 5 yards. If two land at the same depth, one defender covers both.",
    "Perfect for a 1-point (5-yard) or 2-point (10-yard) conversion — pick the spot whose depth matches the line to gain.",
    "Rolling the thrower out buys time against the 7-yard rusher and turns a stationary throw into an easy one.",
  ]},
}

# ---------------------------------------------------------- second batch
from spots_more import NEW_TEXT          # noqa: E402
PLAY_TEXT.update(NEW_TEXT)
