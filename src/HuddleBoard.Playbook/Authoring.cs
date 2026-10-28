namespace HuddleBoard.Playbook;

/// <summary>
/// One play as it is written for a format past the first: the geometry, what
/// the tablet says, the kids' name and who ends up with the ball, all in one
/// place. The 6-on-6 library keeps these in four parallel tables because it
/// grew that way; a new format starts from one row per play, so nothing about
/// a play can be added in one table and forgotten in another.
/// </summary>
/// <param name="Ball">The thrower's first read, or the kid who ends up with the
/// ball — the same one rule as <see cref="ProtoExporter"/>'s 6-on-6 table.</param>
/// <param name="Thrower">Who throws it, when it is not the THROWER.</param>
/// <param name="Assign">One line per spot, in spot language (LEFT/RIGHT names;
/// the exporter recolours them). The paper playbook and the tablet both read it.</param>
internal sealed record Authored(
    int Num,
    string Name,
    string Kid,
    string Formation,
    string Category,
    PlayComplexity Complexity,
    string Tagline,
    string Mistake,
    (string Mode, string Who) Ball,
    IReadOnlyList<PathSeg> Paths,
    IReadOnlyList<Call> Calls,
    IReadOnlyList<Assignment> Assign,
    IReadOnlyList<string> Notes,
    string? Thrower = null);

internal static class Authoring
{
    /// <summary>Six week slots: Week 1 is the game deck a new tablet arrives
    /// with, and the rest ship empty for the coach's own decks.</summary>
    public static IReadOnlyList<PlayPack> Weeks(string blurb, params int[] week1) =>
    [
        new("week1", "Week 1", blurb, week1),
        .. Enumerable.Range(2, 5).Select(n =>
            new PlayPack($"week{n}", $"Week {n}", "Empty until you save a deck into it.", [])),
    ];

    public static Format Build(
        string id, int players, string prefix, string label,
        OrderedDictionary<string, OrderedDictionary<string, Pt>> formations,
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, SpotLabel>> spotMap,
        IReadOnlyDictionary<string, string> notes,
        IReadOnlyList<Authored> plays,
        IReadOnlyList<PlayPack> packs,
        IReadOnlyList<(string Situation, string Call)> script) =>
        new(id, players, prefix, label, formations, spotMap, notes,
            [.. plays.OrderBy(a => a.Num).Select(a => new Play(
                a.Num, a.Name, a.Formation, a.Category, a.Tagline, a.Mistake,
                a.Paths, a.Assign, a.Notes, a.Complexity))],
            plays.ToDictionary(a => a.Num, a => new PlayText(a.Calls, a.Assign, a.Mistake, a.Notes)),
            plays.ToDictionary(a => a.Num, a => a.Kid),
            plays.ToDictionary(a => a.Num, a => a.Ball),
            plays.Where(a => a.Thrower is not null).ToDictionary(a => a.Num, a => a.Thrower!),
            packs, script);
}
