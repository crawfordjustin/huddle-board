using static HuddleBoard.Playbook.EndStyle;
using static HuddleBoard.Playbook.PathType;

namespace HuddleBoard.Playbook;

/// <summary>
/// Second batch of concepts, plays 15 onward. Chosen to fill the gaps in the first
/// fourteen rather than to hit a number: NO-RUN ZONE went from one concept to
/// four, RUN ZONE gained the two actions 6-on-6 flag has left once you own dive,
/// sweep, keep and pitch, and SHOT PLAY gained FOUR VERTS, which is the default
/// rule made into a play. Every route here decomposes into the nine shapes.
/// Nothing needed a tenth.
/// </summary>
/// <remarks>New plays go here. See CLAUDE.md, "Adding a play".</remarks>
public static partial class PlayLibrary
{
    private static readonly IReadOnlyList<Play> Recent =
    [
        new(
            Num: 15,
            Name: "ALL SIT",
            Formation: "SPREAD",
            Category: "NO-RUN ZONE",
            Tagline: "Everybody runs five and turns around. The simplest play in the book.",
            Mistake: "Drifting upfield after the turn. Stop means stop — if he keeps drifting, the throw goes behind him.",
            Paths:
            [
                new("X", Route, [new(-10, 0), new(-10, 5)], Bar),
                new("Y", Route, [new(-5, 0), new(-5, 5)], Bar),
                new("H", Route, [new(5, 0), new(5, 5)], Bar),
                new("Z", Route, [new(10, 0), new(10, 5)], Bar),
                new("C", Route, [new(0, 0), new(0, 4)], Bar),
            ],
            Assign:
            [
                new("X / Y / H / Z",
                    "Sprint 5 yards, stop, turn around and face the thrower. Hands up."),
                new("C",
                    "Snap it, run 4 yards, turn around. You are the safety valve."),
                new("QB",
                    "Throw to whoever turned around first with nobody near him. Any of the five is right."),
            ],
            Notes:
            [
                "This is the play to call when you are out of timeouts, out of ideas, or the kids are rattled. Nobody can run it wrong.",
                "Five yards apart across the whole formation means no two kids can collide even if they run the wrong way.",
                "Great first no-run-zone install: it teaches the SIT shape to all six at once.",
            ]),
        new(
            Num: 16,
            Name: "SLANT FLAT",
            Formation: "TWINS RIGHT",
            Category: "NO-RUN ZONE",
            Tagline: "One in, one out, same side. Whichever way the defender goes, he is wrong.",
            Mistake: "The slot rounding his break. Plant the outside foot and cut flat across, or the defender stays on top of him.",
            Paths:
            [
                new("Y", Route, [new(6, 0), new(6, 6), new(2.2, 6)]),
                new("Z", Route, [new(11, 0), new(11, 3), new(14.2, 3)]),
                new("X", Route, [new(-9, 0), new(-9, 10)]),
                new("H", Route, [new(-2.6, -4.4), new(-5.6, -3.4), new(-9.4, -2.7)]),
                new("C", Route, [new(0, 0), new(0, 4)], Bar),
            ],
            Assign:
            [
                new("Y",
                    "Run 6 yards, plant, and break flat across the middle. You are the first look."),
                new("Z",
                    "Run 3 yards and break straight out to the sideline. Stay shallow — you are pulling the corner down and away from Y."),
                new("X",
                    "Sprint straight downfield. Take the backside defender with you."),
                new("H",
                    "Loop out of the backfield to the left sideline and look back. Late outlet."),
                new("C",
                    "Snap, 4 yards, turn around."),
                new("QB",
                    "Look at Y first. If the middle is crowded, come back to Z in the flat."),
            ],
            Notes:
            [
                "The two routes are on the same side but 3 yards apart in depth, so they never cross and never rub — which is what makes it legal.",
                "If the defense starts jumping the flat, this is when Play 20 (Double Post) hits.",
            ]),
        new(
            Num: 19,
            Name: "HIGH LOW",
            Formation: "TRIPS LEFT",
            Category: "NO-RUN ZONE",
            Tagline: "Two outs to the same sideline, one deep and one shallow. Pick a level.",
            Mistake: "Both receivers breaking at the same depth. If they stack up, the play is dead and it looks like a screen.",
            Paths:
            [
                new("Z", Route, [new(-4, 0), new(-4, 3), new(-7.6, 3)]),
                new("X", Route, [new(-11, 0), new(-11, 8), new(-14.3, 8)]),
                new("Y", Route, [new(-7.5, 0), new(-8.8, 2.0), new(-8.8, 12)]),
                new("H", Route, [new(7, 0), new(7, 10)]),
                new("C", Route, [new(0, 0), new(0, 5)], Bar),
            ],
            Assign:
            [
                new("Z",
                    "Run 3 yards and break out toward the sideline. You are the LOW. First look."),
                new("X",
                    "Run 8 yards and break out toward the sideline. You are the HIGH."),
                new("Y",
                    "Take one step toward the sideline, then sprint straight up. You clear the window the low route breaks into."),
                new("H",
                    "Sprint downfield on the far side. Take your man out of the picture."),
                new("C",
                    "Snap, 5 yards, turn around."),
                new("QB",
                    "One defender cannot cover both. Throw to the one he leaves."),
            ],
            Notes:
            [
                "Five yards of separation between the two break points is what makes this safe to run with 8-year-olds. Do not let them creep together.",
                "Teach the read out loud on the sideline: 'if he comes up, throw over him.'",
            ]),
        new(
            Num: 22,
            Name: "REVERSE",
            Formation: "SPREAD",
            Category: "RUN ZONE",
            Tagline: "Hand it one way, hand it back the other. Beats a defense that chases.",
            Mistake: "The second runner taking off before he has the ball. Take it first, then go.",
            Paths:
            [
                new("H", Motion, [new(5, 0), new(2.2, -1.6), new(-0.4, -1.8)]),
                new("QB", Handoff, [new(0, -3), new(-1.1, -2.0)], To: "H"),
                // H carries toward blue; Y comes back underneath him and takes it
                // going the other way. The two lines are drawn 0.75 yd apart so
                // the second exchange is visible as its own arrow, and the timing
                // is real: both kids reach x = -5.1 about 0.78 s after the snap.
                new("H", Run, [new(-0.4, -1.8), new(-3.4, -1.6), new(-6.2, -1.4)]),
                new("H", Handoff, [new(-5.1, -1.5), new(-5.1, -2.25)], To: "Y"),
                new("Y", Run, [new(-5, 0), new(-7, -1.8), new(-3.2, -2.7), new(2.5, -2.2), new(8.2, 0.6), new(10, 8)]),
                new("X", Route, [new(-10, 0), new(-10, 9)]),
                new("Z", Route, [new(10, 0), new(10, 5), new(5.4, 5)]),
                new("C", Route, [new(0, 0), new(-1.8, 4.5)]),
            ],
            Assign:
            [
                new("H",
                    "Start moving before the snap. Take the ball going left, run two steps, then hand it to Y coming the other way. Keep running left after you give it up."),
                new("Y",
                    "Let H get past you, take the ball with both hands, and run all the way to the right sideline before you turn up."),
                new("QB",
                    "Hand it to H on the move. Do not watch the rest — turn and look left."),
                new("Z",
                    "Break inside at 5 yards. You are clearing the right sideline for Y."),
                new("X",
                    "Sprint downfield and take the corner with you."),
                new("C",
                    "Snap and release up the middle, away from both exchanges."),
            ],
            Notes:
            [
                "Only call this after Jet Sweep has worked. The reverse is the punishment for chasing; with nothing to chase it is just a slow run.",
                "Two exchanges means two chances to fumble. Practise it at walking speed first.",
                "Z breaking IN is not decoration — without it he is standing in the exact spot Y is trying to reach.",
            ]),
        new(
            Num: 17,
            Name: "BUBBLE",
            Formation: "TRIPS LEFT",
            Category: "QUICK GAME",
            Tagline: "Catch it wide with room to run. The answer to a hard rush.",
            Mistake: "Throwing it backwards. The catch has to be in front of the thrower or it is a live ball if it drops.",
            Paths:
            [
                new("Z", Route, [new(-4, 0), new(-6.6, -0.8), new(-9.6, 0.6)]),
                new("Y", Route, [new(-7.5, 0), new(-7.5, 9)]),
                new("X", Route, [new(-11, 0), new(-11, 10)]),
                new("H", Route, [new(7, 0), new(7, 10)]),
                new("C", Route, [new(0, 0), new(0, 4)], Bar),
            ],
            Assign:
            [
                new("Z",
                    "Open out toward the sideline and get width fast, staying level with the line. Look back for the ball immediately."),
                new("Y",
                    "Sprint straight downfield. Take your defender away from Z."),
                new("X",
                    "Sprint straight downfield and stay wide."),
                new("H",
                    "Sprint downfield on the far side."),
                new("C",
                    "Snap, 4 yards, turn around."),
                new("QB",
                    "Catch and throw in one motion. This is the fastest ball you will throw all game."),
            ],
            Notes:
            [
                "The whole play is over in two seconds, which is exactly why it beats a rusher who starts 7 yards away.",
                "Coach the catch point: Z must be even with or in front of the thrower. A backward pass is a lateral, and at 8U a loose ball on the ground is chaos.",
            ]),
        new(
            Num: 24,
            Name: "SNAG",
            Formation: "TRIPS LEFT",
            Category: "QUICK GAME",
            Tagline: "Three receivers, three different depths, one side of the field.",
            Mistake: "The sit drifting toward the sideline. He should settle in the window and stay there.",
            Paths:
            [
                new("Z", Route, [new(-4, 0), new(-5.6, 5)], Bar),
                new("X", Route, [new(-11, 0), new(-11, 5), new(-14.6, 9.5)]),
                new("Y", Route, [new(-7.5, 0), new(-9.6, -1.4), new(-12.4, -1.1)]),
                new("H", Route, [new(7, 0), new(7, 10)]),
                new("C", Route, [new(0, 0), new(0.6, 6)]),
            ],
            Assign:
            [
                new("Z",
                    "Run 5 yards, drift slightly outside, and sit down facing the thrower. First look."),
                new("X",
                    "Run 5 yards and break at an angle for the deep corner."),
                new("Y",
                    "Loop behind the line toward the sideline and look back. You are the checkdown."),
                new("H",
                    "Sprint downfield on the far side."),
                new("C",
                    "Snap and release straight up the middle."),
                new("QB",
                    "Z first. If he is covered, the corner is behind it and the loop is underneath it."),
            ],
            Notes:
            [
                "Three levels on one side — deep, medium, behind the line. Whoever the defense takes away, the other two are open.",
                "This is the most grown-up concept in the book and it still uses only CORNER, SIT and SWING. No new vocabulary.",
            ]),
        new(
            Num: 18,
            Name: "FOUR VERTS",
            Formation: "SPREAD",
            Category: "SHOT PLAY",
            Tagline: "Everybody goes. The default rule, called on purpose.",
            Mistake: "Drifting together downfield. They start 5 yards apart and they must finish 5 yards apart.",
            Paths:
            [
                new("X", Route, [new(-10, 0), new(-10.5, 12)]),
                new("Y", Route, [new(-5, 0), new(-5, 12)]),
                new("H", Route, [new(5, 0), new(5, 12)]),
                new("Z", Route, [new(10, 0), new(10.5, 12)]),
                new("C", Route, [new(0, 0), new(0, 8)]),
            ],
            Assign:
            [
                new("X / Z",
                    "Sprint straight up the sideline. Do not drift inside."),
                new("Y / H",
                    "Sprint straight up your lane. Split the middle of the field."),
                new("C",
                    "Snap and sprint 8 yards up the middle."),
                new("QB",
                    "Take the deepest one who has nobody behind him. If they are all covered, throw it away — this is a shot, not a scramble."),
            ],
            Notes:
            [
                "Every kid already knows this play, because it is the default rule: if the coach did not give you a job, run GO. Call it when the huddle is a mess.",
                "Five lanes, five yards apart, nobody crosses anybody. It is the safest deep call in the book.",
                "Best used right after a run has worked. The defense creeps up, and there is nobody home.",
            ]),
        new(
            Num: 20,
            Name: "DOUBLE POST",
            Formation: "TWINS RIGHT",
            Category: "SHOT PLAY",
            Tagline: "Two receivers attack the deep middle at different depths. Somebody is open.",
            Mistake: "Both breaking at the same yard line. The staggered depth is the whole play.",
            Paths:
            [
                new("Y", Route, [new(6, 0), new(6, 8), new(1.2, 13.6)]),
                new("Z", Route, [new(11, 0), new(11, 4.5), new(6.6, 9.5)]),
                new("X", Route, [new(-9, 0), new(-9, 5), new(-12.6, 5)]),
                new("H", Route, [new(-2.6, -4.4), new(-5.6, -3.6), new(-8.8, -3.0)]),
                new("C", Route, [new(0, 0), new(0, 3)], Bar),
            ],
            Assign:
            [
                new("Y",
                    "Run 8 yards, then break at an angle for the deep middle. You are the deep one."),
                new("Z",
                    "Run 4 yards, then break at an angle inside. You are underneath Y — stay under him."),
                new("X",
                    "Run 5 yards and break out. Backside answer if they take both posts."),
                new("H",
                    "Loop out of the backfield to the left and look back."),
                new("C",
                    "Snap, 3 yards, turn around."),
                new("QB",
                    "One deep defender cannot take both. Throw over him or in front of him."),
            ],
            Notes:
            [
                "The two posts break 3.5 yards apart in depth and finish 5 yards apart. Drill the stagger — if they end up side by side it is a collision, not a concept.",
                "Call it after Slant Flat has pulled the defense up.",
            ]),
        new(
            Num: 21,
            Name: "DRAW",
            Formation: "TWINS RIGHT",
            Category: "RUN ZONE",
            Tagline: "Everybody runs pass routes. Then you hand it off anyway.",
            Mistake: "Handing it off too early. Let the rusher commit upfield first — count one, then give it.",
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(-1.5, -4.9)], To: "H"),
                new("H", Run, [new(-2.6, -4.4), new(-3.3, -5.5), new(-1.5, -4.9), new(0.9, -1.6), new(2.0, 7.5)]),
                new("X", Route, [new(-9, 0), new(-9, 6), new(-12.5, 6)]),
                new("Y", Route, [new(6, 0), new(6, 6), new(9.6, 6)]),
                new("Z", Route, [new(11, 0), new(11, 11)]),
                new("C", Route, [new(0, 0), new(-2.6, 5)]),
            ],
            Assign:
            [
                new("QB",
                    "Catch the snap and take one step back like you are going to throw. Let the rusher come. Then hand it to H."),
                new("H",
                    "Take a small step back, wait one count, take the ball and run straight up past the snapper's right hip."),
                new("X / Y",
                    "Run 6 yards and break out. Sell it — you are pulling defenders sideways."),
                new("Z",
                    "Sprint straight downfield."),
                new("C",
                    "Snap, then release left and stay out of the running lane."),
            ],
            Notes:
            [
                "The rusher has to start 7 yards back, so he arrives with a full head of steam. This play uses that against him — he runs himself out of the play.",
                "Only works if the receivers sell it. If they jog, the defenders never leave.",
                "Call it the down after a pass, never the down after a run.",
            ]),
        new(
            Num: 23,
            Name: "FLAT DUMP",
            Formation: "ACE",
            Category: "GOAL LINE",
            Tagline: "Clear everybody out of the end zone and dump it to the back.",
            Mistake: "The back turning upfield before he catches it. Catch first, then turn.",
            Paths:
            [
                new("H", Route, [new(-1.5, -5), new(1.8, -4.2), new(5.4, -3.4), new(7.6, -2.4)]),
                new("Z", Route, [new(9, 0), new(9, 4), new(13.4, 8.5)]),
                new("Y", Route, [new(3, 0), new(3, 9)]),
                new("X", Route, [new(-9, 0), new(-9, 8)]),
                new("C", Route, [new(0, 0), new(-3, 3)], Bar),
            ],
            Assign:
            [
                new("H",
                    "Loop out of the backfield toward the right sideline. Stay behind everybody, look back, catch it, THEN turn upfield."),
                new("Z",
                    "Run 4 yards and break for the corner. You are taking the corner defender out of the end zone."),
                new("Y",
                    "Sprint straight into the end zone. Take the middle defender with you."),
                new("X",
                    "Sprint into the end zone on the far side."),
                new("C",
                    "Snap and drift left. Stay out of the throwing lane."),
                new("QB",
                    "Everyone else is running away from the flat. Turn and dump it to H."),
            ],
            Notes:
            [
                "Inside the 5, every defender's eyes go to the end zone. The flat is the emptiest grass on the field.",
                "Pairs with Pylon Fade out of the same formation — same picture, opposite answer.",
            ]),
        new(
            Num: 25,
            Name: "JET REVERSE",
            Formation: "TRIPS LEFT",
            Category: "RUN ZONE",
            Tagline: "Jet Sweep one way, hand it back the other. The reverse out of your trips look.",
            Mistake: "The second runner leaving before he has the ball. Let the sweep man get past you, take it, then go.",
            Paths:
            [
                // the same motion and the same handoff as Jet Sweep, so the first
                // second of the play is a picture the defense has already chased
                new("Y", Motion, [new(-7.5, 0), new(-4.5, -1.6), new(-1.5, -1.6)]),
                new("QB", Handoff, [new(0, -3), new(-1, -1.8)], To: "Y"),
                new("Y", Run, [new(-1.5, -1.6), new(2.9, -1.3), new(7, -0.4), new(9.5, 1.2)]),
                // H comes back underneath the sweep and takes it going the other
                // way; both kids reach x = 3 about 0.76 s after the snap
                new("Y", Handoff, [new(2.9, -1.3), new(3.2, -2.0)], To: "H"),
                new("H", Run, [new(7, 0), new(5.4, -1.2), new(3.2, -2.0), new(-2, -2.8), new(-7, -2.4), new(-10.5, 0.5), new(-13, 8)]),
                new("X", Route, [new(-11, 0), new(-11, 5), new(-7.6, 5)]),
                new("Z", Route, [new(-4, 0), new(-4.5, 9)]),
                new("C", Route, [new(0, 0), new(0, 5)]),
            ],
            Assign:
            [
                new("Y",
                    "Same start as Jet Sweep: full speed past the QB, take the ball going right. Two more steps, then hand it to H coming the other way. Keep running right after you give it up."),
                new("H",
                    "Come back behind the QB. Let Y get past you, take the ball with both hands, and run all the way to the left sideline before you turn up."),
                new("QB",
                    "Hand it to Y on the move, exactly like Jet Sweep. Then turn and look left — do not watch the sweep."),
                new("X",
                    "Break inside at 5 yards. You are clearing the left sideline for H."),
                new("Z",
                    "Sprint downfield and take your defender with you."),
                new("C",
                    "Snap and release up the middle, away from both exchanges."),
            ],
            Notes:
            [
                "Jet Sweep first, then this. The reverse only works on a defense that has started chasing the motion.",
                "Two exchanges means two chances to fumble. Practise it at walking speed first.",
                "This is Reverse (22) out of a different formation. A team that knows one already knows the other.",
            ]),
        new(
            Num: 26,
            Name: "PITCH REVERSE",
            Formation: "ACE",
            Category: "RUN ZONE",
            Tagline: "Pitch it to the edge, then hand it back against the grain. The reverse out of the backfield.",
            Mistake: "The second runner taking it standing still. Be moving toward the far sideline when the ball arrives.",
            Paths:
            [
                // the same pitch as Pitch Right, so the defense sees the play it
                // has been chasing
                new("QB", Handoff, [new(0, -3), new(3.5, -4.2)], To: "H"),
                new("H", Run, [new(-1.5, -5), new(2, -4.2), new(5, -3.0), new(8.5, -2.0), new(11, 0.5)]),
                // Z drops in behind H and takes it going the other way; both kids
                // are at x = 4.6 about 1.0 s after the snap
                new("H", Handoff, [new(5, -3.0), new(4.6, -3.8)], To: "Z"),
                new("Z", Run, [new(9, 0), new(8.2, -2.4), new(4.6, -3.8), new(0, -4.4), new(-5, -3.8), new(-9.5, -0.5), new(-12, 7)]),
                new("X", Route, [new(-9, 0), new(-9, 5), new(-5.6, 5)]),
                new("Y", Route, [new(3, 0), new(3.5, 9)]),
                new("C", Route, [new(0, 0), new(-0.5, 5)]),
            ],
            Assign:
            [
                new("H",
                    "Same start as Pitch Right: run for the right sideline and catch the pitch on the move. Two more steps, then hand it to Z coming back the other way. Keep running right after you give it up."),
                new("Z",
                    "Drop back behind H. Let him get past you, take the ball with both hands, and run all the way to the left sideline before you turn up."),
                new("QB",
                    "Pitch it out in front of H, exactly like Pitch Right. Then turn and look left."),
                new("X",
                    "Break inside at 5 yards. You are clearing the left sideline for Z."),
                new("Y",
                    "Sprint downfield and take your defender with you."),
                new("C",
                    "Snap and release up the middle, away from both exchanges."),
            ],
            Notes:
            [
                "Pitch Right first, then this. The pitch has to have gone to the edge a couple of times before anybody bites on it.",
                "A pitch is already a live ball, and the reverse adds a second exchange. Walk it at practice before you call it in a game.",
                "This is Reverse (22) out of the backfield. One concept, three formations — that is how the library grows.",
            ]),

    ];
}
