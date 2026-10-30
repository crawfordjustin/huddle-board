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
/// <param name="Label">What the tablet calls it.</param>
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
    IReadOnlyList<Play> Plays,
    IReadOnlyDictionary<int, PlayText> Texts,
    IReadOnlyDictionary<int, string> KidNames,
    IReadOnlyDictionary<int, (string Mode, string Who)> Ball,
    IReadOnlyDictionary<int, string> Throwers,
    IReadOnlyList<PlayPack> Packs)
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
        HuddleBoard.Playbook.Formations.All, Spots.Map,
        PlayLibrary.All, PlayTexts.All,
        ProtoExporter.KidNames, ProtoExporter.Ball, ProtoExporter.Throwers,
        PlayPacks.All);

    /// <summary>5-on-5: the SNAPPER, the THROWER and three more.</summary>
    public static Format Five => FiveOnFive.Format;

    /// <summary>Every format, in the order Setup offers them.</summary>
    public static IReadOnlyList<Format> All => [Five, Six, Seven];

    /// <summary>7-on-7: the SNAPPER, the THROWER and five more.</summary>
    public static Format Seven => SevenOnSeven.Format;
}
