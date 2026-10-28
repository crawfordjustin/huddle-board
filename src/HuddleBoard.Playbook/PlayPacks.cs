namespace HuddleBoard.Playbook;

/// <summary>A saved deck a coach starts from, chosen on the tablet by name.</summary>
/// <param name="Id">Stable key; the tablet logs it and the checks look it up.</param>
/// <param name="Name">What the chip says.</param>
/// <param name="Blurb">What this deck is for, in one line for the library note.</param>
/// <param name="Plays">Play numbers, in the order the deck shows them.</param>
public sealed record PlayPack(string Id, string Name, string Blurb, IReadOnlyList<int> Plays);

/// <summary>
/// The shipped play packs: six week slots. Week 1 is a complete game deck — a
/// run, a quick pass, a third down and a goal line call — and is the deck a new
/// tablet arrives with. Weeks 2 to 6 ship empty, as slots for the decks a coach
/// builds himself, because a season's worth of somebody else's game plans was
/// more than a coach wanted to start from.
/// </summary>
/// <remarks>
/// The first pack is the deck a new tablet arrives with, so Start over and
/// Week 1 agree by construction, and it is the one pack that must hold plays.
/// A pack names plays by number and nothing else — the tablet resolves them
/// against whatever library it has, and drops any pack whose plays it cannot
/// find rather than offering a deck with holes in it. An empty week is not a
/// deck to take; the tablet offers it only as somewhere to save one.
/// </remarks>
public static class PlayPacks
{
    public static readonly IReadOnlyList<PlayPack> All =
    [
        new("week1", "Week 1",
            "Game one. One run, one quick pass, one third down, one goal line. Master these four first.",
            [1, 5, 7, 13]),
        new("week2", "Week 2",
            "Empty until you save a deck into it.",
            []),
        new("week3", "Week 3",
            "Empty until you save a deck into it.",
            []),
        new("week4", "Week 4",
            "Empty until you save a deck into it.",
            []),
        new("week5", "Week 5",
            "Empty until you save a deck into it.",
            []),
        new("week6", "Week 6",
            "Empty until you save a deck into it.",
            []),
    ];

    /// <summary>The deck a new tablet arrives with: the first pack.</summary>
    public static IReadOnlyList<int> Starting => All[0].Plays;

    /// <summary>
    /// What is wrong with the packs, as checker findings so <c>check</c> reports
    /// them next to the play findings. A pack that names a play the library does
    /// not have, or names one twice, is an error; so is a second pack with the
    /// same id or name, and so is an empty first pack, because that is the deck
    /// a new tablet arrives with.
    /// </summary>
    public static IReadOnlyList<Finding> Check(IReadOnlyList<Play> plays) => Check(plays, All);

    /// <summary>The same, for one format's packs against its own library.</summary>
    public static IReadOnlyList<Finding> Check(Format format) => Check(format.Plays, format.Packs);

    private static IReadOnlyList<Finding> Check(IReadOnlyList<Play> plays, IReadOnlyList<PlayPack> all)
    {
        var known = plays.Select(p => p.Num).ToHashSet();
        var rows = new List<Finding>();

        if (all.Select(k => k.Id).Distinct().Count() != all.Count)
            rows.Add(new(Severity.Error, 0, "PACK", "two packs share an id"));
        if (all.Select(k => k.Name).Distinct().Count() != all.Count)
            rows.Add(new(Severity.Error, 0, "PACK", "two packs share a name"));

        if (all.Count == 0 || all[0].Plays.Count == 0)
            rows.Add(new(Severity.Error, 0, "PACK", "the first pack is the starting deck and has no plays"));

        foreach (var pack in all)
        {

            foreach (var dup in pack.Plays.GroupBy(n => n).Where(g => g.Count() > 1))
                rows.Add(new(Severity.Error, dup.Key, "PACK", $"{pack.Name} names play {dup.Key} twice"));

            foreach (var num in pack.Plays.Where(n => !known.Contains(n)))
                rows.Add(new(Severity.Error, num, "PACK", $"{pack.Name} names play {num}, which does not exist"));
        }

        return rows;
    }
}
