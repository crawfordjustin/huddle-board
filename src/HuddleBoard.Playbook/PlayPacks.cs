namespace HuddleBoard.Playbook;

/// <summary>A saved deck a coach starts from, chosen on the tablet by name.</summary>
/// <param name="Id">Stable key; the tablet logs it and the checks look it up.</param>
/// <param name="Name">What the chip says.</param>
/// <param name="Blurb">What this deck is for, in one line for the library note.</param>
/// <param name="Plays">Play numbers, in the order the deck shows them.</param>
public sealed record PlayPack(string Id, string Name, string Blurb, IReadOnlyList<int> Plays);

/// <summary>
/// The shipped play packs: one complete game deck per week of a season. Every
/// week stands on its own — a run, a quick pass, something downfield or for the
/// no-run zone, and a goal line call — and the weeks together cover the whole
/// library, so a team that plays the season has seen every play once.
/// </summary>
/// <remarks>
/// The first pack is the deck a new tablet arrives with, so Start over and
/// Week 1 agree by construction. Weeks are standalone rather than cumulative on
/// purpose: a deck is what a coach carries into one game, and six plays he can
/// find without looking beats fourteen he has to hunt through. Where a week
/// brings a reverse, the sweep or pitch it punishes was a week or two before.
/// A pack names plays by number and nothing else — the tablet resolves them
/// against whatever library it has, and drops any pack whose plays it cannot
/// find rather than offering a deck with holes in it.
/// </remarks>
public static class PlayPacks
{
    public static readonly IReadOnlyList<PlayPack> All =
    [
        new("week1", "Week 1",
            "Game one. One run, one quick pass, one third down, one goal line. Master these four first.",
            [1, 5, 7, 13]),
        new("week2", "Week 2",
            "Get to the edge. JET SWEEP and SMASH, ALL SIT for the no-run zone, TRIPLE OUT at the goal line, FLOOD for a shot.",
            [2, 6, 15, 14, 9]),
        new("week3", "Week 3",
            "Same looks, new answers. COUNTER KEEP off the dive, BUBBLE, SPACING, FLAT DUMP at the goal line, POST / WHEEL deep.",
            [3, 17, 12, 23, 10]),
        new("week4", "Week 4",
            "The pitch and the punishment. PITCH RIGHT, then JET REVERSE now that they chase the sweep. SNAG, SLANT FLAT, FOUR VERTS.",
            [4, 25, 24, 16, 13, 18]),
        new("week5", "Week 5",
            "Make them hesitate. REVERSE and DRAW off plays they have seen, SNAPPER DELAY for a rusher who is winning, HIGH LOW, DOUBLE POST.",
            [22, 21, 8, 19, 14, 20]),
        new("week6", "Week 6",
            "Playoff week. The best of everything: 22 DIVE, PITCH REVERSE, STICK, PLAY-ACTION CROSS, SPACING, PYLON FADE.",
            [1, 26, 7, 11, 12, 13]),
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
