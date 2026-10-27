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
        var plays = doc.RootElement.GetProperty("plays").EnumerateArray().ToList();
        Assert.Equal(PlayLibrary.All.Count, plays.Count);
        Assert.Equal(4, doc.RootElement.GetProperty("defaultDeck").GetArrayLength());
        Assert.Equal(PlayPacks.All.Count, doc.RootElement.GetProperty("packs").GetArrayLength());
        Assert.All(plays, p => Assert.True(
            new[] { "simple", "moderate", "advanced" }.Contains(p.GetProperty("complexity").GetString()),
            "play exported an unknown complexity"));
        Assert.Equal("simple", plays.Single(p => p.GetProperty("num").GetInt32() == 1)
            .GetProperty("complexity").GetString());
        Assert.Equal("advanced", plays.Single(p => p.GetProperty("num").GetInt32() == 27)
            .GetProperty("complexity").GetString());
    }

    /// <summary>
    /// The first pack is real plays, none twice, and the deck a tablet arrives
    /// with — so Start over and Week 1 cannot drift apart. The rest ship as
    /// empty slots for a coach's own decks.
    /// </summary>
    [Fact]
    public void Week1IsTheStartingDeckAndTheRestShipEmpty()
    {
        var findings = PlayPacks.Check(PlayLibrary.All).Select(f => f.Message).ToList();
        Assert.True(findings.Count == 0, string.Join("\n", findings));
        Assert.Equal([1, 5, 7, 13], PlayPacks.Starting);

        // week 1 is a whole game deck on its own: a run, a pass, a goal line call,
        // and small enough to find a play in without looking
        var cat = PlayLibrary.All.ToDictionary(p => p.Num, p => p.Category);
        var week1 = PlayPacks.All[0];
        var cats = week1.Plays.Select(n => cat[n]).ToList();
        Assert.True(cats.Contains("RUN ZONE"), "Week 1 has no run");
        Assert.True(cats.Any(c => c != "RUN ZONE" && c != "GOAL LINE"), "Week 1 has no pass");
        Assert.True(cats.Contains("GOAL LINE"), "Week 1 has no goal line call");
        Assert.InRange(week1.Plays.Count, 4, 6);

        // six slots in all, and every one after the first is the coach's to fill
        Assert.Equal(6, PlayPacks.All.Count);
        foreach (var pack in PlayPacks.All.Skip(1))
            Assert.True(pack.Plays.Count == 0, $"{pack.Name} ships with plays in it");
    }
}
