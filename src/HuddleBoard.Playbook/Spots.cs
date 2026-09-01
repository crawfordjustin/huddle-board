using static HuddleBoard.Playbook.EndStyle;

namespace HuddleBoard.Playbook;

/// <summary>
/// The spot-based language: places on the field instead of player positions.
/// Nobody is X or Y. Every job belongs to a SPOT, and the coach puts whichever
/// kid is on the field into that spot. Six spots are on the field at a time.
/// </summary>
public static class Spots
{
    /// <summary>Geometry key -> the short diagram tag and the full spot name.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, SpotLabel>> Map =
        new Dictionary<string, IReadOnlyDictionary<string, SpotLabel>>
        {
            ["TWINS RIGHT"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["X"] = new("WL", "WIDE LEFT"),
                ["Y"] = new("SR", "SLOT RIGHT"),
                ["Z"] = new("WR", "WIDE RIGHT"),
                ["H"] = new("B", "BACK"),
            },
            ["TRIPS LEFT"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["X"] = new("WL", "WIDE LEFT"),
                ["Y"] = new("SL", "SLOT LEFT"),
                ["Z"] = new("TL", "TIGHT LEFT"),
                ["H"] = new("WR", "WIDE RIGHT"),
            },
            ["SPREAD"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["X"] = new("WL", "WIDE LEFT"),
                ["Y"] = new("SL", "SLOT LEFT"),
                ["H"] = new("SR", "SLOT RIGHT"),
                ["Z"] = new("WR", "WIDE RIGHT"),
            },
            ["ACE"] = new Dictionary<string, SpotLabel>
            {
                ["C"] = new("SN", "SNAPPER"),
                ["QB"] = new("QB", "THROWER"),
                ["X"] = new("WL", "WIDE LEFT"),
                ["Y"] = new("TR", "TIGHT RIGHT"),
                ["Z"] = new("WR", "WIDE RIGHT"),
                ["H"] = new("B", "BACK"),
            },
        };

    /// <summary>Every spot, and how to explain where it is.</summary>
    public static readonly IReadOnlyList<SpotDef> Glossary =
    [
        new("SN", "SNAPPER",
            "Right over the ball. Snaps it, then becomes a receiver."),
        new("QB", "THROWER",
            "Three yards behind the snapper. Takes the snap."),
        new("WL", "WIDE LEFT",
            "All the way out by the left sideline."),
        new("SL", "SLOT LEFT",
            "Halfway between the snapper and the left sideline."),
        new("TL", "TIGHT LEFT",
            "Just outside the snapper's left shoulder."),
        new("TR", "TIGHT RIGHT",
            "Just outside the snapper's right shoulder."),
        new("SR", "SLOT RIGHT",
            "Halfway between the snapper and the right sideline."),
        new("WR", "WIDE RIGHT",
            "All the way out by the right sideline."),
        new("B", "BACK",
            "Behind and beside the thrower."),
    ];

    /// <summary>
    /// The nine shapes, and no tenth. Every route a kid runs is one of these,
    /// plus the default rule below. A tenth shape costs every kid on the team,
    /// so adding one is a product decision, not a convenience.
    /// </summary>
    public static readonly IReadOnlyList<Shape> Shapes =
    [
        new("GO",
            "Sprint straight downfield as fast as you can. Do not stop.",
            [new(0, 0), new(0, 10)], Arrow),
        new("OUT",
            "Run to the number, plant, and break toward the sideline.",
            [new(0, 0), new(0, 5), new(4.5, 5)], Arrow),
        new("IN",
            "Run to the number, plant, and break toward the middle.",
            [new(0, 0), new(0, 5), new(-4.5, 5)], Arrow),
        new("SIT",
            "Run to the number, stop, and turn around facing the thrower.",
            [new(0, 0), new(0, 5)], Bar),
        new("CORNER",
            "Run up, then break at an angle toward the deep corner.",
            [new(0, 0), new(0, 6), new(4.5, 11)], Arrow),
        new("POST",
            "Run up, then break at an angle toward the deep middle.",
            [new(0, 0), new(0, 6), new(-4.5, 11)], Arrow),
        new("WHEEL",
            "Run out behind the line, get wide, then turn straight up the sideline.",
            [new(0, -1), new(3.5, -1.5), new(5.5, 0), new(6, 10)], Arrow),
        new("SWING",
            "Loop out of the backfield toward the sideline and look back.",
            [new(0, -4), new(-3, -2.5), new(-6, -2)], Arrow),
        new("CARRY",
            "You get the ball. Run where the coach points.",
            [new(0, -4), new(0.8, -2), new(1.5, 7)], Arrow),
    ];

    /// <summary>What to tell a kid who forgets his job.</summary>
    public const string DefaultRule =
        "If the coach did not give you a job, run GO — sprint straight downfield and take your defender with you.";
}
