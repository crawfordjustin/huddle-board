using HuddleBoard.Playbook;

namespace HuddleBoard.Tests;

/// <summary>
/// The geometry gate: every play is legal, safe and inside the vocabulary. This
/// is the one check that needs no browser.
/// </summary>
public sealed class PlayLibraryChecks
{
    [Fact]
    public void EveryPlayIsLegalAndSafe()
    {
        var errors = PlayChecker.Check(PlayLibrary.All)
            .Where(f => f.Level == Severity.Error)
            .Select(f => $"play {f.Num} {f.Rule}: {f.Message}")
            .ToList();

        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// The calibration set. Nothing was tuned to make these pass — if a change
    /// to the checker makes them fail, the change is wrong, not the plays.
    /// </summary>
    [Fact]
    public void TheOriginalFourteenStillPassClean()
    {
        var original = PlayLibrary.All.Where(p => p.Num <= 14).ToList();
        var errors = PlayChecker.Check(original)
            .Where(f => f.Level == Severity.Error)
            .Select(f => $"play {f.Num} {f.Rule}: {f.Message}")
            .ToList();

        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public void EveryPlayHasTextAndAUniqueNumberAndName()
    {
        var plays = PlayLibrary.All;
        Assert.Equal(plays.Count, plays.Select(p => p.Num).Distinct().Count());
        Assert.Equal(plays.Count, plays.Select(p => p.Name).Distinct().Count());
        foreach (var p in plays)
            Assert.True(PlayTexts.All.ContainsKey(p.Num), $"play {p.Num} has no spot-language text");
    }

    /// <summary>
    /// Nine shapes, and no tenth. Play 11 is a known, deliberate exception — see
    /// CLAUDE.md — so this asserts the count of offenders does not grow.
    /// </summary>
    [Fact]
    public void TheVocabularyHasNotGrown()
    {
        var offenders = PlayChecker.Check(PlayLibrary.All)
            .Where(f => f.Rule == "TEACHABLE/vocab")
            .Select(f => f.Num)
            .Distinct()
            .ToList();

        Assert.True(offenders.SequenceEqual([11]),
            "plays outside the nine shapes: " + string.Join(", ", offenders));
    }

    [Fact]
    public void TheExportRoundTripsEveryPlay()
    {
        var json = ProtoExporter.Serialise(PlayLibrary.All);
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal(PlayLibrary.All.Count, doc.RootElement.GetProperty("plays").GetArrayLength());
        Assert.Equal(4, doc.RootElement.GetProperty("defaultDeck").GetArrayLength());
    }
}
