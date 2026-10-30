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
}
