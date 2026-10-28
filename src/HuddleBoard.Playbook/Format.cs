namespace HuddleBoard.Playbook;

/// <summary>
/// One division's whole library: how many kids are on the field, where they
/// line up, every play and what the tablet says about it, and the weeks a
/// coach starts from. 5-on-5, 6-on-6 and 7-on-7 are each one of these, and
/// they are distinct on purpose — their own formations, their own plays,
/// their own Week 1 — not one library with a kid taken away or added.
/// </summary>
/// <param name="Id">The key the tablet stores and syncs it under: "5", "6", "7".</param>
/// <param name="Players">Kids on the field, the SNAPPER and THROWER included.</param>
/// <param name="IdPrefix">Play ids are this plus the two-digit number, so no two
/// formats can ever name the same play.</param>
/// <param name="Label">What the tablet and the paper call it.</param>
/// <param name="Script">The paper playbook's "a simple script that works":
/// a situation, and what to call in it, by name and number.</param>
/// <remarks>
/// Formation names are unique across every format — the tablet and a stored
/// design look a formation up by name alone — and <c>QB</c> and <c>C</c> are
/// the THROWER's and SNAPPER's keys in all of them.
/// </remarks>
public sealed record Format(
    string Id,
    int Players,
    string IdPrefix,
    string Label,
    OrderedDictionary<string, OrderedDictionary<string, Pt>> Formations,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, SpotLabel>> SpotMap,
    IReadOnlyDictionary<string, string> FormationNotes,
    IReadOnlyList<Play> Plays,
    IReadOnlyDictionary<int, PlayText> Texts,
    IReadOnlyDictionary<int, string> KidNames,
    IReadOnlyDictionary<int, (string Mode, string Who)> Ball,
    IReadOnlyDictionary<int, string> Throwers,
    IReadOnlyList<PlayPack> Packs,
    IReadOnlyList<(string Situation, string Call)> Script)
{
    /// <summary>The tablet's id for play <paramref name="num"/>.</summary>
    public string PlayId(int num) => $"{IdPrefix}{num:00}";

    /// <summary>The deck a new tablet arrives with in this format: its first pack.</summary>
    public IReadOnlyList<int> Starting => Packs[0].Plays;
}

/// <summary>The divisions the tool ships for, smallest first.</summary>
public static class Formats
{
    private static Format? _six;

    /// <summary>6-on-6: the library this tool was first built around.</summary>
    public static Format Six => _six ??= new(
        "6", 6, "p_", "6 on 6",
        HuddleBoard.Playbook.Formations.All, Spots.Map, HuddleBoard.Playbook.Formations.Notes,
        PlayLibrary.All, PlayTexts.All,
        ProtoExporter.KidNames, ProtoExporter.Ball, ProtoExporter.Throwers,
        PlayPacks.All, SixScript);

    private static readonly IReadOnlyList<(string, string)> SixScript =
    [
        ("1st down, run zone", "22 Dive (1) or Jet Sweep (2)"),
        ("2nd and short", "Stick (7) or Pitch Right (4)"),
        ("2nd and long", "Flood (9)"),
        ("3rd and short", "Stick (7) — set the break past the sticks"),
        ("3rd and long", "Spacing (12) or Smash (6)"),
        ("Rusher is winning", "Double Slant (5)"),
        ("Defense chases motion", "Counter Keep (3)"),
        ("Defense crowds the middle", "Pitch Right (4) or Post / Wheel (10)"),
        ("Run has been working", "Play-Action Cross (11)"),
        ("No-run zone", "Spacing (12)"),
        ("Inside the 5", "Pylon Fade (13)"),
        ("Extra point", "Triple Out (14)"),
        ("Need a change-up", "Snapper Delay (8) — once per game"),
    ];

    /// <summary>5-on-5: the SNAPPER, the THROWER and three more.</summary>
    public static Format Five => FiveOnFive.Format;

    /// <summary>Every format, in the order Setup offers them.</summary>
    public static IReadOnlyList<Format> All => [Five, Six, Seven];

    /// <summary>7-on-7: the SNAPPER, the THROWER and five more.</summary>
    public static Format Seven => SevenOnSeven.Format;

    /// <summary>The format a formation belongs to. Formation names are unique
    /// across formats, so a name is enough.</summary>
    public static Format ByFormation(string formation) =>
        All.FirstOrDefault(f => f.Formations.ContainsKey(formation))
        ?? throw new KeyNotFoundException($"no format has a formation called {formation}");
}
