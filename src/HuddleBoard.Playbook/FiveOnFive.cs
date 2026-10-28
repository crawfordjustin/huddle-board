using static HuddleBoard.Playbook.EndStyle;
using static HuddleBoard.Playbook.PathType;

namespace HuddleBoard.Playbook;

/// <summary>
/// 5-on-5: the SNAPPER, the THROWER and three more. Its own four formations
/// and its own library, not 6-on-6 with a kid taken away — with one fewer
/// receiver the field is wider for everybody, so the concepts lean on space:
/// one-on-one routes, a back who can leak, and a whole side left empty.
/// </summary>
/// <remarks>
/// Formation names are unique across every format. Where an exchange point is
/// the same as one in the 6-on-6 library (the dive, the jet, the pitch, the
/// pitch reverse), the geometry is the same too, because those are timings the
/// checker has already held.
/// </remarks>
public static class FiveOnFive
{
    public static readonly OrderedDictionary<string, OrderedDictionary<string, Pt>> Formations = new()
    {
        ["DOUBLES"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["H"] = new(-2.6, -4.4),
            ["X"] = new(-10, 0),
            ["Z"] = new(10, 0),
        },
        ["EMPTY"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["X"] = new(-10, 0),
            ["H"] = new(5, 0),
            ["Z"] = new(10, 0),
        },
        ["TRIO RIGHT"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["H"] = new(4, 0),
            ["Y"] = new(7.5, 0),
            ["Z"] = new(11, 0),
        },
        ["POWER RIGHT"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["H"] = new(-1.5, -5),
            ["Y"] = new(3, 0),
            ["Z"] = new(9, 0),
        },
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, SpotLabel>> SpotMap =
        new Dictionary<string, IReadOnlyDictionary<string, SpotLabel>>
        {
            ["DOUBLES"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["H"] = new("B", "BACK"),
                ["X"] = new("WL", "WIDE LEFT"),
                ["Z"] = new("WR", "WIDE RIGHT"),
            },
            ["EMPTY"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["X"] = new("WL", "WIDE LEFT"),
                ["H"] = new("SR", "SLOT RIGHT"),
                ["Z"] = new("WR", "WIDE RIGHT"),
            },
            ["TRIO RIGHT"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["H"] = new("TR", "TIGHT RIGHT"),
                ["Y"] = new("SR", "SLOT RIGHT"),
                ["Z"] = new("WR", "WIDE RIGHT"),
            },
            ["POWER RIGHT"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["H"] = new("B", "BACK"),
                ["Y"] = new("TR", "TIGHT RIGHT"),
                ["Z"] = new("WR", "WIDE RIGHT"),
            },
        };

    public static readonly IReadOnlyDictionary<string, string> Notes = new Dictionary<string, string>
    {
        ["DOUBLES"] =
            "Your base look. One wide each side and a BACK, so the defense cannot tell run from pass and has to cover the whole width.",
        ["EMPTY"] =
            "Three receivers and nobody in the backfield. The SLOT is your motion man: he is the jet, the fake and the halfback pass.",
        ["TRIO RIGHT"] =
            "Everybody on one side. Three receivers at three depths against two defenders, and the other half of the field is the THROWER's to roll into.",
        ["POWER RIGHT"] =
            "Tight, with a BACK deep. Your run look and your goal line look, and where the pitch and the reverse come from.",
    };

    private static readonly IReadOnlyList<Authored> Library =
    [
        // ------------------------------------------------------------ DOUBLES
        new(
            Num: 1, Name: "51 DIVE", Kid: "TRUCK",
            Formation: "DOUBLES", Category: "RUN ZONE", Complexity: PlayComplexity.Simple,
            Tagline: "Hand it to the BACK and go straight ahead. Your first-down play.",
            Mistake: "The Back drifting outside. With only five on the field the middle is wide open — hit it.",
            Ball: ("carry", "H"),
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(-1.4, -3.7)], To: "H"),
                new("H", Run, [new(-2.6, -4.4), new(-1.4, -3.7), new(2.2, 0.5), new(3, 8)]),
                new("X", Route, [new(-10, 0), new(-10, 10)]),
                new("Z", Route, [new(10, 0), new(10, 10)]),
                new("C", Route, [new(0, 0), new(-2.5, 5)]),
            ],
            Calls:
            [
                new("BACK", "CARRY — straight ahead"),
                new("THROWER", "hand it off, don't watch"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "CARRY. One step toward the thrower, take it with both hands, and run straight past the snapper's right hip."),
                new("THROWER", "Catch the snap, turn, and put the ball in the Back's belly. Do not watch him run."),
                new("SNAPPER", "Snap it, then GO up the field, away from the Back's lane."),
                new("WIDE LEFT / WIDE RIGHT", "GO. Sprint downfield and take your defender as far from the ball as you can."),
            ],
            Notes:
            [
                "With three defenders and a rusher, the wides running off leaves one kid to stop the Back. Win that and it is a big gain.",
                "A brand-new kid can play Back on this one: one job, one direction, and you can walk him to the hole before the snap.",
            ]),
        new(
            Num: 2, Name: "QUICK IN", Kid: "ZIGZAG",
            Formation: "DOUBLES", Category: "QUICK GAME", Complexity: PlayComplexity.Simple,
            Tagline: "Both wides break in at four. Throw to whichever one is open first.",
            Mistake: "Breaking in too deep. Four steps, plant, cut — the ball is coming right now.",
            Ball: ("pass", "Z"),
            Paths:
            [
                new("X", Route, [new(-10, 0), new(-10, 4), new(-6, 4)]),
                new("Z", Route, [new(10, 0), new(10, 4), new(6, 4)]),
                new("H", Route, [new(-2.6, -4.4), new(-5.6, -2.9), new(-8.6, -2.4)]),
                new("C", Route, [new(0, 0), new(0, 4)], Bar),
            ],
            Calls:
            [
                new("WIDE RIGHT", "IN at 4 — first look"),
                new("WIDE LEFT", "IN at 4"),
                new("BACK", "SWING to the left flat"),
                new("SNAPPER", "SIT at 4"),
            ],
            Assign:
            [
                new("WIDE RIGHT", "IN at 4. Four steps, plant on your outside foot, and cut toward the middle. Hands up — it comes fast."),
                new("WIDE LEFT", "IN at 4. The same cut on your side. If the thrower looks your way, the ball is already in the air."),
                new("BACK", "SWING. Loop out to the left flat and look back for the ball."),
                new("SNAPPER", "Snap it, run four steps, and SIT facing the thrower."),
                new("THROWER", "Look at the Wide Right first. If he is covered, the Snapper is sitting right in front of you."),
            ],
            Notes:
            [
                "This is the play to run when you need four yards and the defense is playing deep.",
                "The two IN routes finish far apart, so there is no traffic in the middle — only the Snapper sitting under them.",
            ]),
        new(
            Num: 3, Name: "BACK WHEEL", Kid: "FERRIS WHEEL",
            Formation: "DOUBLES", Category: "SHOT PLAY", Complexity: PlayComplexity.Moderate,
            Tagline: "The BACK swings out and turns up the sideline while the Wide Left clears the corner.",
            Mistake: "The Back turning up too early. Get wide first, then up — the sideline is his lane.",
            Ball: ("pass", "H"),
            Paths:
            [
                new("H", Route, [new(-2.6, -4.4), new(-5.5, -3.5), new(-7.5, -1), new(-7.8, 10)]),
                new("X", Route, [new(-10, 0), new(-10, 6), new(-5.5, 11)]),
                new("Z", Route, [new(10, 0), new(10, 10)]),
                new("C", Route, [new(0, 0), new(0, 5)], Bar),
            ],
            Calls:
            [
                new("BACK", "WHEEL up the left side — the shot"),
                new("WIDE LEFT", "POST — take the defender inside"),
                new("WIDE RIGHT", "GO"),
                new("SNAPPER", "SIT at 5"),
            ],
            Assign:
            [
                new("BACK", "WHEEL. Run out behind the line toward the left sideline, get wide, then turn straight up it and look back."),
                new("WIDE LEFT", "POST. Run up six, then break toward the middle. Your defender has to come with you, and that leaves the sideline empty."),
                new("WIDE RIGHT", "GO. Sprint straight downfield."),
                new("SNAPPER", "Snap it, run five steps and SIT. You are the safe throw."),
                new("THROWER", "Watch the Back. When he turns up the sideline and nobody turned with him, throw it out in front of him."),
            ],
            Notes:
            [
                "Run the dive at least once first. The defender who comes up to stop the Back is the one he runs past.",
                "If the defense stays deep, the Snapper is open underneath every time.",
            ]),
        new(
            Num: 4, Name: "CORNER SIT", Kid: "MAILBOX",
            Formation: "DOUBLES", Category: "QUICK GAME", Complexity: PlayComplexity.Moderate,
            Tagline: "The Wide Right sits at five, the Snapper bends to the corner over him. One defender cannot cover both.",
            Mistake: "The Snapper rounding his corner off. Six steps straight, then break — or he runs into the sit.",
            Ball: ("pass", "C"),
            Paths:
            [
                new("Z", Route, [new(10, 0), new(10, 5)], Bar),
                new("C", Route, [new(0, 0), new(0, 6), new(4.5, 11)]),
                new("X", Route, [new(-10, 0), new(-10, 10)]),
                new("H", Route, [new(-2.6, -4.4), new(-5.6, -2.9), new(-8.6, -2.4)]),
            ],
            Calls:
            [
                new("SNAPPER", "CORNER — first look"),
                new("WIDE RIGHT", "SIT at 5"),
                new("BACK", "SWING to the left flat"),
                new("WIDE LEFT", "GO"),
            ],
            Assign:
            [
                new("SNAPPER", "CORNER. Snap it, run six steps straight up, then break toward the right corner. Look back over your outside shoulder."),
                new("WIDE RIGHT", "SIT. Run five steps, stop, and turn around facing the thrower."),
                new("BACK", "SWING. Loop out to the left flat and look back."),
                new("WIDE LEFT", "GO. Sprint downfield and take your defender away."),
                new("THROWER", "Watch the defender on the right. If he jumps the sit, throw the corner. If he stays deep, throw the sit."),
            ],
            Notes:
            [
                "The read is one defender. Teach the thrower to look at him, not at the receivers.",
                "Good third-down call: the sit is a sure five yards and the corner is a touchdown.",
            ]),
        new(
            Num: 5, Name: "QUICK TOSS", Kid: "SKATEBOARD",
            Formation: "DOUBLES", Category: "RUN ZONE", Complexity: PlayComplexity.Moderate,
            Tagline: "Hand it to the BACK going left and race to the edge.",
            Mistake: "The Back cutting up inside the Wide Left. Get to the sideline first, then turn up.",
            Ball: ("carry", "H"),
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(-1.6, -3.6)], To: "H"),
                new("H", Run, [new(-2.6, -4.4), new(-2.2, -4.0), new(-5.5, -3.2), new(-8.8, -0.5), new(-6.5, 8)]),
                new("X", Route, [new(-10, 0), new(-10, 10)]),
                new("Z", Route, [new(10, 0), new(10, 10)]),
                new("C", Route, [new(0, 0), new(1, 8)]),
            ],
            Calls:
            [
                new("BACK", "CARRY to the left sideline, then up"),
                new("THROWER", "hand it off, don't watch"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "CARRY. Step toward the thrower, take the ball, and race to the left sideline. Turn up only when you get there."),
                new("THROWER", "Catch the snap, step left, and hand it to the Back as he goes by."),
                new("WIDE LEFT", "GO. Sprint straight downfield and take the corner with you — the sideline is the Back's."),
                new("WIDE RIGHT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "The pair to 51 Dive: the same handoff, the other direction. Call it once the defense crowds the middle.",
                "The Wide Left is the key. If he stops to watch, his defender is standing where the Back needs to go.",
            ]),
        new(
            Num: 6, Name: "FAKE DIVE POST", Kid: "SNEAKY PETE",
            Formation: "DOUBLES", Category: "SHOT PLAY", Complexity: PlayComplexity.Moderate,
            Tagline: "Fake 51 Dive, then throw the Wide Right's post behind the defense that bit.",
            Mistake: "A lazy fake. If the Back does not sell it, nobody comes up and the post is covered.",
            Ball: ("pass", "Z"),
            Paths:
            [
                new("H", Fake, [new(-2.6, -4.4), new(-1.4, -3.7), new(1.5, -1)]),
                new("Z", Route, [new(10, 0), new(10, 6), new(5.5, 11)]),
                new("X", Route, [new(-10, 0), new(-10, 6), new(-14.5, 11)]),
                new("C", Route, [new(0, 0), new(0, 5)], Bar),
            ],
            Calls:
            [
                new("BACK", "FAKE the dive — sell it"),
                new("WIDE RIGHT", "POST — the shot"),
                new("WIDE LEFT", "CORNER"),
                new("SNAPPER", "SIT at 5"),
            ],
            Assign:
            [
                new("BACK", "FAKE. Run exactly like 51 Dive, arms folded like you have the ball, all the way to the line."),
                new("WIDE RIGHT", "POST. Run up six, then break toward the middle of the field. Look for the ball over your inside shoulder."),
                new("WIDE LEFT", "CORNER. Run up six, then break toward the left corner."),
                new("SNAPPER", "Snap it, run five steps and SIT."),
                new("THROWER", "Fake to the Back, then look at the Wide Right. If a defender came up for the fake, throw the post."),
            ],
            Notes:
            [
                "Only after the defense has seen 51 Dive two or three times. The fake is the play.",
                "If they did not bite, the Snapper is sitting open in front of you.",
            ]),

        // -------------------------------------------------------------- EMPTY
        new(
            Num: 7, Name: "FLY SWEEP", Kid: "ZOOM",
            Formation: "EMPTY", Category: "RUN ZONE", Complexity: PlayComplexity.Moderate,
            Tagline: "The Slot runs across at full speed, takes the ball, and beats everybody to the edge.",
            Mistake: "The Slot slowing down for the handoff. Full speed past the thrower, or the play is dead.",
            Ball: ("carry", "H"),
            Paths:
            [
                new("H", Motion, [new(5, 0), new(2.2, -1.6), new(-0.4, -1.8)]),
                new("QB", Handoff, [new(0, -3), new(-1.1, -2.0)], To: "H"),
                new("H", Run, [new(-0.4, -1.8), new(-3.4, -1.6), new(-7, -1), new(-9.5, 2), new(-10.5, 8)]),
                new("X", Route, [new(-10, 0), new(-10, 6), new(-5.5, 11)]),
                new("Z", Route, [new(10, 0), new(10, 10)]),
                new("C", Route, [new(0, 0), new(1.5, 6)]),
            ],
            Calls:
            [
                new("SLOT RIGHT", "MOTION → CARRY left"),
                new("THROWER", "hold it out at belly height"),
                new("WIDE LEFT", "POST — take the corner inside"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "MOTION, then CARRY. Start on the coach's signal, be at FULL SPEED when you pass the thrower, take the ball and run for the left sideline."),
                new("THROWER", "Catch the snap and hold the ball out at belly height. He takes it — do not push it at him."),
                new("WIDE LEFT", "POST. Run up six, then break to the middle. Your defender goes with you, and the sideline is empty."),
                new("WIDE RIGHT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "Practise the motion twenty times before you practise the handoff. Timing is the whole play.",
                "When the defense starts chasing the motion, call Fly Keep — and after that, Fly Pass.",
            ]),
        new(
            Num: 8, Name: "FLY KEEP", Kid: "SWITCHEROO",
            Formation: "EMPTY", Category: "RUN ZONE", Complexity: PlayComplexity.Advanced,
            Tagline: "Fake Fly Sweep one way, and the THROWER keeps it the other.",
            Mistake: "The thrower going too early. Let the Slot clear the fake, count two, then run.",
            Ball: ("carry", "QB"),
            Paths:
            [
                new("H", Motion, [new(5, 0), new(2.4, -2.0)]),
                new("H", Fake, [new(2.4, -2.0), new(-2, -2.2), new(-6.5, -2.0)]),
                new("QB", Run, [new(0, -3), new(2, -3.9), new(5.5, -3.4), new(7.8, 0.5), new(8.2, 7)]),
                new("X", Route, [new(-10, 0), new(-10, 8)]),
                new("Z", Route, [new(10, 0), new(10, 9)]),
                new("C", Route, [new(0, 0), new(-1, 6)]),
            ],
            Calls:
            [
                new("SLOT RIGHT", "MOTION left → FAKE"),
                new("THROWER", "fake, count two, CARRY right"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "MOTION left and run the FAKE all the way to the sideline. Arms up like you have the ball — sell it."),
                new("THROWER", "Fake the handoff, count two, then step back to the right and CARRY it up the right side."),
                new("WIDE RIGHT", "GO. Sprint downfield and clear the right side for the thrower."),
                new("WIDE LEFT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "Only once the defense has seen Fly Sweep twice. A defense that has not been burned does not chase.",
                "Rule check: some leagues do not let the thrower run. If yours does not, this play is off the card.",
            ]),
        new(
            Num: 9, Name: "FLY PASS", Kid: "SURPRISE PARTY",
            Formation: "EMPTY", Category: "SHOT PLAY", Complexity: PlayComplexity.Advanced,
            Tagline: "Fly Sweep for two steps, then the Slot stops behind the line and throws it deep to the Wide Left.",
            Mistake: "The Slot throwing on the run or past the line. Stop, set your feet behind the line, then throw.",
            Ball: ("pass", "X"),
            Thrower: "H",
            Paths:
            [
                new("H", Motion, [new(5, 0), new(2.2, -1.6), new(-0.4, -1.8)]),
                new("QB", Handoff, [new(0, -3), new(-1.1, -2.0)], To: "H"),
                new("H", Run, [new(-0.4, -1.8), new(-3.4, -1.6), new(-5.5, -1.9)], Bar),
                new("X", Route, [new(-10, 0), new(-11, 12)]),
                new("Z", Route, [new(10, 0), new(10, 10)]),
                new("C", Route, [new(0, 0), new(1, 5)]),
            ],
            Calls:
            [
                new("SLOT RIGHT", "MOTION → CARRY left, stop, throw it deep"),
                new("WIDE LEFT", "GO up the sideline — look back"),
                new("THROWER", "hold it out at belly height"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "MOTION, CARRY, then THROW. The same start as Fly Sweep. Two steps with the ball, then STOP before the line, set your feet, and throw it deep to the Wide Left. Covered? Tuck it and run."),
                new("WIDE LEFT", "GO. Sprint straight up the left sideline and look back over your inside shoulder. Your defender will come up for the sweep — run past him."),
                new("THROWER", "The same as Fly Sweep: hold it out and let him take it. Then get out of the way."),
                new("WIDE RIGHT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "Fly Sweep first, at least twice. This is the punishment for a defender who comes up to chase it.",
                "Legal check: the throw leaves from behind the line, and there is one forward pass per play. A few leagues let only the thrower pass — check yours.",
            ]),
        new(
            Num: 10, Name: "FOUR SPOTS", Kid: "CHECKERS",
            Formation: "EMPTY", Category: "NO-RUN ZONE", Complexity: PlayComplexity.Simple,
            Tagline: "Everybody runs to a spot and turns around. Find the one with nobody near him.",
            Mistake: "Receivers drifting toward the ball once they stop. Sit on your spot and let it come to you.",
            Ball: ("pass", "H"),
            Paths:
            [
                new("X", Route, [new(-10, 0), new(-10, 5)], Bar),
                new("H", Route, [new(5, 0), new(5, 5)], Bar),
                new("Z", Route, [new(10, 0), new(10, 3), new(13.5, 3)]),
                new("C", Route, [new(0, 0), new(-1, 4)], Bar),
            ],
            Calls:
            [
                new("SLOT RIGHT", "SIT at 5 — first look"),
                new("WIDE LEFT", "SIT at 5"),
                new("WIDE RIGHT", "OUT at 3"),
                new("SNAPPER", "SIT at 4"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "SIT. Five steps, stop, and turn around facing the thrower."),
                new("WIDE LEFT", "SIT. Five steps, stop, turn around."),
                new("WIDE RIGHT", "OUT at 3. Three steps, plant, and break toward the right sideline."),
                new("SNAPPER", "Snap it, run four steps and SIT."),
                new("THROWER", "Look right first. Throw to whoever has no defender standing next to him."),
            ],
            Notes:
            [
                "Your no-run-zone call: four receivers spread across the field and three defenders to cover them.",
                "Count it out at practice: the defense cannot cover four spots with three kids.",
            ]),
        new(
            Num: 11, Name: "THREE VERTS", Kid: "SKYSCRAPER",
            Formation: "EMPTY", Category: "SHOT PLAY", Complexity: PlayComplexity.Simple,
            Tagline: "Three receivers straight downfield. One of them is open deep.",
            Mistake: "Receivers looking back too early and slowing down. Run first, look later.",
            Ball: ("pass", "H"),
            Paths:
            [
                new("X", Route, [new(-10, 0), new(-10, 12)]),
                new("H", Route, [new(5, 0), new(5, 12)]),
                new("Z", Route, [new(10, 0), new(11, 12)]),
                new("C", Route, [new(0, 0), new(-1, 5)], Bar),
            ],
            Calls:
            [
                new("SLOT RIGHT", "GO — first look"),
                new("BOTH WIDES", "GO"),
                new("SNAPPER", "SIT at 5"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "GO. Sprint straight downfield. Look back after ten steps."),
                new("WIDE LEFT / WIDE RIGHT", "GO. Sprint straight downfield as fast as you can."),
                new("SNAPPER", "Snap it, run five steps and SIT. You are the safe throw."),
                new("THROWER", "Look at the Slot. If the defender is beside him and not behind him, throw it deep."),
            ],
            Notes:
            [
                "Three deep runners against three defenders means one defender has to choose. Throw where he did not go.",
                "The Snapper sitting at five is the answer when everybody is covered.",
            ]),
        new(
            Num: 12, Name: "QUICK OUT", Kid: "POPCORN",
            Formation: "EMPTY", Category: "QUICK GAME", Complexity: PlayComplexity.Moderate,
            Tagline: "The Wide Right breaks out at four, the Slot sits under him. Your third-down call.",
            Mistake: "The thrower waiting for the out to come open. Throw it as he breaks, to where he is going.",
            Ball: ("pass", "Z"),
            Paths:
            [
                new("Z", Route, [new(10, 0), new(10, 4), new(14, 4)]),
                new("H", Route, [new(5, 0), new(5, 5)], Bar),
                new("X", Route, [new(-10, 0), new(-10, 5), new(-5.5, 5)]),
                new("C", Route, [new(0, 0), new(0, 9)]),
            ],
            Calls:
            [
                new("WIDE RIGHT", "OUT at 4 — first look"),
                new("SLOT RIGHT", "SIT at 5"),
                new("WIDE LEFT", "IN at 5"),
                new("SNAPPER", "GO"),
            ],
            Assign:
            [
                new("WIDE RIGHT", "OUT at 4. Four steps, plant, and break toward the right sideline. Catch it and get out of bounds."),
                new("SLOT RIGHT", "SIT. Five steps, stop, turn around facing the thrower."),
                new("WIDE LEFT", "IN at 5. Five steps, plant, break toward the middle."),
                new("SNAPPER", "Snap it and GO straight downfield. You clear the middle for the in."),
                new("THROWER", "Out first. If his defender is outside him, the Slot is sitting open."),
            ],
            Notes:
            [
                "Third and short: the out is a sure four if it is thrown on time.",
                "The Snapper's GO is what keeps the middle clear for the Wide Left's in.",
            ]),

        // --------------------------------------------------------- TRIO RIGHT
        new(
            Num: 13, Name: "STAIRS", Kid: "LADDER",
            Formation: "TRIO RIGHT", Category: "GOAL LINE", Complexity: PlayComplexity.Advanced,
            Tagline: "The THROWER rolls right behind three outs at three depths. Throw the highest one open.",
            Mistake: "Two receivers breaking at the same depth. One, three, five — drill it with cones.",
            Ball: ("pass", "Z"),
            Paths:
            [
                new("Z", Route, [new(11, 0), new(12.4, 2.5), new(12.4, 5), new(14.4, 5)]),
                new("Y", Route, [new(7.5, 0), new(7.5, 3), new(11, 3)]),
                new("H", Route, [new(4, 0), new(4, 1.8), new(6.6, 1.8)]),
                new("QB", Run, [new(0, -3), new(3.5, -4), new(6.5, -3.5)]),
                new("C", Route, [new(0, 0), new(-1, 3)], Bar),
            ],
            Calls:
            [
                new("WIDE RIGHT", "OUT at 5"),
                new("SLOT RIGHT", "OUT at 3"),
                new("TIGHT RIGHT", "OUT at 1"),
                new("THROWER", "roll right, throw the highest one open"),
                new("SNAPPER", "SIT at 3"),
            ],
            Assign:
            [
                new("WIDE RIGHT", "OUT at 5. Get wide first, run to five, then break to the sideline. You are the top step."),
                new("SLOT RIGHT", "OUT at 3. Three steps, then break out. Never drift down to the Tight Right's depth."),
                new("TIGHT RIGHT", "OUT at 1. One step and break out — the shortest, quickest answer."),
                new("SNAPPER", "Snap it, run three steps to the left and SIT."),
                new("THROWER", "Roll to the right behind the line. Three receivers are breaking out in front of you. Throw the highest one open, and if nobody is, run for the sideline."),
            ],
            Notes:
            [
                "The extra-point call. Three outs at three depths against two defenders on that side — somebody is open.",
                "Legal check: the thrower has to throw from behind the line. Teach him to stop rolling before he gets to it.",
            ]),
        new(
            Num: 14, Name: "LEVELS", Kid: "LAYER CAKE",
            Formation: "TRIO RIGHT", Category: "SHOT PLAY", Complexity: PlayComplexity.Moderate,
            Tagline: "Deep, middle and short on the same side. The defender in the middle cannot be in two places.",
            Mistake: "The Slot rounding his out up into the Wide Right's lane. Six, plant, square out.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("Z", Route, [new(11, 0), new(11, 13)]),
                new("Y", Route, [new(7.5, 0), new(7.5, 6), new(11.5, 6)]),
                new("H", Route, [new(4, 0), new(4, 2), new(8, 2)]),
                new("C", Route, [new(0, 0), new(0, 6), new(-4.5, 11)]),
            ],
            Calls:
            [
                new("SLOT RIGHT", "OUT at 6 — first look"),
                new("WIDE RIGHT", "GO"),
                new("TIGHT RIGHT", "OUT at 2"),
                new("SNAPPER", "POST the other way"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "OUT at 6. Six steps, plant, break to the right sideline."),
                new("WIDE RIGHT", "GO. Sprint straight down the sideline and take the deep defender with you."),
                new("TIGHT RIGHT", "OUT at 2. Two steps and break to the flat."),
                new("SNAPPER", "POST. Snap it, run up six, then break toward the left."),
                new("THROWER", "Watch the defender in the middle of the right side. If he drops, throw the short out. If he comes up, throw the Slot."),
            ],
            Notes:
            [
                "Three levels on one side against two defenders. The read is one kid: whoever is between the Slot and the Tight Right.",
                "If the deep defender cheats up, the Wide Right is behind everybody.",
            ]),
        new(
            Num: 15, Name: "SIT FLAT", Kid: "LEMONADE",
            Formation: "TRIO RIGHT", Category: "QUICK GAME", Complexity: PlayComplexity.Simple,
            Tagline: "The Slot sits at five, the Tight Right runs to the flat under him. Throw the one left open.",
            Mistake: "The Tight Right running his out too deep. One step, then flat, so he is under the sit.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("Y", Route, [new(7.5, 0), new(7.5, 5)], Bar),
                new("H", Route, [new(4, 0), new(4, 1), new(8.5, 1)]),
                new("Z", Route, [new(11, 0), new(11, 10)]),
                new("C", Route, [new(0, 0), new(-1.5, 4)], Bar),
            ],
            Calls:
            [
                new("SLOT RIGHT", "SIT at 5 — first look"),
                new("TIGHT RIGHT", "OUT at 1"),
                new("WIDE RIGHT", "GO"),
                new("SNAPPER", "SIT at 4"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "SIT. Five steps, stop, turn around facing the thrower."),
                new("TIGHT RIGHT", "OUT at 1. One step, then break flat toward the right sideline."),
                new("WIDE RIGHT", "GO. Sprint straight downfield and take your defender away."),
                new("SNAPPER", "Snap it, run four steps to the left and SIT."),
                new("THROWER", "Look at the Slot. If a defender is sitting on him, the flat is open."),
            ],
            Notes:
            [
                "A short, sure throw for a thrower who is still learning. Two receivers, one defender between them.",
                "The Snapper sitting on the empty side is the third answer.",
            ]),
        new(
            Num: 16, Name: "THROWER SNEAK", Kid: "BULLDOG",
            Formation: "TRIO RIGHT", Category: "GOAL LINE", Complexity: PlayComplexity.Simple,
            Tagline: "Everybody runs off to the right and the THROWER keeps it up the empty left side.",
            Mistake: "The receivers jogging. If they do not run hard, their defenders stay home and close the gap.",
            Ball: ("carry", "QB"),
            Paths:
            [
                new("QB", Run, [new(0, -3), new(-1.2, -1.6), new(-2, 5)]),
                new("C", Route, [new(0, 0), new(2, 5)]),
                new("H", Route, [new(4, 0), new(4.5, 8)]),
                new("Y", Route, [new(7.5, 0), new(8, 9)]),
                new("Z", Route, [new(11, 0), new(11.5, 10)]),
            ],
            Calls:
            [
                new("THROWER", "CARRY up the left side"),
                new("SNAPPER", "GO to the right"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("THROWER", "CARRY. Catch the snap and run straight up the left side, just past the snapper's shoulder."),
                new("SNAPPER", "Snap it and GO up and to the right. Take your defender away from the thrower."),
                new("TIGHT RIGHT / SLOT RIGHT / WIDE RIGHT", "GO. Sprint downfield as hard as you can. You are clearing the field for him."),
            ],
            Notes:
            [
                "Short yardage when the defense has come up to stop the outs. Run Stairs first.",
                "Rule check: some leagues do not let the thrower run, and some have a no-run zone at the goal line. Check both.",
            ]),
        new(
            Num: 17, Name: "POST SIT", Kid: "PAPER PLANE",
            Formation: "TRIO RIGHT", Category: "NO-RUN ZONE", Complexity: PlayComplexity.Moderate,
            Tagline: "The Wide Right sits, the Slot runs a post behind him. High-low on one defender.",
            Mistake: "The Slot breaking before six steps. Too early and he runs into the sit.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("Z", Route, [new(11, 0), new(11, 5)], Bar),
                new("Y", Route, [new(7.5, 0), new(7.5, 6), new(3, 11)]),
                new("H", Route, [new(4, 0), new(4, 3), new(8, 3)]),
                new("C", Route, [new(0, 0), new(-2, 9)]),
            ],
            Calls:
            [
                new("SLOT RIGHT", "POST — first look"),
                new("WIDE RIGHT", "SIT at 5"),
                new("TIGHT RIGHT", "OUT at 3"),
                new("SNAPPER", "GO"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "POST. Run up six, then break toward the middle. Look for it over your inside shoulder."),
                new("WIDE RIGHT", "SIT. Five steps, stop, turn around."),
                new("TIGHT RIGHT", "OUT at 3. Three steps and break toward the sideline."),
                new("SNAPPER", "Snap it and GO up the left side."),
                new("THROWER", "Post first. If the defender stays deep with him, throw the sit."),
            ],
            Notes:
            [
                "No running in the zone, so this is three throws on one side and the Snapper clearing the other.",
                "The high-low is one defender's problem. Teach the thrower to watch that one kid.",
            ]),
        new(
            Num: 18, Name: "TRIO FLAT", Kid: "PANCAKE",
            Formation: "TRIO RIGHT", Category: "QUICK GAME", Complexity: PlayComplexity.Simple,
            Tagline: "The Tight Right breaks straight to the flat behind two receivers running their defenders off.",
            Mistake: "The thrower looking downfield first. This is a one-look play — the flat, right now.",
            Ball: ("pass", "H"),
            Paths:
            [
                new("H", Route, [new(4, 0), new(4, 1), new(9, 1)]),
                new("Y", Route, [new(7.5, 0), new(7.5, 10)]),
                new("Z", Route, [new(11, 0), new(11, 10)]),
                new("C", Route, [new(0, 0), new(0, 5)], Bar),
            ],
            Calls:
            [
                new("TIGHT RIGHT", "OUT at 1 — the throw"),
                new("SLOT RIGHT / WIDE RIGHT", "GO"),
                new("SNAPPER", "SIT at 5"),
            ],
            Assign:
            [
                new("TIGHT RIGHT", "OUT at 1. One step, then break flat to the right sideline. Catch it and turn up."),
                new("SLOT RIGHT / WIDE RIGHT", "GO. Sprint downfield and take your defenders with you."),
                new("SNAPPER", "Snap it, run five steps and SIT."),
                new("THROWER", "Catch the snap and throw it to the Tight Right in the flat. One look."),
            ],
            Notes:
            [
                "A one-read throw for a brand-new thrower: quick, short and toward the sideline.",
                "The two GO routes are the blocking. Coach them to run hard even when they know the ball is not coming.",
            ]),

        // -------------------------------------------------------- POWER RIGHT
        new(
            Num: 19, Name: "POWER PITCH", Kid: "SLED",
            Formation: "POWER RIGHT", Category: "RUN ZONE", Complexity: PlayComplexity.Simple,
            Tagline: "Pitch it to the BACK and beat the rusher to the right edge.",
            Mistake: "Pitching it behind the Back. Toss it where he is going, not where he is.",
            Ball: ("carry", "H"),
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(3.5, -4.2)], To: "H"),
                new("H", Run, [new(-1.5, -5), new(2, -4.5), new(6, -3), new(9.5, 1), new(10.5, 8)]),
                new("Z", Route, [new(9, 0), new(12.6, 3), new(13.4, 8)]),
                new("Y", Route, [new(3, 0), new(5, 9)]),
                new("C", Route, [new(0, 0), new(-1, 6)]),
            ],
            Calls:
            [
                new("BACK", "CARRY — pitch, then the right sideline"),
                new("THROWER", "pitch it in front of him"),
                new("WIDE RIGHT", "GO to the sideline"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "CARRY. Run hard to the right, catch the pitch, and get to the sideline before you turn up."),
                new("THROWER", "Catch the snap, turn right, and toss it softly to where the Back is going."),
                new("WIDE RIGHT", "GO. Run to the sideline and up it. Take the corner away from the Back."),
                new("TIGHT RIGHT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "The rusher starts seven yards back. A pitch to the edge gets there before he does.",
                "When the defense starts flying to the right, call Power Reverse.",
            ]),
        new(
            Num: 20, Name: "POWER REVERSE", Kid: "SPINNER",
            Formation: "POWER RIGHT", Category: "RUN ZONE", Complexity: PlayComplexity.Advanced,
            Tagline: "Power Pitch to the right, and the Wide Right takes it back the other way.",
            Mistake: "The Wide Right leaving before the Back gets to him. Take the ball first, then go.",
            Ball: ("carry", "Z"),
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(3.5, -4.2)], To: "H"),
                new("H", Run, [new(-1.5, -5), new(2, -4.2), new(5, -3.0), new(8.5, -2.0), new(11, 0.5)]),
                new("H", Handoff, [new(5, -3.0), new(4.6, -3.8)], To: "Z"),
                new("Z", Run, [new(9, 0), new(8.2, -2.4), new(4.6, -3.8), new(0, -4.4), new(-5, -3.8), new(-9.5, -0.5), new(-12, 7)]),
                new("Y", Route, [new(3, 0), new(3.5, 9)]),
                new("C", Route, [new(0, 0), new(-0.5, 5)]),
            ],
            Calls:
            [
                new("BACK", "CARRY right, hand it to the Wide Right"),
                new("WIDE RIGHT", "come back, CARRY to the left sideline"),
                new("THROWER", "pitch it, then get out of the way"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "CARRY. Exactly like Power Pitch — catch it and run right. When the Wide Right comes back to you, hand him the ball and keep running."),
                new("WIDE RIGHT", "CARRY. Take two steps up, then come back behind the Back. Take the ball from him and run all the way to the left sideline, then up."),
                new("THROWER", "Pitch it to the Back exactly like Power Pitch, then step back out of the way."),
                new("TIGHT RIGHT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "The punishment for a defense that flies to Power Pitch. Run the pitch twice first.",
                "Practise the exchange walking, then jogging. A dropped reverse is a long way backwards.",
            ]),
        new(
            Num: 21, Name: "POWER DIVE", Kid: "PLOW",
            Formation: "POWER RIGHT", Category: "GOAL LINE", Complexity: PlayComplexity.Moderate,
            Tagline: "The BACK takes it straight up behind the Tight Right. Your short-yardage run.",
            Mistake: "The Back stopping to look for a hole. Hit it, low and fast.",
            Ball: ("carry", "H"),
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(-0.8, -3.6)], To: "H"),
                new("H", Run, [new(-1.5, -5), new(-0.9, -3.8), new(1.3, 0.5), new(1.8, 5)]),
                new("Y", Route, [new(3, 0), new(3, 4), new(7, 8)]),
                new("Z", Route, [new(9, 0), new(9, 8)]),
                new("C", Route, [new(0, 0), new(-2, 4)]),
            ],
            Calls:
            [
                new("BACK", "CARRY — straight up, right of the snapper"),
                new("THROWER", "hand it off, don't watch"),
                new("TIGHT RIGHT", "CORNER"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "CARRY. Take the ball and run straight up, just right of the snapper. Do not stop."),
                new("THROWER", "Catch the snap, turn, and hand it to the Back."),
                new("TIGHT RIGHT", "CORNER. Run up four, then break toward the right corner. Your defender goes with you."),
                new("WIDE RIGHT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "Two yards to go and the defense spread out: run it right at the middle.",
                "Rule check: many leagues have a no-run zone at the goal line. This is for when you are outside it.",
            ]),
        new(
            Num: 22, Name: "CORNER FADE", Kid: "TOUCHDOWN",
            Formation: "POWER RIGHT", Category: "GOAL LINE", Complexity: PlayComplexity.Simple,
            Tagline: "The Wide Right fades to the back corner. One throw, one catch, six points.",
            Mistake: "The Wide Right drifting toward the middle. Stay on the sideline — the corner is yours.",
            Ball: ("pass", "Z"),
            Paths:
            [
                new("Z", Route, [new(9, 0), new(12.5, 12)]),
                new("Y", Route, [new(3, 0), new(3, 3)], Bar),
                new("H", Route, [new(-1.5, -5), new(-4.5, -3.5), new(-7.5, -3)]),
                new("C", Route, [new(0, 0), new(-1, 2)], Bar),
            ],
            Calls:
            [
                new("WIDE RIGHT", "GO to the back corner — the throw"),
                new("TIGHT RIGHT", "SIT at 3"),
                new("BACK", "SWING to the left flat"),
                new("SNAPPER", "SIT at 2"),
            ],
            Assign:
            [
                new("WIDE RIGHT", "GO. Run toward the back right corner of the end zone and look back over your outside shoulder."),
                new("TIGHT RIGHT", "SIT. Three steps, stop, turn around."),
                new("BACK", "SWING. Loop out to the left flat and look back."),
                new("SNAPPER", "Snap it, two steps and SIT."),
                new("THROWER", "Throw it high to the back corner, where only the Wide Right can get it."),
            ],
            Notes:
            [
                "Your extra-point call. A high ball to the corner is caught by your kid or nobody.",
                "If the defender plays the corner, the Tight Right is sitting open right in front of you.",
            ]),
        new(
            Num: 23, Name: "TIGHT FLAT", Kid: "PEANUT BUTTER",
            Formation: "POWER RIGHT", Category: "NO-RUN ZONE", Complexity: PlayComplexity.Simple,
            Tagline: "The Tight Right breaks to the flat while the Wide Right runs his defender off.",
            Mistake: "The Wide Right stopping early. If he does not run, his defender is standing in the flat.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("Y", Route, [new(3, 0), new(3, 2), new(7, 2)]),
                new("Z", Route, [new(9, 0), new(10, 10)]),
                new("H", Route, [new(-1.5, -5), new(-4.5, -3.5), new(-7.5, -3)]),
                new("C", Route, [new(0, 0), new(0, 4)], Bar),
            ],
            Calls:
            [
                new("TIGHT RIGHT", "OUT at 2 — the throw"),
                new("WIDE RIGHT", "GO"),
                new("BACK", "SWING to the left flat"),
                new("SNAPPER", "SIT at 4"),
            ],
            Assign:
            [
                new("TIGHT RIGHT", "OUT at 2. Two steps, then break toward the right sideline. Catch it and turn up."),
                new("WIDE RIGHT", "GO. Sprint straight downfield and take your defender away."),
                new("BACK", "SWING. Loop out to the left flat — the second throw."),
                new("SNAPPER", "Snap it, four steps and SIT."),
                new("THROWER", "Tight Right first. If he is covered, turn and find the Back."),
            ],
            Notes:
            [
                "No running in the zone, so give the defense a run look and throw short out of it.",
                "Two flats, one each side, and the Snapper in the middle. Three easy throws.",
            ]),
        new(
            Num: 24, Name: "POWER FAKE POST", Kid: "MAGIC CARPET",
            Formation: "POWER RIGHT", Category: "SHOT PLAY", Complexity: PlayComplexity.Moderate,
            Tagline: "Fake Power Dive, and the Tight Right runs a post behind the defense that came up.",
            Mistake: "The Back stopping his fake at the line. Run it like you have the ball.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("H", Fake, [new(-1.5, -5), new(-0.9, -3.8), new(1.3, 0.5)]),
                new("Y", Route, [new(3, 0), new(3, 6), new(-1.5, 11)]),
                new("Z", Route, [new(9, 0), new(9, 6), new(13.5, 11)]),
                new("C", Route, [new(0, 0), new(-3, 8)]),
            ],
            Calls:
            [
                new("BACK", "FAKE the dive — sell it"),
                new("TIGHT RIGHT", "POST — the shot"),
                new("WIDE RIGHT", "CORNER"),
                new("SNAPPER", "GO up the left side"),
            ],
            Assign:
            [
                new("BACK", "FAKE. Run exactly like Power Dive, arms folded like you have it, all the way to the line."),
                new("TIGHT RIGHT", "POST. Run up six, then break toward the middle. The defender will come up for the fake — run past him."),
                new("WIDE RIGHT", "CORNER. Run up six, then break toward the right corner."),
                new("SNAPPER", "Snap it and GO up the left side."),
                new("THROWER", "Fake to the Back, then throw the post if the middle defender came up."),
            ],
            Notes:
            [
                "Run Power Dive twice first. The fake works on a defense that has been hit by the real thing.",
                "If nobody bit, the Wide Right's corner is the second look.",
            ]),
    ];

    private static Format? _format;

    /// <summary>The 5-on-5 library, as the checker, the exporter and the paper read it.</summary>
    public static Format Format => _format ??= Authoring.Build(
        "5", 5, "f_", "5 on 5", Formations, SpotMap, Notes, Library,
        Authoring.Weeks(
            "Game one. One run, one quick pass, one third down, one goal line. Master these four first.",
            1, 2, 12, 22));
}
