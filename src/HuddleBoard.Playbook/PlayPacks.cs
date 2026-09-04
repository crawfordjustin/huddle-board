namespace HuddleBoard.Playbook;

/// <summary>A saved deck a coach starts from, chosen on the tablet by name.</summary>
/// <param name="Id">Stable key; the tablet logs it and the checks look it up.</param>
/// <param name="Name">What the chip says.</param>
/// <param name="Blurb">What this week adds, in one line for the library note.</param>
/// <param name="Plays">Play numbers, in the order the deck shows them.</param>
public sealed record PlayPack(string Id, string Name, string Blurb, IReadOnlyList<int> Plays);

/// <summary>
/// The shipped play packs: one deck per week of a season, each the week before
/// plus two plays, following the playbook's own advice — master four, then add
/// a play a week once those are automatic.
/// </summary>
/// <remarks>
/// The first pack is the deck a new tablet arrives with, so Start over and
/// Week 1 agree by construction. Packs are cumulative on purpose: a team does
/// not forget 22 DIVE in week three, so a week-three deck still has it. A
/// pack names plays by number and nothing else — the tablet resolves them
/// against whatever library it has, and drops any pack whose plays it cannot
/// find rather than offering a deck with holes in it.
/// </remarks>
public static class PlayPacks
{
    public static readonly IReadOnlyList<PlayPack> All =
    [
        new("week1", "Week 1",
            "One run, one quick pass, one third down, one goal line. Master these four before adding anything.",
            [1, 5, 7, 13]),
        new("week2", "Week 2",
            "Adds JET SWEEP to get to the edge and SPACING for the no-run zone.",
            [1, 5, 7, 13, 2, 12]),
        new("week3", "Week 3",
            "Adds FLOOD for second and long and TRIPLE OUT as a second goal line call.",
            [1, 5, 7, 13, 2, 12, 9, 14]),
        new("week4", "Week 4",
            "Adds PITCH RIGHT for short yardage and SMASH for third and long.",
            [1, 5, 7, 13, 2, 12, 9, 14, 4, 6]),
        new("week5", "Week 5",
            "Adds REVERSE, now that the defense chases the sweep, and ALL SIT for when the kids are rattled.",
            [1, 5, 7, 13, 2, 12, 9, 14, 4, 6, 22, 15]),
        new("week6", "Week 6",
            "Adds POST / WHEEL as a deep shot and SNAPPER DELAY for a rusher who is winning.",
            [1, 5, 7, 13, 2, 12, 9, 14, 4, 6, 22, 15, 10, 8]),
    ];

    /// <summary>The deck a new tablet arrives with: the first pack.</summary>
    public static IReadOnlyList<int> Starting => All[0].Plays;

    /// <summary>
    /// What is wrong with the packs, as checker findings so <c>check</c> reports
    /// them next to the play findings. A pack that names a play the library does
    /// not have, names one twice, or names none at all is an error; so is a
    /// second pack with the same id or name.
    /// </summary>
    public static IReadOnlyList<Finding> Check(IReadOnlyList<Play> plays)
    {
        var known = plays.Select(p => p.Num).ToHashSet();
        var rows = new List<Finding>();

        if (All.Select(k => k.Id).Distinct().Count() != All.Count)
            rows.Add(new(Severity.Error, 0, "PACK", "two packs share an id"));
        if (All.Select(k => k.Name).Distinct().Count() != All.Count)
            rows.Add(new(Severity.Error, 0, "PACK", "two packs share a name"));

        foreach (var pack in All)
        {
            if (pack.Plays.Count == 0)
                rows.Add(new(Severity.Error, 0, "PACK", $"{pack.Name} has no plays"));

            foreach (var dup in pack.Plays.GroupBy(n => n).Where(g => g.Count() > 1))
                rows.Add(new(Severity.Error, dup.Key, "PACK", $"{pack.Name} names play {dup.Key} twice"));

            foreach (var num in pack.Plays.Where(n => !known.Contains(n)))
                rows.Add(new(Severity.Error, num, "PACK", $"{pack.Name} names play {num}, which does not exist"));
        }

        return rows;
    }
}
