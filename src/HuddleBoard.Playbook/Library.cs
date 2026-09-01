namespace HuddleBoard.Playbook;

/// <summary>
/// The whole play library. Plays live in <c>Plays.cs</c> (the original
/// fourteen) and <c>PlaysMore.cs</c> (everything since); this joins them and
/// puts them in play-number order.
/// </summary>
public static partial class PlayLibrary
{
    private static IReadOnlyList<Play>? _all;

    /// <summary>Every play, lowest number first.</summary>
    public static IReadOnlyList<Play> All =>
        _all ??= [.. Original.Concat(Recent).OrderBy(p => p.Num)];
}

/// <summary>
/// The spot-language text for every play, from <c>PlayTexts.cs</c> and
/// <c>PlayTextsMore.cs</c>.
/// </summary>
public static partial class PlayTexts
{
    private static IReadOnlyDictionary<int, PlayText>? _all;

    /// <summary>Play number -> what the tablet says.</summary>
    public static IReadOnlyDictionary<int, PlayText> All =>
        _all ??= Original.Concat(Recent).ToDictionary(e => e.Key, e => e.Value);
}
