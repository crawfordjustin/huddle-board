using static HuddleBoard.Playbook.EndStyle;
using static HuddleBoard.Playbook.PathType;

namespace HuddleBoard.Playbook;

/// <summary>
/// 7-on-7: the SNAPPER, the THROWER and five more. Its own four formations
/// and its own library. With seven kids on the same field the space is the
/// scarce thing, so the concepts here are built around getting five receivers
/// out without two of them meeting — the checker's time-aware collision rule
/// has more pairs to hold here than anywhere else, and it is not loosened.
/// </summary>
/// <remarks>
/// Five non-ball spots need five keys, so this format adds <c>F</c> to
/// <c>X/Y/Z/H</c>. Keys are never shown; a kid still hears his spot's name.
/// Where an exchange is the same as one in the 6-on-6 library — the dive, the
/// jet, the pitch, the pitch reverse — the geometry is the same too.
/// </remarks>
public static class SevenOnSeven
{
    public static readonly OrderedDictionary<string, OrderedDictionary<string, Pt>> Formations = new()
    {
        ["PRO"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["H"] = new(-2.6, -4.4),
            ["X"] = new(-11, 0),
            ["F"] = new(-3.5, 0),
            ["Y"] = new(6, 0),
            ["Z"] = new(11.5, 0),
        },
        ["FLOOD RIGHT"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["H"] = new(-2.6, -4.4),
            ["X"] = new(-11, 0),
            ["F"] = new(4, 0),
            ["Y"] = new(7.5, 0),
            ["Z"] = new(11, 0),
        },
        ["FIVE WIDE"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["X"] = new(-12, 0),
            ["F"] = new(-7.5, 0),
            ["H"] = new(3.5, 0),
            ["Y"] = new(7.5, 0),
            ["Z"] = new(12, 0),
        },
        ["HEAVY"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["H"] = new(-1.5, -5),
            ["X"] = new(-9, 0),
            ["F"] = new(-3, 0),
            ["Y"] = new(3, 0),
            ["Z"] = new(9, 0),
        },
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, SpotLabel>> SpotMap =
        new Dictionary<string, IReadOnlyDictionary<string, SpotLabel>>
        {
            ["PRO"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["H"] = new("B", "BACK"),
                ["X"] = new("WL", "WIDE LEFT"),
                ["F"] = new("TL", "TIGHT LEFT"),
                ["Y"] = new("SR", "SLOT RIGHT"),
                ["Z"] = new("WR", "WIDE RIGHT"),
            },
            ["FLOOD RIGHT"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["H"] = new("B", "BACK"),
                ["X"] = new("WL", "WIDE LEFT"),
                ["F"] = new("TR", "TIGHT RIGHT"),
                ["Y"] = new("SR", "SLOT RIGHT"),
                ["Z"] = new("WR", "WIDE RIGHT"),
            },
            ["FIVE WIDE"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["X"] = new("WL", "WIDE LEFT"),
                ["F"] = new("SL", "SLOT LEFT"),
                ["H"] = new("TR", "TIGHT RIGHT"),
                ["Y"] = new("SR", "SLOT RIGHT"),
                ["Z"] = new("WR", "WIDE RIGHT"),
            },
            ["HEAVY"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["H"] = new("B", "BACK"),
                ["X"] = new("WL", "WIDE LEFT"),
                ["F"] = new("TL", "TIGHT LEFT"),
                ["Y"] = new("TR", "TIGHT RIGHT"),
                ["Z"] = new("WR", "WIDE RIGHT"),
            },
        };

    public static readonly IReadOnlyDictionary<string, string> Notes = new Dictionary<string, string>
    {
        ["PRO"] =
            "Your base look. Two each side and a BACK, so the dive, the toss and the play-action all start from the same picture.",
        ["FLOOD RIGHT"] =
            "Three receivers to the right at three widths, a BACK, and one wide alone on the left. Built for flooding one side and rolling the THROWER into it.",
        ["FIVE WIDE"] =
            "Nobody in the backfield. Five receivers stretch the whole width; the SLOT LEFT is your jet man.",
        ["HEAVY"] =
            "Both TIGHTs in close and a BACK deep. Your run look, your goal line look, and where the pitch and the reverse come from.",
    };

    private static readonly IReadOnlyList<Authored> Library =
    [
        // ---------------------------------------------------------------- PRO
        new(
            Num: 1, Name: "71 DIVE", Kid: "BIG WHEEL",
            Formation: "PRO", Category: "RUN ZONE", Complexity: PlayComplexity.Simple,
            Tagline: "Hand it to the BACK and go straight ahead behind the Slot Right. Your first-down play.",
            Mistake: "The Back bouncing outside. Five receivers running off leaves the middle open — hit it.",
            Ball: ("carry", "H"),
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(-1.4, -3.7)], To: "H"),
                new("H", Run, [new(-2.6, -4.4), new(-1.4, -3.7), new(2.2, 0.5), new(3, 8)]),
                new("X", Route, [new(-11, 0), new(-11, 10)]),
                new("F", Route, [new(-3.5, 0), new(-4.5, 9)]),
                new("Y", Route, [new(6, 0), new(6.5, 9)]),
                new("Z", Route, [new(11.5, 0), new(11.5, 10)]),
                new("C", Route, [new(0, 0), new(-1, 4)], Bar),
            ],
            Calls:
            [
                new("BACK", "CARRY — straight ahead"),
                new("THROWER", "hand it off, don't watch"),
                new("SNAPPER", "SIT at 4"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "CARRY. One step toward the thrower, take it with both hands, and run straight past the snapper's right hip."),
                new("THROWER", "Catch the snap, turn, and put the ball in the Back's belly. Do not watch him run."),
                new("SNAPPER", "Snap it, run four steps to the left and SIT, out of the Back's lane."),
                new("WIDE LEFT / TIGHT LEFT / SLOT RIGHT / WIDE RIGHT", "GO. Sprint downfield and take your defender as far from the ball as you can."),
            ],
            Notes:
            [
                "Seven on the field means seven defenders. The four GO routes are what thin the middle out.",
                "A brand-new kid can play Back on this one: one job, one direction.",
            ]),
        new(
            Num: 2, Name: "PRO SMASH", Kid: "HIGH FIVE",
            Formation: "PRO", Category: "QUICK GAME", Complexity: PlayComplexity.Moderate,
            Tagline: "The Wide Right sits at five, the Slot Right bends to the corner over him.",
            Mistake: "The Slot rounding his corner off. Six steps straight, then break.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("Y", Route, [new(6, 0), new(6, 6), new(10.5, 11)]),
                new("Z", Route, [new(11.5, 0), new(11.5, 5)], Bar),
                new("F", Route, [new(-3.5, 0), new(-3.5, 5)], Bar),
                new("X", Route, [new(-11, 0), new(-11, 10)]),
                new("H", Route, [new(-2.6, -4.4), new(-5.6, -2.9), new(-8.6, -2.4)]),
                new("C", Route, [new(0, 0), new(0.5, 9)]),
            ],
            Calls:
            [
                new("SLOT RIGHT", "CORNER — first look"),
                new("WIDE RIGHT", "SIT at 5"),
                new("TIGHT LEFT", "SIT at 5"),
                new("BACK", "SWING to the left flat"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "CORNER. Run six steps straight up, then break toward the right corner. Look back over your outside shoulder."),
                new("WIDE RIGHT", "SIT. Five steps, stop, and turn around facing the thrower."),
                new("TIGHT LEFT", "SIT. Five steps, stop, turn around. You are the safe throw on the other side."),
                new("BACK", "SWING. Loop out to the left flat and look back."),
                new("WIDE LEFT / SNAPPER", "GO. Sprint straight downfield."),
                new("THROWER", "Watch the defender on the right. If he jumps the sit, throw the corner. If he stays deep, throw the sit."),
            ],
            Notes:
            [
                "The read is one defender. Teach the thrower to look at him, not at the receivers.",
                "If the right side is crowded, the Tight Left is sitting alone.",
            ]),
        new(
            Num: 3, Name: "SHORT SIDE", Kid: "SNOWCONE",
            Formation: "PRO", Category: "QUICK GAME", Complexity: PlayComplexity.Simple,
            Tagline: "The Tight Left breaks to the left flat with the Back swinging under him. Quick and short.",
            Mistake: "The Tight Left running his out too deep. Two steps, then flat.",
            Ball: ("pass", "F"),
            Paths:
            [
                new("F", Route, [new(-3.5, 0), new(-3.5, 2), new(-7.5, 2)]),
                new("H", Route, [new(-2.6, -4.4), new(-5.6, -2.9), new(-8.6, -2.4)]),
                new("X", Route, [new(-11, 0), new(-11, 10)]),
                new("Y", Route, [new(6, 0), new(6, 5), new(1.5, 5)]),
                new("Z", Route, [new(11.5, 0), new(11.5, 10)]),
                new("C", Route, [new(0, 0), new(-1, 9)]),
            ],
            Calls:
            [
                new("TIGHT LEFT", "OUT at 2 — first look"),
                new("BACK", "SWING to the left flat"),
                new("SLOT RIGHT", "IN at 5"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("TIGHT LEFT", "OUT at 2. Two steps, then break flat toward the left sideline. Catch it and turn up."),
                new("BACK", "SWING. Loop out to the left flat, under the Tight Left."),
                new("SLOT RIGHT", "IN at 5. Five steps, plant, break toward the middle."),
                new("WIDE LEFT", "GO. Sprint downfield and take the corner away from the flat."),
                new("WIDE RIGHT / SNAPPER", "GO. Sprint downfield."),
                new("THROWER", "Tight Left first. If a defender is on him, the Back is underneath."),
            ],
            Notes:
            [
                "A one-side, two-throw play for a thrower who is still learning to read.",
                "The Wide Left's GO is the block. If he stops, his defender is standing in the flat.",
            ]),
        new(
            Num: 4, Name: "71 FAKE POST", Kid: "ABRACADABRA",
            Formation: "PRO", Category: "SHOT PLAY", Complexity: PlayComplexity.Moderate,
            Tagline: "Fake 71 Dive, then throw the Wide Right's post behind the defense that bit.",
            Mistake: "A lazy fake. If the Back does not sell it, nobody comes up and the post is covered.",
            Ball: ("pass", "Z"),
            Paths:
            [
                new("H", Fake, [new(-2.6, -4.4), new(-1.4, -3.7), new(1.5, -1)]),
                new("Z", Route, [new(11.5, 0), new(11.5, 6), new(7, 11)]),
                new("Y", Route, [new(6, 0), new(6, 5)], Bar),
                new("F", Route, [new(-3.5, 0), new(-4, 10)]),
                new("X", Route, [new(-11, 0), new(-11, 6), new(-15, 10.5)]),
                new("C", Route, [new(0, 0), new(-1, 5)], Bar),
            ],
            Calls:
            [
                new("BACK", "FAKE the dive — sell it"),
                new("WIDE RIGHT", "POST — the shot"),
                new("SLOT RIGHT", "SIT at 5"),
                new("WIDE LEFT", "CORNER"),
                new("TIGHT LEFT", "GO"),
                new("SNAPPER", "SIT at 5"),
            ],
            Assign:
            [
                new("BACK", "FAKE. Run exactly like 71 Dive, arms folded like you have the ball, all the way to the line."),
                new("WIDE RIGHT", "POST. Run up six, then break toward the middle. Look for the ball over your inside shoulder."),
                new("SLOT RIGHT", "SIT. Five steps, stop, turn around."),
                new("WIDE LEFT", "CORNER. Run up six, then break toward the left corner."),
                new("TIGHT LEFT", "GO. Sprint straight downfield."),
                new("SNAPPER", "Snap it, run five steps to the left and SIT."),
                new("THROWER", "Fake to the Back, then look at the Wide Right. If a defender came up for the fake, throw the post."),
            ],
            Notes:
            [
                "Only after the defense has seen 71 Dive two or three times. The fake is the play.",
                "If they did not bite, the Slot Right is sitting open in front of you.",
            ]),
        new(
            Num: 5, Name: "71 TOSS", Kid: "ROLLER SKATE",
            Formation: "PRO", Category: "RUN ZONE", Complexity: PlayComplexity.Moderate,
            Tagline: "Hand it to the BACK going left and race to the edge behind the Tight Left.",
            Mistake: "The Back cutting up too soon. Get outside the Tight Left first, then turn up.",
            Ball: ("carry", "H"),
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(-1.6, -3.6)], To: "H"),
                new("H", Run, [new(-2.6, -4.4), new(-2.2, -4.0), new(-5.5, -3.2), new(-8.8, -0.5), new(-6.5, 8)]),
                new("X", Route, [new(-11, 0), new(-11, 10)]),
                new("F", Route, [new(-3.5, 0), new(-4.5, 9)]),
                new("Y", Route, [new(6, 0), new(6, 9)]),
                new("Z", Route, [new(11.5, 0), new(11.5, 10)]),
                new("C", Route, [new(0, 0), new(1, 8)]),
            ],
            Calls:
            [
                new("BACK", "CARRY to the left, then up"),
                new("THROWER", "hand it off, don't watch"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "CARRY. Step toward the thrower, take the ball, and run wide to the left. Turn up once you are outside the Tight Left."),
                new("THROWER", "Catch the snap, step left, and hand it to the Back as he goes by."),
                new("WIDE LEFT / TIGHT LEFT", "GO. Sprint straight downfield and take your defenders with you — the left side is the Back's."),
                new("SLOT RIGHT / WIDE RIGHT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "The pair to 71 Dive: the same handoff, the other direction. Call it once the defense crowds the middle.",
                "Coach the two GO routes on the left hard. They are the blocking.",
            ]),
        new(
            Num: 6, Name: "PRO WHEEL", Kid: "CAROUSEL",
            Formation: "PRO", Category: "SHOT PLAY", Complexity: PlayComplexity.Advanced,
            Tagline: "The BACK wheels up the left side while the Wide Left takes his defender to the corner.",
            Mistake: "The Back turning up before he is wide. Out first, then up.",
            Ball: ("pass", "H"),
            Paths:
            [
                new("H", Route, [new(-2.6, -4.4), new(-5.5, -3.5), new(-7.5, -1), new(-8, 9)]),
                new("X", Route, [new(-11, 0), new(-11, 6), new(-15, 10)]),
                new("F", Route, [new(-3.5, 0), new(-3.5, 4), new(1, 4)]),
                new("Y", Route, [new(6, 0), new(6, 10)]),
                new("Z", Route, [new(11.5, 0), new(11.5, 10)]),
                new("C", Route, [new(0, 0), new(-0.5, 6)], Bar),
            ],
            Calls:
            [
                new("BACK", "WHEEL up the left side — the shot"),
                new("WIDE LEFT", "CORNER — take the defender outside"),
                new("TIGHT LEFT", "IN at 4"),
                new("SNAPPER", "SIT at 6"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "WHEEL. Run out behind the line toward the left sideline, get wide, then turn straight up it and look back."),
                new("WIDE LEFT", "CORNER. Run up six, then break toward the left corner. Your defender goes with you, and the Back turns up inside him."),
                new("TIGHT LEFT", "IN at 4. Four steps, plant, break to the middle."),
                new("SNAPPER", "Snap it, run six steps and SIT."),
                new("SLOT RIGHT / WIDE RIGHT", "GO. Sprint downfield."),
                new("THROWER", "Watch the Back. When he turns up the sideline and nobody turned with him, throw it out in front of him."),
            ],
            Notes:
            [
                "Run 71 Toss first. The defender who flies out to stop the Back is the one he runs past.",
                "The Tight Left's IN is the safe throw if the wheel is covered.",
            ]),

        // -------------------------------------------------------- FLOOD RIGHT
        new(
            Num: 7, Name: "ROLL STAIRS", Kid: "ESCALATOR",
            Formation: "FLOOD RIGHT", Category: "GOAL LINE", Complexity: PlayComplexity.Advanced,
            Tagline: "The THROWER rolls right behind three outs at three depths. Throw the highest one open.",
            Mistake: "Two receivers breaking at the same depth. One, three, five — drill it with cones.",
            Ball: ("pass", "Z"),
            Paths:
            [
                new("Z", Route, [new(11, 0), new(12.4, 2.5), new(12.4, 5), new(14.4, 5)]),
                new("Y", Route, [new(7.5, 0), new(7.5, 3), new(11, 3)]),
                new("F", Route, [new(4, 0), new(4, 1.8), new(6.6, 1.8)]),
                new("QB", Run, [new(0, -3), new(3.5, -4), new(6.5, -3.5)]),
                new("H", Route, [new(-2.6, -4.4), new(-5.6, -2.9), new(-8.6, -2.4)]),
                new("X", Route, [new(-11, 0), new(-11, 10)]),
                new("C", Route, [new(0, 0), new(-1, 3)], Bar),
            ],
            Calls:
            [
                new("WIDE RIGHT", "OUT at 5"),
                new("SLOT RIGHT", "OUT at 3"),
                new("TIGHT RIGHT", "OUT at 1"),
                new("THROWER", "roll right, throw the highest one open"),
                new("BACK", "SWING to the left flat"),
                new("WIDE LEFT", "GO"),
                new("SNAPPER", "SIT at 3"),
            ],
            Assign:
            [
                new("WIDE RIGHT", "OUT at 5. Get wide first, run to five, then break to the sideline. You are the top step."),
                new("SLOT RIGHT", "OUT at 3. Three steps, then break out. Never drift down to the Tight Right's depth."),
                new("TIGHT RIGHT", "OUT at 1. One step and break out — the shortest, quickest answer."),
                new("BACK", "SWING. Loop out to the left flat — the throwback if everybody chases the roll."),
                new("WIDE LEFT", "GO. Sprint downfield."),
                new("SNAPPER", "Snap it, run three steps to the left and SIT."),
                new("THROWER", "Roll right behind the line. Throw the highest out that is open, and if nobody is, run for the sideline."),
            ],
            Notes:
            [
                "The extra-point call. Three outs at three depths on one side — somebody is open.",
                "Legal check: the thrower throws from behind the line. Teach him to stop rolling before he gets to it.",
            ]),
        new(
            Num: 8, Name: "SIDELINE FLOOD", Kid: "TIDAL WAVE",
            Formation: "FLOOD RIGHT", Category: "SHOT PLAY", Complexity: PlayComplexity.Moderate,
            Tagline: "Deep, middle and short on the right sideline. The defender in the middle cannot be in two places.",
            Mistake: "The Slot rounding his out up into the Wide Right's lane. Six, plant, square out.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("Z", Route, [new(11, 0), new(11, 13)]),
                new("Y", Route, [new(7.5, 0), new(7.5, 6), new(11.5, 6)]),
                new("F", Route, [new(4, 0), new(4, 2), new(8, 2)]),
                new("H", Route, [new(-2.6, -4.4), new(-5.6, -2.9), new(-8.6, -2.4)]),
                new("X", Route, [new(-11, 0), new(-11, 6), new(-6.5, 11)]),
                new("C", Route, [new(0, 0), new(-1, 9)]),
            ],
            Calls:
            [
                new("SLOT RIGHT", "OUT at 6 — first look"),
                new("WIDE RIGHT", "GO"),
                new("TIGHT RIGHT", "OUT at 2"),
                new("BACK", "SWING to the left flat"),
                new("WIDE LEFT", "POST"),
                new("SNAPPER", "GO"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "OUT at 6. Six steps, plant, break to the right sideline."),
                new("WIDE RIGHT", "GO. Sprint straight down the sideline and take the deep defender with you."),
                new("TIGHT RIGHT", "OUT at 2. Two steps and break to the flat."),
                new("BACK", "SWING. Loop out to the left flat."),
                new("WIDE LEFT", "POST. Run up six, then break toward the middle."),
                new("SNAPPER", "Snap it and GO straight downfield."),
                new("THROWER", "Watch the defender between the Slot and the Tight Right. If he drops, throw the short out. If he comes up, throw the Slot."),
            ],
            Notes:
            [
                "Three levels on one side. The read is one kid: whoever is between the two outs.",
                "If the deep defender cheats up, the Wide Right is behind everybody.",
            ]),
        new(
            Num: 9, Name: "FLOOD SIT", Kid: "ICE CREAM",
            Formation: "FLOOD RIGHT", Category: "QUICK GAME", Complexity: PlayComplexity.Simple,
            Tagline: "The Slot sits at five, the Tight Right runs to the flat under him. Your third-down call.",
            Mistake: "The Tight Right running his out too deep. One step, then flat, so he is under the sit.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("Y", Route, [new(7.5, 0), new(7.5, 5)], Bar),
                new("F", Route, [new(4, 0), new(4, 1), new(8.5, 1)]),
                new("Z", Route, [new(11, 0), new(11, 10)]),
                new("X", Route, [new(-11, 0), new(-11, 5), new(-6.5, 5)]),
                new("H", Route, [new(-2.6, -4.4), new(-5.6, -2.9), new(-8.6, -2.4)]),
                new("C", Route, [new(0, 0), new(-1.5, 4)], Bar),
            ],
            Calls:
            [
                new("SLOT RIGHT", "SIT at 5 — first look"),
                new("TIGHT RIGHT", "OUT at 1"),
                new("WIDE RIGHT", "GO"),
                new("WIDE LEFT", "IN at 5"),
                new("BACK", "SWING to the left flat"),
                new("SNAPPER", "SIT at 4"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "SIT. Five steps, stop, turn around facing the thrower."),
                new("TIGHT RIGHT", "OUT at 1. One step, then break flat toward the right sideline."),
                new("WIDE RIGHT", "GO. Sprint straight downfield and take your defender away."),
                new("WIDE LEFT", "IN at 5. Five steps, plant, break toward the middle."),
                new("BACK", "SWING. Loop out to the left flat."),
                new("SNAPPER", "Snap it, run four steps to the left and SIT."),
                new("THROWER", "Look at the Slot. If a defender is sitting on him, the flat is open."),
            ],
            Notes:
            [
                "Third and short: two receivers, one defender between them.",
                "The Wide Left's IN and the Back's swing are the answers on the other side.",
            ]),
        new(
            Num: 10, Name: "WEAKSIDE DIVE", Kid: "BACKDOOR",
            Formation: "FLOOD RIGHT", Category: "RUN ZONE", Complexity: PlayComplexity.Moderate,
            Tagline: "Everybody floods right and the BACK runs straight up the empty left side.",
            Mistake: "The receivers jogging. If they do not run hard to the right, their defenders stay home.",
            Ball: ("carry", "H"),
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(-1.4, -3.7)], To: "H"),
                new("H", Run, [new(-2.6, -4.4), new(-1.4, -3.7), new(-1.2, 0.5), new(-2, 8)]),
                new("C", Route, [new(0, 0), new(2, 5)]),
                new("F", Route, [new(4, 0), new(5.5, 9)]),
                new("Y", Route, [new(7.5, 0), new(8, 10)]),
                new("Z", Route, [new(11, 0), new(11.5, 10)]),
                new("X", Route, [new(-11, 0), new(-11, 10)]),
            ],
            Calls:
            [
                new("BACK", "CARRY — straight up the left of the snapper"),
                new("THROWER", "hand it off, don't watch"),
                new("SNAPPER", "GO to the right"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "CARRY. Take the ball and run straight up, just left of the snapper. Do not stop."),
                new("THROWER", "Catch the snap, turn, and hand it to the Back."),
                new("SNAPPER", "Snap it and GO up and to the right. Take your defender with you."),
                new("TIGHT RIGHT / SLOT RIGHT / WIDE RIGHT", "GO. Sprint downfield as hard as you can. You are pulling the defense right."),
                new("WIDE LEFT", "GO. Sprint downfield and take your defender deep."),
            ],
            Notes:
            [
                "Three receivers to the right drag three defenders with them. The left side is the Back's.",
                "Call it once the defense has started shifting toward the flood.",
            ]),
        new(
            Num: 11, Name: "FLOOD SPOTS", Kid: "BINGO",
            Formation: "FLOOD RIGHT", Category: "NO-RUN ZONE", Complexity: PlayComplexity.Simple,
            Tagline: "Everybody runs to a spot and turns around. Find the one with nobody near him.",
            Mistake: "Receivers drifting toward the ball once they stop. Sit on your spot and let it come.",
            Ball: ("pass", "F"),
            Paths:
            [
                new("F", Route, [new(4, 0), new(4, 3)], Bar),
                new("Y", Route, [new(7.5, 0), new(7.5, 5), new(11.5, 5)]),
                new("Z", Route, [new(11, 0), new(11, 12)]),
                new("X", Route, [new(-11, 0), new(-11, 5)], Bar),
                new("H", Route, [new(-2.6, -4.4), new(-5.6, -2.9), new(-8.6, -2.4)]),
                new("C", Route, [new(0, 0), new(-1, 5)], Bar),
            ],
            Calls:
            [
                new("TIGHT RIGHT", "SIT at 3 — first look"),
                new("SLOT RIGHT", "OUT at 5"),
                new("WIDE RIGHT", "GO"),
                new("WIDE LEFT", "SIT at 5"),
                new("BACK", "SWING to the left flat"),
                new("SNAPPER", "SIT at 5"),
            ],
            Assign:
            [
                new("TIGHT RIGHT", "SIT. Three steps, stop, and turn around facing the thrower."),
                new("SLOT RIGHT", "OUT at 5. Five steps, plant, break toward the right sideline."),
                new("WIDE RIGHT", "GO. Sprint downfield and take the deep defender away."),
                new("WIDE LEFT", "SIT. Five steps, stop, turn around."),
                new("BACK", "SWING. Loop out to the left flat."),
                new("SNAPPER", "Snap it, run five steps to the left and SIT."),
                new("THROWER", "Look right first. Throw to whoever has no defender standing next to him."),
            ],
            Notes:
            [
                "Your no-run-zone call: five places to throw it, spread across the whole width.",
                "Count it out at practice so the kids see it: somebody is always alone.",
            ]),
        new(
            Num: 12, Name: "SLOT POST", Kid: "ARROWHEAD",
            Formation: "FLOOD RIGHT", Category: "SHOT PLAY", Complexity: PlayComplexity.Moderate,
            Tagline: "The Wide Right sits, the Slot runs a post behind him. High-low on one defender.",
            Mistake: "The Slot breaking before six steps. Too early and he runs into the sit.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("Y", Route, [new(7.5, 0), new(7.5, 6), new(3, 11)]),
                new("Z", Route, [new(11, 0), new(11, 5)], Bar),
                new("F", Route, [new(4, 0), new(4, 3), new(8, 3)]),
                new("X", Route, [new(-11, 0), new(-11, 10)]),
                new("H", Route, [new(-2.6, -4.4), new(-5.6, -2.9), new(-8.6, -2.4)]),
                new("C", Route, [new(0, 0), new(-2, 9)]),
            ],
            Calls:
            [
                new("SLOT RIGHT", "POST — first look"),
                new("WIDE RIGHT", "SIT at 5"),
                new("TIGHT RIGHT", "OUT at 3"),
                new("BACK", "SWING to the left flat"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "POST. Run up six, then break toward the middle. Look for it over your inside shoulder."),
                new("WIDE RIGHT", "SIT. Five steps, stop, turn around."),
                new("TIGHT RIGHT", "OUT at 3. Three steps and break toward the sideline."),
                new("BACK", "SWING. Loop out to the left flat."),
                new("WIDE LEFT / SNAPPER", "GO. Sprint downfield."),
                new("THROWER", "Post first. If the defender stays deep with him, throw the sit."),
            ],
            Notes:
            [
                "The high-low is one defender's problem. Teach the thrower to watch that one kid.",
                "The Tight Right's out is the third answer, under both of them.",
            ]),

        // ---------------------------------------------------------- FIVE WIDE
        new(
            Num: 13, Name: "SPEED SWEEP", Kid: "COMET",
            Formation: "FIVE WIDE", Category: "RUN ZONE", Complexity: PlayComplexity.Moderate,
            Tagline: "The Slot Left runs across at full speed, takes the ball, and races to the right edge.",
            Mistake: "The Slot Left slowing down for the handoff. Full speed past the thrower, or the play is dead.",
            Ball: ("carry", "F"),
            Paths:
            [
                new("F", Motion, [new(-7.5, 0), new(-4.5, -1.6), new(-1.5, -1.6)]),
                new("QB", Handoff, [new(0, -3), new(-1, -1.8)], To: "F"),
                new("F", Run, [new(-1.5, -1.6), new(3, -1.4), new(8, -0.6), new(11.5, 6.5)]),
                new("H", Route, [new(3.5, 0), new(3, 9)]),
                new("Y", Route, [new(7.5, 0), new(7, 10)]),
                new("Z", Route, [new(12, 0), new(13, 10)]),
                new("X", Route, [new(-12, 0), new(-12, 8)]),
                new("C", Route, [new(0, 0), new(0, 5)]),
            ],
            Calls:
            [
                new("SLOT LEFT", "MOTION → CARRY right"),
                new("THROWER", "hold it out at belly height"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("SLOT LEFT", "MOTION, then CARRY. Start on the coach's signal, be at FULL SPEED when you pass the thrower, take the ball and run for the right sideline."),
                new("THROWER", "Catch the snap and hold the ball out at belly height. He takes it — do not push it at him."),
                new("TIGHT RIGHT / SLOT RIGHT / WIDE RIGHT", "GO. Sprint downfield and take the right side's defenders away from him."),
                new("WIDE LEFT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "Practise the motion twenty times before the handoff. Timing is the whole play.",
                "When the defense starts chasing it, call Speed Keep, and after that Speed Pass.",
            ]),
        new(
            Num: 14, Name: "SPEED PASS", Kid: "SHOOTING STAR",
            Formation: "FIVE WIDE", Category: "SHOT PLAY", Complexity: PlayComplexity.Advanced,
            Tagline: "Speed Sweep for two steps, then the Slot Left stops behind the line and throws it deep.",
            Mistake: "The Slot Left throwing on the run or past the line. Stop, feet set, behind the line — then throw.",
            Ball: ("pass", "Z"),
            Thrower: "F",
            Paths:
            [
                new("F", Motion, [new(-7.5, 0), new(-4.5, -1.6), new(-1.5, -1.6)]),
                new("QB", Handoff, [new(0, -3), new(-1, -1.8)], To: "F"),
                new("F", Run, [new(-1.5, -1.6), new(3, -1.4), new(5.5, -1.7)], Bar),
                new("Z", Route, [new(12, 0), new(13, 12)]),
                new("H", Route, [new(3.5, 0), new(3, 9)]),
                new("Y", Route, [new(7.5, 0), new(7, 10)]),
                new("X", Route, [new(-12, 0), new(-12, 8)]),
                new("C", Route, [new(0, 0), new(0, 5)]),
            ],
            Calls:
            [
                new("SLOT LEFT", "MOTION → CARRY right, stop, throw it deep"),
                new("WIDE RIGHT", "GO up the sideline — look back"),
                new("THROWER", "hold it out at belly height"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("SLOT LEFT", "MOTION, CARRY, then THROW. The same start as Speed Sweep. Two steps with the ball, then STOP before the line, set your feet, and throw it deep to the Wide Right. Covered? Tuck it and run."),
                new("WIDE RIGHT", "GO. Sprint straight up the right sideline and look back over your inside shoulder. Your defender will come up for the sweep — run past him."),
                new("THROWER", "The same as Speed Sweep: hold it out and let him take it. Then get out of the way."),
                new("TIGHT RIGHT / SLOT RIGHT / WIDE LEFT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "Speed Sweep first, at least twice. This is the punishment for a defender who comes up to chase it.",
                "Legal check: the throw leaves from behind the line, and there is one forward pass per play. A few leagues let only the thrower pass — check yours.",
            ]),
        new(
            Num: 15, Name: "FIVE VERTS", Kid: "THUNDERSTORM",
            Formation: "FIVE WIDE", Category: "SHOT PLAY", Complexity: PlayComplexity.Simple,
            Tagline: "Five receivers straight downfield. Somebody runs past his defender.",
            Mistake: "Receivers looking back too early and slowing down. Run first, look later.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("X", Route, [new(-12, 0), new(-12.5, 12)]),
                new("F", Route, [new(-7.5, 0), new(-6.5, 12)]),
                new("H", Route, [new(3.5, 0), new(2.5, 12)]),
                new("Y", Route, [new(7.5, 0), new(7.5, 12)]),
                new("Z", Route, [new(12, 0), new(12.5, 12)]),
                new("C", Route, [new(0, 0), new(-1, 5)], Bar),
            ],
            Calls:
            [
                new("SLOT RIGHT", "GO — first look"),
                new("EVERYONE ELSE", "GO"),
                new("SNAPPER", "SIT at 5"),
            ],
            Assign:
            [
                new("SLOT RIGHT", "GO. Sprint straight downfield. Look back after ten steps."),
                new("WIDE LEFT / SLOT LEFT / TIGHT RIGHT / WIDE RIGHT", "GO. Sprint straight downfield as fast as you can."),
                new("SNAPPER", "Snap it, run five steps and SIT. You are the safe throw."),
                new("THROWER", "Look at the Slot Right. If the defender is beside him and not behind him, throw it deep."),
            ],
            Notes:
            [
                "Five deep runners: somebody's defender is going to lose the race. Find him.",
                "The Snapper sitting at five is the answer when everybody is covered.",
            ]),
        new(
            Num: 16, Name: "ALL SPOTS", Kid: "CONNECT FOUR",
            Formation: "FIVE WIDE", Category: "NO-RUN ZONE", Complexity: PlayComplexity.Simple,
            Tagline: "Five receivers stop on five spots across the field. Throw to the one standing alone.",
            Mistake: "Receivers drifting toward the ball once they stop. Sit on your spot.",
            Ball: ("pass", "F"),
            Paths:
            [
                new("X", Route, [new(-12, 0), new(-12, 4), new(-15, 4)]),
                new("F", Route, [new(-7.5, 0), new(-7.5, 5)], Bar),
                new("H", Route, [new(3.5, 0), new(3.5, 4)], Bar),
                new("Y", Route, [new(7.5, 0), new(7.5, 6)], Bar),
                new("Z", Route, [new(12, 0), new(12, 3), new(15, 3)]),
                new("C", Route, [new(0, 0), new(-1, 3)], Bar),
            ],
            Calls:
            [
                new("SLOT LEFT", "SIT at 5 — first look"),
                new("TIGHT RIGHT", "SIT at 4"),
                new("SLOT RIGHT", "SIT at 6"),
                new("WIDE LEFT", "OUT at 4"),
                new("WIDE RIGHT", "OUT at 3"),
                new("SNAPPER", "SIT at 3"),
            ],
            Assign:
            [
                new("SLOT LEFT", "SIT. Five steps, stop, and turn around facing the thrower."),
                new("TIGHT RIGHT", "SIT. Four steps, stop, turn around."),
                new("SLOT RIGHT", "SIT. Six steps, stop, turn around."),
                new("WIDE LEFT", "OUT at 4. Four steps, plant, break toward the left sideline."),
                new("WIDE RIGHT", "OUT at 3. Three steps, plant, break toward the right sideline."),
                new("SNAPPER", "Snap it, three steps and SIT."),
                new("THROWER", "Slot Left first. Throw to whoever has no defender standing next to him."),
            ],
            Notes:
            [
                "Your no-run-zone call: six receivers spread across the field.",
                "Short and safe. Nobody runs past five steps, so the ball comes out fast.",
            ]),
        new(
            Num: 17, Name: "DOUBLE IN", Kid: "SCISSORS",
            Formation: "FIVE WIDE", Category: "QUICK GAME", Complexity: PlayComplexity.Moderate,
            Tagline: "Both slots break in at different depths. Throw the one in front of the defense.",
            Mistake: "The two slots breaking at the same depth. Four and six — never the same.",
            Ball: ("pass", "F"),
            Paths:
            [
                new("F", Route, [new(-7.5, 0), new(-7.5, 4), new(-3.5, 4)]),
                new("Y", Route, [new(7.5, 0), new(7.5, 6), new(1.5, 6)]),
                new("H", Route, [new(3.5, 0), new(4.5, 10)]),
                new("X", Route, [new(-12, 0), new(-12, 10)]),
                new("Z", Route, [new(12, 0), new(12, 10)]),
                new("C", Route, [new(0, 0), new(0, 3)], Bar),
            ],
            Calls:
            [
                new("SLOT LEFT", "IN at 4 — first look"),
                new("SLOT RIGHT", "IN at 6"),
                new("SNAPPER", "SIT at 3"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("SLOT LEFT", "IN at 4. Four steps, plant, break toward the middle. Hands up — it comes fast."),
                new("SLOT RIGHT", "IN at 6. Six steps, plant, break toward the middle. You are above the Slot Left."),
                new("SNAPPER", "Snap it, three steps and SIT."),
                new("WIDE LEFT / TIGHT RIGHT / WIDE RIGHT", "GO. Sprint downfield and clear the middle."),
                new("THROWER", "Slot Left first. If a defender is in front of him, the Slot Right is behind that defender."),
            ],
            Notes:
            [
                "The two ins finish on opposite sides of the middle at two depths, so they never meet.",
                "The three GO routes are what empty the middle. Coach them to run hard.",
            ]),
        new(
            Num: 18, Name: "SPEED KEEP", Kid: "HOCUS POCUS",
            Formation: "FIVE WIDE", Category: "RUN ZONE", Complexity: PlayComplexity.Advanced,
            Tagline: "Fake Speed Sweep to the right, and the THROWER keeps it up the left.",
            Mistake: "The thrower going too early. Let the Slot Left clear the fake, count two, then run.",
            Ball: ("carry", "QB"),
            Paths:
            [
                new("F", Motion, [new(-7.5, 0), new(-4.5, -1.6), new(-1.5, -1.6)]),
                new("F", Fake, [new(-1.5, -1.6), new(3, -1.4), new(8, -0.6)]),
                new("QB", Run, [new(0, -3), new(-2, -3.9), new(-5.5, -3.4), new(-7.8, 0.5), new(-8.2, 7)]),
                new("X", Route, [new(-12, 0), new(-12, 9)]),
                new("H", Route, [new(3.5, 0), new(3, 9)]),
                new("Y", Route, [new(7.5, 0), new(7, 10)]),
                new("Z", Route, [new(12, 0), new(12, 10)]),
                new("C", Route, [new(0, 0), new(1, 6)]),
            ],
            Calls:
            [
                new("SLOT LEFT", "MOTION → FAKE right"),
                new("THROWER", "fake, count two, CARRY left"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("SLOT LEFT", "MOTION and run the FAKE all the way to the right sideline. Arms up like you have the ball — sell it."),
                new("THROWER", "Fake the handoff, count two, then step back to the left and CARRY it up the left side."),
                new("WIDE LEFT", "GO. Sprint downfield and clear the left side for the thrower."),
                new("TIGHT RIGHT / SLOT RIGHT / WIDE RIGHT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "Only once the defense has seen Speed Sweep twice. A defense that has not been burned does not chase.",
                "Rule check: some leagues do not let the thrower run. If yours does not, this play is off the card.",
            ]),

        // -------------------------------------------------------------- HEAVY
        new(
            Num: 19, Name: "HEAVY PITCH", Kid: "SNOWPLOW",
            Formation: "HEAVY", Category: "RUN ZONE", Complexity: PlayComplexity.Simple,
            Tagline: "Pitch it to the BACK and beat the rusher to the right edge.",
            Mistake: "Pitching it behind the Back. Toss it where he is going, not where he is.",
            Ball: ("carry", "H"),
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(3.5, -4.2)], To: "H"),
                new("H", Run, [new(-1.5, -5), new(2, -4.5), new(6, -3), new(9.5, 1), new(10.5, 8)]),
                new("Z", Route, [new(9, 0), new(12.6, 3), new(13.4, 8)]),
                new("Y", Route, [new(3, 0), new(5, 9)]),
                new("F", Route, [new(-3, 0), new(-3.5, 8)]),
                new("X", Route, [new(-9, 0), new(-9, 8)]),
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
                new("TIGHT RIGHT / TIGHT LEFT / WIDE LEFT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "The rusher starts seven yards back. A pitch to the edge gets there before he does.",
                "When the defense starts flying to the right, call Heavy Reverse.",
            ]),
        new(
            Num: 20, Name: "HEAVY REVERSE", Kid: "TORNADO",
            Formation: "HEAVY", Category: "RUN ZONE", Complexity: PlayComplexity.Advanced,
            Tagline: "Heavy Pitch to the right, and the Wide Right takes it back the other way.",
            Mistake: "The Wide Right leaving before the Back gets to him. Take the ball first, then go.",
            Ball: ("carry", "Z"),
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(3.5, -4.2)], To: "H"),
                new("H", Run, [new(-1.5, -5), new(2, -4.2), new(5, -3.0), new(8.5, -2.0), new(11, 0.5)]),
                new("H", Handoff, [new(5, -3.0), new(4.6, -3.8)], To: "Z"),
                new("Z", Run, [new(9, 0), new(8.2, -2.4), new(4.6, -3.8), new(0, -4.4), new(-5, -3.8), new(-9.5, -0.5), new(-12, 7)]),
                new("X", Route, [new(-9, 0), new(-9, 5), new(-5.6, 5)]),
                new("F", Route, [new(-3, 0), new(-3.5, 9)]),
                new("Y", Route, [new(3, 0), new(3.5, 9)]),
                new("C", Route, [new(0, 0), new(-0.5, 5)]),
            ],
            Calls:
            [
                new("BACK", "CARRY right, hand it to the Wide Right"),
                new("WIDE RIGHT", "come back, CARRY to the left sideline"),
                new("THROWER", "pitch it, then get out of the way"),
                new("WIDE LEFT", "IN at 5"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "CARRY. Exactly like Heavy Pitch — catch it and run right. When the Wide Right comes back to you, hand him the ball and keep running."),
                new("WIDE RIGHT", "CARRY. Take two steps up, then come back behind the Back. Take the ball from him and run all the way to the left sideline, then up."),
                new("THROWER", "Pitch it to the Back exactly like Heavy Pitch, then step back out of the way."),
                new("WIDE LEFT", "IN at 5. Five steps, then break in. Your defender follows you away from the sideline."),
                new("TIGHT LEFT / TIGHT RIGHT / SNAPPER", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "The punishment for a defense that flies to Heavy Pitch. Run the pitch twice first.",
                "Practise the exchange walking, then jogging. A dropped reverse is a long way backwards.",
            ]),
        new(
            Num: 21, Name: "HEAVY DIVE", Kid: "BATTERING RAM",
            Formation: "HEAVY", Category: "GOAL LINE", Complexity: PlayComplexity.Moderate,
            Tagline: "The BACK takes it straight up between the two Tights. Your short-yardage run.",
            Mistake: "The Back stopping to look for a hole. Hit it, low and fast.",
            Ball: ("carry", "H"),
            Paths:
            [
                new("QB", Handoff, [new(0, -3), new(-0.8, -3.6)], To: "H"),
                new("H", Run, [new(-1.5, -5), new(-0.9, -3.8), new(1.3, 0.5), new(1.8, 5)]),
                new("Y", Route, [new(3, 0), new(3, 4), new(7, 8)]),
                new("F", Route, [new(-3, 0), new(-3, 4), new(-7, 8)]),
                new("Z", Route, [new(9, 0), new(9, 8)]),
                new("X", Route, [new(-9, 0), new(-9, 8)]),
                new("C", Route, [new(0, 0), new(0, 3)], Bar),
            ],
            Calls:
            [
                new("BACK", "CARRY — straight up, right of the snapper"),
                new("THROWER", "hand it off, don't watch"),
                new("TIGHT LEFT / TIGHT RIGHT", "CORNER"),
                new("SNAPPER", "SIT at 3"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "CARRY. Take the ball and run straight up, just right of the snapper. Do not stop."),
                new("THROWER", "Catch the snap, turn, and hand it to the Back."),
                new("TIGHT LEFT / TIGHT RIGHT", "CORNER. Run up four, then break toward your corner. Your defender goes with you."),
                new("SNAPPER", "Snap it, three steps and SIT."),
                new("WIDE LEFT / WIDE RIGHT", "GO. Sprint downfield."),
            ],
            Notes:
            [
                "Two yards to go: the two Tights take their defenders out of the middle and the Back runs through it.",
                "Rule check: many leagues have a no-run zone at the goal line. This is for when you are outside it.",
            ]),
        new(
            Num: 22, Name: "TWIN FADES", Kid: "BULLSEYE",
            Formation: "HEAVY", Category: "GOAL LINE", Complexity: PlayComplexity.Simple,
            Tagline: "Both wides fade to the back corners, both Tights sit under them. Four easy throws.",
            Mistake: "The wides drifting toward the middle. Stay on the sideline — the corner is yours.",
            Ball: ("pass", "Z"),
            Paths:
            [
                new("Z", Route, [new(9, 0), new(12.5, 12)]),
                new("X", Route, [new(-9, 0), new(-12.5, 12)]),
                new("Y", Route, [new(3, 0), new(3, 3)], Bar),
                new("F", Route, [new(-3, 0), new(-3, 3)], Bar),
                new("H", Route, [new(-1.5, -5), new(-4.5, -3.5), new(-7.5, -3)]),
                new("C", Route, [new(0, 0), new(0, 2)], Bar),
            ],
            Calls:
            [
                new("WIDE RIGHT", "GO to the back corner — first look"),
                new("WIDE LEFT", "GO to the back corner"),
                new("TIGHT LEFT / TIGHT RIGHT", "SIT at 3"),
                new("BACK", "SWING to the left flat"),
                new("SNAPPER", "SIT at 2"),
            ],
            Assign:
            [
                new("WIDE RIGHT", "GO. Run toward the back right corner of the end zone and look back over your outside shoulder."),
                new("WIDE LEFT", "GO. Run toward the back left corner and look back."),
                new("TIGHT LEFT / TIGHT RIGHT", "SIT. Three steps, stop, turn around."),
                new("BACK", "SWING. Loop out to the left flat and look back."),
                new("SNAPPER", "Snap it, two steps and SIT."),
                new("THROWER", "Throw it high to the right corner. If the defender is there, a Tight is sitting right in front of you."),
            ],
            Notes:
            [
                "Your extra-point call. A high ball to the corner is caught by your kid or nobody.",
                "Two corners and two sits means the defense cannot take them all away.",
            ]),
        new(
            Num: 23, Name: "HEAVY FLATS", Kid: "BUTTERFLY",
            Formation: "HEAVY", Category: "NO-RUN ZONE", Complexity: PlayComplexity.Simple,
            Tagline: "Both Tights break to the flats while both wides run their defenders off.",
            Mistake: "The wides stopping early. If they do not run, their defenders are standing in the flats.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("Y", Route, [new(3, 0), new(3, 2), new(7, 2)]),
                new("F", Route, [new(-3, 0), new(-3, 2), new(-7, 2)]),
                new("Z", Route, [new(9, 0), new(10, 10)]),
                new("X", Route, [new(-9, 0), new(-10, 10)]),
                new("H", Route, [new(-1.5, -5), new(-4.5, -3.5), new(-7.5, -3)]),
                new("C", Route, [new(0, 0), new(0, 4)], Bar),
            ],
            Calls:
            [
                new("TIGHT RIGHT", "OUT at 2 — first look"),
                new("TIGHT LEFT", "OUT at 2"),
                new("BOTH WIDES", "GO"),
                new("BACK", "SWING to the left flat"),
                new("SNAPPER", "SIT at 4"),
            ],
            Assign:
            [
                new("TIGHT RIGHT", "OUT at 2. Two steps, then break toward the right sideline. Catch it and turn up."),
                new("TIGHT LEFT", "OUT at 2. The same on your side."),
                new("WIDE LEFT / WIDE RIGHT", "GO. Sprint straight downfield and take your defenders away."),
                new("BACK", "SWING. Loop out to the left flat — the last throw."),
                new("SNAPPER", "Snap it, four steps and SIT."),
                new("THROWER", "Tight Right first, then Tight Left, then the Snapper in the middle."),
            ],
            Notes:
            [
                "No running in the zone, so give the defense the run look and throw short out of it.",
                "Two flats, a sit and a swing: four easy throws for a young thrower.",
            ]),
        new(
            Num: 24, Name: "HEAVY FAKE POST", Kid: "SMOKE SCREEN",
            Formation: "HEAVY", Category: "SHOT PLAY", Complexity: PlayComplexity.Moderate,
            Tagline: "Fake Heavy Dive, and the Tight Right runs a post behind the defense that came up.",
            Mistake: "The Back stopping his fake at the line. Run it like you have the ball.",
            Ball: ("pass", "Y"),
            Paths:
            [
                new("H", Fake, [new(-1.5, -5), new(-0.9, -3.8), new(1.3, 0.5)]),
                new("Y", Route, [new(3, 0), new(3, 6), new(-1.5, 11)]),
                new("Z", Route, [new(9, 0), new(9, 6), new(13.5, 11)]),
                new("F", Route, [new(-3, 0), new(-4, 10)]),
                new("X", Route, [new(-9, 0), new(-9, 10)]),
                new("C", Route, [new(0, 0), new(0, 5)], Bar),
            ],
            Calls:
            [
                new("BACK", "FAKE the dive — sell it"),
                new("TIGHT RIGHT", "POST — the shot"),
                new("WIDE RIGHT", "CORNER"),
                new("SNAPPER", "SIT at 5"),
                new("EVERYONE ELSE", "GO"),
            ],
            Assign:
            [
                new("BACK", "FAKE. Run exactly like Heavy Dive, arms folded like you have it, all the way to the line."),
                new("TIGHT RIGHT", "POST. Run up six, then break toward the middle. The defender will come up for the fake — run past him."),
                new("WIDE RIGHT", "CORNER. Run up six, then break toward the right corner."),
                new("SNAPPER", "Snap it, five steps and SIT."),
                new("TIGHT LEFT / WIDE LEFT", "GO. Sprint downfield."),
                new("THROWER", "Fake to the Back, then throw the post if the middle defender came up."),
            ],
            Notes:
            [
                "Run Heavy Dive twice first. The fake works on a defense that has been hit by the real thing.",
                "If nobody bit, the Wide Right's corner is the second look.",
            ]),
    ];

    private static Format? _format;

    /// <summary>The 7-on-7 library, as the checker, the exporter and the paper read it.</summary>
    public static Format Format => _format ??= Authoring.Build(
        "7", 7, "s_", "7 on 7", Formations, SpotMap, Notes, Library,
        Authoring.Weeks(
            "Game one. One run, one quick pass, one third down, one goal line. Master these four first.",
            1, 2, 9, 22),
        [
            ("1st down, run zone", "71 Dive (1) or Speed Sweep (13)"),
            ("2nd and short", "Flood Sit (9) or Heavy Pitch (19)"),
            ("2nd and long", "Sideline Flood (8)"),
            ("3rd and short", "Flood Sit (9) — set the sit past the sticks"),
            ("3rd and long", "Pro Smash (2) or Slot Post (12)"),
            ("Rusher is winning", "Short Side (3) or Double In (17)"),
            ("Defense chases motion", "Speed Keep (18), then Speed Pass (14)"),
            ("Defense crowds the middle", "71 Toss (5) or Weakside Dive (10)"),
            ("Defense flies to the pitch", "Heavy Reverse (20) or Pro Wheel (6)"),
            ("Run has been working", "71 Fake Post (4) or Heavy Fake Post (24)"),
            ("No-run zone", "Flood Spots (11) or All Spots (16)"),
            ("Inside the 5", "Twin Fades (22) or Heavy Dive (21)"),
            ("Extra point", "Roll Stairs (7)"),
            ("Need a big play", "Five Verts (15)"),
        ]);
}
