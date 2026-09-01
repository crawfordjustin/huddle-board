namespace HuddleBoard.Playbook;

/// <summary>
/// Where everybody lines up, in yards. x = left/right of the snapper (negative
/// is the offense's left, the BLUE side), y = downfield from the line of
/// scrimmage (negative is the backfield).
/// </summary>
/// <remarks>
/// The order of the spots inside a formation is the order they are exported in,
/// so leave it alone unless you mean to change the JSON.
/// </remarks>
public static class Formations
{
    public static readonly OrderedDictionary<string, OrderedDictionary<string, Pt>> All = new()
    {
        ["TWINS RIGHT"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["H"] = new(-2.6, -4.4),
            ["X"] = new(-9, 0),
            ["Y"] = new(6, 0),
            ["Z"] = new(11, 0),
        },
        ["TRIPS LEFT"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["H"] = new(7, 0),
            ["X"] = new(-11, 0),
            ["Y"] = new(-7.5, 0),
            ["Z"] = new(-4, 0),
        },
        ["SPREAD"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["X"] = new(-10, 0),
            ["Y"] = new(-5, 0),
            ["H"] = new(5, 0),
            ["Z"] = new(10, 0),
        },
        ["ACE"] = new()
        {
            ["C"] = new(0, 0),
            ["QB"] = new(0, -3),
            ["H"] = new(-1.5, -5),
            ["X"] = new(-9, 0),
            ["Y"] = new(3, 0),
            ["Z"] = new(9, 0),
        },
    };

    /// <summary>Why you would call each one.</summary>
    public static readonly IReadOnlyDictionary<string, string> Notes =
        new Dictionary<string, string>
        {
            ["TWINS RIGHT"] =
                "Your base look. A ball carrier in the backfield plus two receivers stacked to the right, so you can run it or throw a two-man combo out of the same picture.",
            ["TRIPS LEFT"] =
                "Three to the left, one to the right. Overloads one side so the defense has to declare who covers whom. Best for flood and staircase-out concepts.",
            ["SPREAD"] =
                "Two each side, nobody in the backfield. Widest possible spacing — the defense cannot double anybody. This is your must-pass formation.",
            ["ACE"] =
                "Tighter set with a BACK behind the thrower. Best run look and best goal-line look, because everybody is close enough to the ball to get there fast.",
        };
}
