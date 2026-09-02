using static HuddleBoard.Playbook.EndStyle;
using static HuddleBoard.Playbook.PathType;

namespace HuddleBoard.Playbook;

/// <summary>
/// The original fourteen plays. These are the calibration set: they pass the
/// checker with zero errors, and nothing was tuned to make that true. If a
/// change to the checker makes them fail, the change is wrong, not the plays.
/// </summary>
public static partial class PlayLibrary
{
    private static readonly IReadOnlyList<Play> Original =
    [
        new(
            Num: 1,
            Name: "22 DIVE",
            Formation: "TWINS RIGHT",
            Category: "RUN ZONE",
            Tagline: "Simple downhill handoff. Your first-down play.",
            Mistake: "H bouncing outside instead of hitting the hole. Straight ahead beats pretty, every single time.",
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(-1.4, -3.7)], To: "H"),
                new("H", Run, [new(-2.6, -4.4), new(-1.4, -3.7), new(2.2, 0.5), new(3, 8)]),
                new("X", Route, [new(-9, 0), new(-9, 10)]),
                new("Y", Route, [new(6, 0), new(6, 9)]),
                new("Z", Route, [new(11, 0), new(11, 10)]),
                new("C", Route, [new(0, 0), new(-2.5, 5)]),
            ],
            Assign:
            [
                new("QB",
                    "Catch the snap, turn immediately, hand the ball to H with both hands. Do not watch him run."),
                new("H",
                    "Take one step toward the QB, take the ball, run north-south past the center's right hip. Get vertical."),
                new("X / Y / Z",
                    "Sprint straight downfield 9-10 yards. You are not blocking — you are taking your defender away from the run."),
                new("C",
                    "Snap it, then release straight up the field. Stay out of H's running lane."),
            ],
            Notes:
            [
                "Call this on 1st down in a run zone to set the tone and get the offense moving forward.",
                "The rusher starts 7 yards back, so a downhill run hits the line before he can get there. Speed matters more than fancy footwork.",
                "Coach the receivers hard on this: at 8U they want to stand and watch. Their sprint downfield is what opens the hole.",
            ]),
        new(
            Num: 2,
            Name: "JET SWEEP",
            Formation: "TRIPS LEFT",
            Category: "RUN ZONE",
            Tagline: "Full-speed motion handoff to the edge. Beats slow defenses every time.",
            Mistake: "Y slowing down to take the handoff. If he catches it standing still, the play is already dead.",
            Paths:
            [
                new("Y", Motion, [new(-7.5, 0), new(-4.5, -1.6), new(-1.5, -1.6)]),
                new("QB", Handoff, [new(0, -3), new(-1, -1.8)], To: "Y"),
                new("Y", Run, [new(-1.5, -1.6), new(3, -1.4), new(8, -0.6), new(11.5, 6.5)]),
                new("X", Route, [new(-11, 0), new(-11, 8)]),
                new("Z", Route, [new(-4, 0), new(-5.5, 9)]),
                new("H", Route, [new(7, 0), new(6, 9)]),
                new("C", Route, [new(0, 0), new(0, 5)]),
            ],
            Assign:
            [
                new("Y",
                    "Start moving on the coach's signal. Be at FULL SPEED when you pass the QB. Take the ball and keep running to the sideline, then turn up."),
                new("QB",
                    "Catch the snap and hold the ball out at belly height. Y takes it from you — do not throw it or push it at him."),
                new("H",
                    "Sprint straight downfield. Take the corner with you and leave the sideline empty for Y."),
                new("Z / X",
                    "Sprint downfield. Take your defender with you."),
                new("C",
                    "Snap and release straight up the middle."),
            ],
            Notes:
            [
                "Timing is everything: Y must already be moving before the snap. Practice the motion 20 times before you ever practice the handoff.",
                "If the defense starts chasing the motion, that is when you call Play 3 (QB Counter) — the fake becomes the whole play.",
                "Legal check: only one player may be in motion at the snap and he must be moving parallel to or away from the line, not toward it.",
            ]),
        new(
            Num: 3,
            Name: "COUNTER KEEP",
            Formation: "SPREAD",
            Category: "RUN ZONE",
            Tagline: "Fake the sweep one way, keep it the other. The counter-punch to Jet Sweep.",
            Mistake: "The QB pulling it out too early. Let H clear the fake, count two, then go.",
            Paths:
            [
                new("H", Motion, [new(5, 0), new(2.4, -2.0)]),
                new("H", Fake, [new(2.4, -2.0), new(-2, -2.2), new(-6.5, -2.0)]),
                new("QB", Run, [new(0, -3), new(2, -3.9), new(5.5, -3.4), new(7.8, 0.5), new(8.2, 7)]),
                new("X", Route, [new(-10, 0), new(-10, 8)]),
                new("Y", Route, [new(-5, 0), new(-5, 8)]),
                new("Z", Route, [new(10, 0), new(10, 9)]),
                new("C", Route, [new(0, 0), new(-1, 6)]),
            ],
            Assign:
            [
                new("H",
                    "Motion left and run the sweep fake all the way to the sideline. SELL IT — arms up like you have the ball."),
                new("QB",
                    "Fake the handoff to H, then step back to the right and run. Two counts of patience, then go."),
                new("Z",
                    "Sprint downfield and to the sideline to clear the QB's running lane."),
                new("X / Y",
                    "Sprint straight downfield 8 yards."),
                new("C",
                    "Snap and release up the middle, away from the QB's path."),
            ],
            Notes:
            [
                "Only call this after the defense has seen Jet Sweep at least twice. The fake does not work on a defense that has not been burned yet.",
                "Rule check: some leagues do not let the QB run the ball. If yours does not, run this exact play with H taking a real handoff going right instead — the fake is the H motion left with a second back, or simply pitch it.",
                "Tell the QB the sideline is a friend. Get what is there and get out of bounds.",
            ]),
        new(
            Num: 4,
            Name: "PITCH RIGHT",
            Formation: "ACE",
            Category: "RUN ZONE",
            Tagline: "Get outside fast. The rusher is 7 yards away — beat him to the edge.",
            Mistake: "Pitching the ball behind H. Toss it to where he is going, not where he is.",
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(3.5, -4.2)], To: "H"),
                new("H", Run, [new(-1.5, -5), new(2, -4.5), new(6, -3), new(9.5, 1), new(10.5, 8)]),
                new("Z", Route, [new(9, 0), new(12.6, 3), new(13.4, 8)]),
                new("Y", Route, [new(3, 0), new(5, 9)]),
                new("X", Route, [new(-9, 0), new(-9, 8)]),
                new("C", Route, [new(0, 0), new(-1, 6)]),
            ],
            Assign:
            [
                new("QB",
                    "Catch the snap and pitch it underhanded, out in front of H so he runs onto it. Soft toss, chest high."),
                new("H",
                    "Start running to the sideline BEFORE you catch it. Catch the ball on the move, get to the edge, then turn up the field."),
                new("Z",
                    "Release straight for the sideline, then turn up. Take the corner with you and leave the edge empty for H."),
                new("Y",
                    "Sprint downfield at an angle toward the sideline."),
                new("X / C",
                    "Sprint downfield on the backside."),
            ],
            Notes:
            [
                "This is a race to the sideline. If H tries to turn upfield too early he runs into traffic — coach him to get width first.",
                "If the pitch is dropped it is a live ball. Practice it slow before you practice it fast.",
                "Great call when the defense has been crowding the middle to stop 22 Dive.",
            ]),
        new(
            Num: 5,
            Name: "DOUBLE SLANT",
            Formation: "SPREAD",
            Category: "QUICK GAME",
            Tagline: "Ball out in two seconds. Your answer to a hard rush.",
            Mistake: "Both slants breaking at the same depth. Y at 4, H at 6 — say the numbers out loud in practice.",
            Paths:
            [
                new("X", Route, [new(-10, 0), new(-10, 5.5)], Bar),
                new("Y", Route, [new(-5, 0), new(-4, 1.5), new(-0.5, 4.5)]),
                new("H", Route, [new(5, 0), new(4, 2), new(0.5, 6.5)]),
                new("Z", Route, [new(10, 0), new(10, 5.5)], Bar),
                new("C", Route, [new(0, 0), new(0, 2.2)], Bar),
            ],
            Assign:
            [
                new("Y",
                    "Three hard steps upfield, then break inside at an angle. Look for the ball right away — it is coming at 4 yards."),
                new("H",
                    "Same slant, but take FIVE steps first so you cross behind Y at 6 yards. Never run at the same depth as Y."),
                new("X / Z",
                    "Run 5 yards, stop, turn back to the QB and show your hands. Sit down in the open grass."),
                new("QB",
                    "Pick your slant before the snap. Catch, one step, throw. Do not hold this ball."),
                new("C",
                    "Snap, release 2 yards straight up, and turn around. You are the emergency dump-off."),
            ],
            Notes:
            [
                "This is the play you call when the rusher is getting home. It should be out of the QB's hand in about two seconds.",
                "Throw it low and in front. A ball at the receiver's chest is a catch; a ball behind him is an interception.",
                "The staggered depths are on purpose — no blocking or screening is allowed, so receivers must never run into each other's defender.",
            ]),
        new(
            Num: 6,
            Name: "SMASH",
            Formation: "TWINS RIGHT",
            Category: "QUICK GAME",
            Tagline: "High-low the corner. One defender, two receivers, he cannot be right.",
            Mistake: "Z drifting past 5 yards. His whole job is to stop short and be the easy completion.",
            Paths:
            [
                new("Z", Route, [new(11, 0), new(11, 5)], Bar),
                new("Y", Route, [new(6, 0), new(6, 7), new(12, 12)]),
                new("X", Route, [new(-9, 0), new(-9, 7), new(-4, 7)]),
                new("H", Route, [new(-2.6, -4.4), new(-5, -2.4), new(-8, -1.8)]),
                new("C", Route, [new(0, 0), new(0, 4)], Bar),
            ],
            Assign:
            [
                new("Z",
                    "Run 5 yards, stop, come back one step toward the QB. You are the easy completion."),
                new("Y",
                    "Sprint straight up 7 yards, then break out toward the corner of the field. Run away from the sideline defender."),
                new("X",
                    "Run 7 yards and break in across the middle."),
                new("H",
                    "Swing out to the left flat as the safety valve."),
                new("QB",
                    "Look at the deep defender on the right. If he stays deep, throw the Z hitch. If he comes up, throw Y over his head."),
            ],
            Notes:
            [
                "This is a real read, so it is a good one for a returning QB. If your QB is new, just tell him 'throw the hitch to Z unless nobody is near Y.'",
                "The hitch alone is a 5-yard gain. Take it. Five-yard gains win 8U games.",
                "Y must run his corner route AWAY from Z, never through him — no screening allowed.",
            ]),
        new(
            Num: 7,
            Name: "STICK",
            Formation: "TRIPS LEFT",
            Category: "QUICK GAME",
            Tagline: "Two easy answers on the same side. Almost impossible to cover.",
            Mistake: "Y rounding off his break. Plant hard and run flat to the sideline.",
            Paths:
            [
                new("X", Route, [new(-11, 0), new(-13, 3), new(-13.6, 10)]),
                new("Y", Route, [new(-7.5, 0), new(-8, 4.5), new(-11.4, 4.5)]),
                new("Z", Route, [new(-4, 0), new(-2, 1.5), new(4, 2.5)]),
                new("H", Route, [new(7, 0), new(7, 6), new(10.5, 6)]),
                new("C", Route, [new(0, 0), new(0, 3.5)], Bar),
            ],
            Assign:
            [
                new("X",
                    "Release toward the sideline, then sprint straight downfield. Run the deep defender off and stay outside of Y."),
                new("Y",
                    "Run 4-5 yards, plant, and break out toward the sideline. Show your hands the moment you turn."),
                new("Z",
                    "Run a shallow crossing route about 2 yards deep, all the way across the field. Keep moving."),
                new("H",
                    "Run 6 yards and break out to the right sideline — your backside answer."),
                new("QB",
                    "Read Y first. If the defender is sitting on the out, throw the Z crosser underneath. Two answers, one look."),
            ],
            Notes:
            [
                "The out route to the sideline is the safest throw in flag football — worst case it goes out of bounds.",
                "Z's crossing route is the pressure valve. If he keeps running, he will eventually be wide open.",
                "This is a great third-and-short call. Set Y's break just past the sticks.",
            ]),
        new(
            Num: 8,
            Name: "SNAPPER DELAY",
            Formation: "ACE",
            Category: "QUICK GAME",
            Tagline: "Nobody covers the snapper. Nobody. Ever.",
            Mistake: "The center releasing on the snap. Count it out — the pause IS the play.",
            Paths:
            [
                new("X", Route, [new(-9, 0), new(-9, 12)]),
                new("Z", Route, [new(9, 0), new(9, 12)]),
                new("Y", Route, [new(3, 0), new(3, 6), new(7, 7)]),
                new("H", Route, [new(-1.5, -5), new(-4, -4), new(-8, -3)]),
                new("C", Route, [new(0, 0), new(0, 1), new(-1.8, 6.5)], Delay: true),
            ],
            Assign:
            [
                new("C",
                    "Snap the ball. Count 'one-thousand-one, one-thousand-two' while standing still. THEN release straight up the middle to 6 yards and turn around."),
                new("X / Z",
                    "Sprint as deep as you can. Take everybody with you."),
                new("Y",
                    "Run 6 yards and break out to the right."),
                new("H",
                    "Swing out to the left as the checkdown."),
                new("QB",
                    "Look deep for one count to hold the defense, then come back to the center. He will be alone."),
            ],
            Notes:
            [
                "Confirm your league makes the center an eligible receiver — nearly all 6-on-6 leagues do. If yours does not, run this with Y delaying instead.",
                "The count is what makes it work. If the center leaves immediately, a defender goes with him.",
                "Save this one. It is a change-up, not a base play — it stops working the third time you call it in a game.",
            ]),
        new(
            Num: 9,
            Name: "FLOOD",
            Formation: "TRIPS LEFT",
            Category: "SHOT PLAY",
            Tagline: "Three receivers, three different depths, one side of the field.",
            Mistake: "The QB locking onto the flat route. Eyes go deep first, then work down the ladder.",
            Paths:
            [
                new("X", Route, [new(-11, 0), new(-13.8, 4.5), new(-15, 13.5)]),
                new("Y", Route, [new(-7.5, 0), new(-7.5, 8), new(-11.8, 8)]),
                new("Z", Route, [new(-4, 0), new(-7, 1.2), new(-10.8, 1.8)]),
                new("H", Route, [new(7, 0), new(5, 3.8), new(1.6, 4.8)]),
                new("C", Route, [new(0, 0), new(-0.5, 2.4)], Bar),
            ],
            Assign:
            [
                new("Z",
                    "Sprint to the left flat, about 2 yards deep. You are the shallow answer — get width fast."),
                new("Y",
                    "Run 8 yards straight up, then break out to the sideline. You are the middle answer."),
                new("X",
                    "Release straight toward the sideline, then sprint up the sideline. You are the shot — stay outside of everybody."),
                new("H",
                    "Work back across the middle at 4 yards as the backside checkdown."),
                new("QB",
                    "Count them off deep-to-short: X, then Y, then Z. Take the first one open and get rid of it."),
            ],
            Notes:
            [
                "Three receivers at three depths on one side means the two defenders over there have to pick two. One of yours is always free.",
                "Teach the QB to look deep FIRST. If he looks short first he will never come back to the big play.",
                "Best on 2nd-and-long or any time you want a chunk without a risky throw across the middle.",
            ]),
        new(
            Num: 10,
            Name: "POST / WHEEL",
            Formation: "TWINS RIGHT",
            Category: "SHOT PLAY",
            Tagline: "The back sneaks up the sideline while everyone watches the deep routes.",
            Mistake: "H turning up the sideline too early. Get all the way to the numbers first, then turn straight up.",
            Paths:
            [
                new("H", Route, [new(-2.6, -4.4), new(2, -4), new(8, -3), new(13, -0.5), new(13.5, 12)]),
                new("Z", Route, [new(11, 0), new(9.5, 4), new(5, 12)]),
                new("Y", Route, [new(6, 0), new(6, 6), new(1.5, 6)]),
                new("X", Route, [new(-9, 0), new(-9, 6), new(-12.5, 6)]),
                new("C", Route, [new(0, 0), new(-1.5, 3)], Bar),
            ],
            Assign:
            [
                new("H",
                    "Run out toward the sideline behind the line of scrimmage. Get all the way wide, THEN turn straight up the sideline at full speed. Look back over your outside shoulder."),
                new("Z",
                    "Sprint 4 yards, then angle inside toward the middle of the field on a post. Stay inside — you are clearing the deep defender away from H."),
                new("Y",
                    "Run 6 yards and break in across the middle. Second option."),
                new("X",
                    "Run 6 yards and break out to the sideline. Backside answer."),
                new("QB",
                    "Peek at the post to move the deep defender, then throw the wheel to H up the sideline. Lead him toward the sideline, never inside."),
            ],
            Notes:
            [
                "This is the touchdown call. Nobody at 8U covers a running back who leaves the backfield sideways.",
                "H stays BEHIND the line until he is wide, so he never runs into Z. No screening is allowed and, just as important, 8U receivers who cross paths collide.",
                "Underthrown deep balls get intercepted. Coach the QB: if in doubt, throw it toward the sideline and out of bounds. Call this once a half, on 1st down, after the run has been working.",
            ]),
        new(
            Num: 11,
            Name: "PLAY-ACTION CROSS",
            Formation: "TWINS RIGHT",
            Category: "SHOT PLAY",
            Tagline: "Fake the dive, throw it deep across the middle.",
            Mistake: "A lazy run fake. If H does not sell it with his arms, no defender bites and the cross is covered.",
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(-1.4, -3.7)], To: "H"),
                new("H", Run, [new(-2.6, -4.4), new(-1.4, -3.7), new(1.5, -2.8), new(5.5, -1.8), new(8.5, -1.2)]),
                new("X", Route, [new(-9, 0), new(-9, 3), new(-2, 10), new(5, 12)]),
                new("Z", Route, [new(11, 0), new(11, 13)]),
                new("Y", Route, [new(6, 0), new(4.2, 2.4), new(1.9, 3.2)]),
                new("C", Route, [new(0, 0), new(-2.6, 5)]),
            ],
            Assign:
            [
                new("QB",
                    "Sell the dive fake with both hands, then pull it and set up. Eyes stay on the fake for one full count."),
                new("H",
                    "Run the dive fake hard, then slide out to the right flat as the checkdown."),
                new("X",
                    "Take 3 hard steps upfield, then cross the field on a climbing angle — you should be 12 yards deep by the far hash."),
                new("Z",
                    "Sprint straight downfield. Clear the deep middle out for X."),
                new("Y",
                    "Shallow route across at 3 yards — the safety valve if the deep cross is covered."),
            ],
            Notes:
            [
                "Call this right after 22 Dive has worked. The fake only sells if the defense already respects the run.",
                "X's route takes time to develop. The QB has to trust it and count — this is not a quick-throw play, so avoid it if the rush is winning.",
                "Lead X toward the sideline he is running to. Throw it where he is going, not where he is.",
            ]),
        new(
            Num: 12,
            Name: "SPACING",
            Formation: "SPREAD",
            Category: "NO-RUN ZONE",
            Tagline: "Five receivers, five depths. Somebody is always open.",
            Mistake: "Receivers sitting down with their backs to the QB. Stop, turn, hands up — every time.",
            Paths:
            [
                new("X", Route, [new(-10, 0), new(-10, 12)]),
                new("Y", Route, [new(-5, 0), new(-5, 6), new(-7.2, 6)], Bar),
                new("H", Route, [new(5, 0), new(5, 4), new(7, 4)], Bar),
                new("Z", Route, [new(10, 0), new(10, 12)]),
                new("C", Route, [new(0, 0), new(0, 2)], Bar),
            ],
            Assign:
            [
                new("X / Z",
                    "Sprint straight downfield as fast as you can. Clear out the deep defenders."),
                new("Y",
                    "Run 6 yards, drift 2 yards toward the sideline, and sit down in the open space. Face the QB."),
                new("H",
                    "Run 4 yards, drift toward the sideline, and sit down. Face the QB."),
                new("C",
                    "Snap, release 2 yards straight up, turn around. You are the last resort — always available."),
                new("QB",
                    "Everybody is at a different depth on purpose. Scan Y, then H, then C. Somebody will be standing in grass."),
            ],
            Notes:
            [
                "This is your no-run-zone base call: when the offense cannot run, you need a play with zero chance of a sack-for-loss and four easy targets.",
                "Every receiver turns and FACES the QB when they stop. At 8U, half of all incompletions are receivers who never looked back.",
                "Also a good call after a penalty, or any time your QB looks rattled and needs an easy completion.",
            ]),
        new(
            Num: 13,
            Name: "PYLON FADE",
            Formation: "ACE",
            Category: "GOAL LINE",
            Tagline: "Two-level attack at the corner of the end zone.",
            Mistake: "Throwing the fade low or inside. High and toward the sideline, or do not throw it at all.",
            Paths:
            [
                new("Z", Route, [new(9, 0), new(11, 3), new(12.5, 6.5)]),
                new("Y", Route, [new(3, 0), new(3, 1.5), new(8, 2.5)]),
                new("X", Route, [new(-9, 0), new(-11, 3), new(-12.5, 6.5)]),
                new("H", Route, [new(-1.5, -5), new(-3.5, -3), new(-6.5, -1.5)]),
                new("C", Route, [new(0, 0), new(-2, 2), new(-5, 2)]),
            ],
            Assign:
            [
                new("Z",
                    "Sprint on an angle toward the back corner of the end zone (the back pylon). Look over your outside shoulder."),
                new("Y",
                    "Run 2 yards and break flat to the front corner. You are the quick, easy score."),
                new("X",
                    "Same fade on the backside — the answer if the defense overloads the right."),
                new("H",
                    "Swing left as the checkdown if nothing opens up."),
                new("QB",
                    "Y first — if the front corner is open it is a walk-in. If not, throw Z high and toward the back pylon where only he can get it."),
            ],
            Notes:
            [
                "Inside the 5-yard line you are almost certainly in a no-run zone, so plan on throwing it.",
                "On the fade, high and outside is the only safe miss. A low or inside throw is an interception.",
                "Y's flat route and Z's fade go opposite directions on purpose — no screening allowed, so they must never cross.",
            ]),
        new(
            Num: 14,
            Name: "TRIPLE OUT",
            Formation: "TRIPS LEFT",
            Category: "GOAL LINE",
            Tagline: "Three staircase outs plus a shot. Your extra-point and short-yardage call.",
            Mistake: "Two receivers ending up at the same depth. 1, 3, 5 — drill it with cones until it is automatic.",
            Paths:
            [
                new("X", Route, [new(-11, 0), new(-12.4, 2.5), new(-12.4, 5), new(-14.4, 5)]),
                new("Y", Route, [new(-7.5, 0), new(-7.5, 3), new(-11, 3)]),
                new("Z", Route, [new(-4, 0), new(-4, 1.8), new(-6.6, 1.8)]),
                new("H", Route, [new(7, 0), new(5, 7), new(0, 9)]),
                new("QB", Run, [new(0, -3), new(-3.5, -4), new(-6.5, -3.5)]),
                new("C", Route, [new(0, 0), new(1, 3)], Bar),
            ],
            Assign:
            [
                new("Z",
                    "Break out at about 1 yard — the shallowest step of the staircase. You are the fastest, shortest answer."),
                new("Y",
                    "Break out at 3 yards. Never drift into Z's depth."),
                new("X",
                    "Release wide right away, get to 5 yards, then break out. Stay on top of and outside of both of them."),
                new("H",
                    "Backside post across the middle — the shot if everyone chases the outs."),
                new("QB",
                    "Roll to the left. Three receivers are breaking out in a staircase in front of you. Throw the highest open one and get to the sideline if nobody is."),
            ],
            Notes:
            [
                "The staircase is the whole point: 1 yard, 3 yards, 5 yards. Drill the depths — if two receivers end up at the same depth, they cover each other.",
                "Perfect for a 1-point (5-yard) or 2-point (10-yard) conversion — pick the receiver whose depth matches the line to gain.",
                "Rolling the QB out buys time against the 7-yard rusher and turns a stationary throw into an easy one.",
            ]),
    ];
}
