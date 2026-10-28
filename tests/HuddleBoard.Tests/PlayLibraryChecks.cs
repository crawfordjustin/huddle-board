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

    /// <summary>
    /// The formats after the first start with no exception to the nine shapes,
    /// and every one of their plays passes the same gate as 6-on-6's.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    public void EveryFormatIsLegalSafeAndInsideTheNineShapes(int players)
    {
        var f = Formats.All.Single(x => x.Players == players);
        var findings = PlayChecker.Check(f.Plays, f);
        var errors = findings.Where(x => x.Level == Severity.Error)
            .Select(x => $"{f.Label} play {x.Num} {x.Rule}: {x.Message}").ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));

        var vocab = findings.Where(x => x.Rule == "TEACHABLE/vocab").Select(x => $"play {x.Num}: {x.Message}");
        Assert.Empty(vocab);
        Assert.Empty(PlayPacks.Check(f));
    }

    /// <summary>
    /// A format is its roster: every formation, and so every play, has exactly
    /// the SNAPPER, the THROWER and the rest — and no two formats share a
    /// formation name, because the tablet finds a formation by name alone.
    /// </summary>
    [Fact]
    public void EveryPlayFieldsItsFormatsRosterAndFormationNamesAreUnique()
    {
        foreach (var f in Formats.All)
        {
            Assert.InRange(f.Plays.Count, 20, 40);
            foreach (var (name, spots) in f.Formations)
            {
                Assert.Equal(f.Players, spots.Count);
                Assert.True(spots.ContainsKey("QB") && spots.ContainsKey("C"), $"{name} has no THROWER or SNAPPER");
                Assert.Equal(spots.Keys.Order(), f.SpotMap[name].Keys.Order());
            }

            foreach (var p in f.Plays)
                Assert.True(f.Formations.ContainsKey(p.Formation), $"{f.Label} play {p.Num}: {p.Formation} is not its formation");
        }

        var names = Formats.All.SelectMany(f => f.Formations.Keys).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
        var ids = Formats.All.Select(f => f.IdPrefix).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    /// <summary>
    /// The whole document carries every format, each a library of its own, and
    /// every play says how many are on the field.
    /// </summary>
    [Fact]
    public void TheExportCarriesEveryFormat()
    {
        using var doc = System.Text.Json.JsonDocument.Parse(ProtoExporter.Serialise());
        var formats = doc.RootElement.GetProperty("formats");
        foreach (var f in Formats.All)
        {
            var o = formats.GetProperty(f.Id);
            Assert.Equal(f.Players, o.GetProperty("players").GetInt32());
            var plays = o.GetProperty("plays").EnumerateArray().ToList();
            Assert.Equal(f.Plays.Count, plays.Count);
            Assert.All(plays, p =>
            {
                Assert.StartsWith(f.IdPrefix, p.GetProperty("id").GetString());
                Assert.Equal(f.Players, p.GetProperty("spots").EnumerateObject().Count());
                Assert.Equal(f.Players, p.GetProperty("players").GetInt32());
            });
            Assert.Equal(f.Starting.Select(f.PlayId),
                o.GetProperty("defaultDeck").EnumerateArray().Select(e => e.GetString()));
        }
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

    /// <summary>The same promise, in every format: Week 1 is a whole game and the rest are slots.</summary>
    [Fact]
    public void EveryFormatsWeek1IsAWholeGame()
    {
        foreach (var f in Formats.All)
        {
            var cat = f.Plays.ToDictionary(p => p.Num, p => p.Category);
            var cats = f.Starting.Select(n => cat[n]).ToList();
            Assert.True(cats.Contains("RUN ZONE"), $"{f.Label} Week 1 has no run");
            Assert.True(cats.Any(c => c != "RUN ZONE" && c != "GOAL LINE"), $"{f.Label} Week 1 has no pass");
            Assert.True(cats.Contains("GOAL LINE"), $"{f.Label} Week 1 has no goal line call");
            Assert.InRange(f.Starting.Count, 4, 6);
            Assert.Equal(6, f.Packs.Count);
            foreach (var pack in f.Packs.Skip(1))
                Assert.True(pack.Plays.Count == 0, $"{f.Label} {pack.Name} ships with plays in it");
        }
    }
}
