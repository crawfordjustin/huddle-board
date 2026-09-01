using System.Globalization;

namespace HuddleBoard.Playbook;

/// <summary>
/// Exports the play library to the JSON the tablet reads, in colour-side
/// language. Gated on <see cref="PlayChecker"/>: bad geometry cannot reach a
/// tablet.
/// </summary>
public static class ProtoExporter
{
    /// <summary>What the kids call each play.</summary>
    private static readonly IReadOnlyDictionary<int, string> KidNames = new Dictionary<int, string>
    {
        [1] = "BULLDOZER", [2] = "ROCKET", [3] = "BOOMERANG", [4] = "RACECAR", [5] = "LIGHTNING",
        [6] = "HAMMER", [7] = "ZIPPER", [8] = "NINJA", [9] = "WATERFALL", [10] = "MOONSHOT",
        [11] = "MAGIC TRICK", [12] = "SPIDERWEB", [13] = "RAINBOW", [14] = "STAIRCASE",
        [15] = "STOP SIGN", [16] = "SLINGSHOT", [17] = "PINBALL", [18] = "ELEVATOR", [19] = "SEESAW",
        [20] = "FIREWORKS", [21] = "MOUSETRAP", [22] = "PINWHEEL", [23] = "DRAWBRIDGE",
        [24] = "FISHHOOK",
    };

    /// <summary>
    /// How the ball gets there, and to whom. The target is always the thrower's
    /// FIRST read, or the ball carrier — one rule for every play, no judgement
    /// calls.
    /// </summary>
    private static readonly IReadOnlyDictionary<int, (string Mode, string Who)> Ball =
        new Dictionary<int, (string, string)>
        {
            [1] = ("carry", "H"), [2] = ("carry", "Y"), [3] = ("carry", "QB"),
            [4] = ("carry", "H"), [5] = ("pass", "Y"), [6] = ("pass", "Z"),
            [7] = ("pass", "Y"), [8] = ("pass", "C"), [9] = ("pass", "X"),
            [10] = ("pass", "H"), [11] = ("pass", "X"), [12] = ("pass", "Y"),
            [13] = ("pass", "Y"), [14] = ("pass", "X"),
            [15] = ("pass", "Y"), [16] = ("pass", "Y"), [17] = ("pass", "Z"),
            [18] = ("pass", "Z"), [19] = ("pass", "Z"), [20] = ("pass", "Y"),
            [21] = ("carry", "H"), [22] = ("carry", "Y"), [23] = ("pass", "H"),
            [24] = ("pass", "Z"),
        };

    /// <summary>The four a new team starts with, straight from the playbook's
    /// "start here" page.</summary>
    private static readonly int[] DefaultDeck = [1, 5, 7, 13];

    /// <summary>Full spot name -> the marker tag, which side it is on, and what
    /// the coach says out loud.</summary>
    private static readonly IReadOnlyDictionary<string, (string Tag, string Side, string Spoken)>
        NameMap = new Dictionary<string, (string, string, string)>
        {
            ["WIDE LEFT"] = ("W", "blue", "WIDE BLUE"),
            ["SLOT LEFT"] = ("S", "blue", "SLOT BLUE"),
            ["TIGHT LEFT"] = ("T", "blue", "TIGHT BLUE"),
            ["TIGHT RIGHT"] = ("T", "orange", "TIGHT ORANGE"),
            ["SLOT RIGHT"] = ("S", "orange", "SLOT ORANGE"),
            ["WIDE RIGHT"] = ("W", "orange", "WIDE ORANGE"),
            ["BACK"] = ("B", "none", "BACK"),
            ["SNAPPER"] = ("SN", "none", "SNAPPER"),
            ["THROWER"] = ("QB", "none", "THROWER"),
        };

    /// <summary>
    /// Left and right become BLUE and ORANGE on the way out, because 8U players
    /// confuse left and right and because left/right inverts depending on which
    /// way the coach is facing. Applied in order.
    /// </summary>
    private static readonly (string From, string To)[] Recolour =
    [
        ("WIDE LEFT", "WIDE BLUE"), ("SLOT LEFT", "SLOT BLUE"), ("TIGHT LEFT", "TIGHT BLUE"),
        ("TIGHT RIGHT", "TIGHT ORANGE"), ("SLOT RIGHT", "SLOT ORANGE"),
        ("WIDE RIGHT", "WIDE ORANGE"),
        ("right sideline", "orange sideline"), ("left sideline", "blue sideline"),
        ("right flat", "orange flat"), ("left flat", "blue flat"),
        ("MOTION left", "MOTION to blue"), ("SWING left", "SWING to blue"),
        ("CARRY right", "CARRY to orange"), ("Roll to the left", "Roll to the blue side"),
        ("to the right sideline", "to the orange sideline"),
        ("to the left flat", "to the blue flat"),
    ];

    private const string DefaultJob =
        "GO. Sprint straight downfield and take your defender with you.";

    private static string Recolor(string text)
    {
        foreach (var (from, to) in Recolour)
            text = text.Replace(from, to, StringComparison.Ordinal);
        return text;
    }

    private static string TypeName(PathType type) => type switch
    {
        PathType.Route => "route",
        PathType.Run => "run",
        PathType.Handoff => "handoff",
        PathType.Motion => "motion",
        PathType.Fake => "fake",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    /// <summary>
    /// Resolves a call-strip label to the spot keys it covers. Deriving this
    /// rather than hand-listing it is what stops the call strip and the
    /// highlighted routes from drifting apart.
    /// </summary>
    private static IEnumerable<string> KeysForLabel(string label, string formation)
    {
        if (label == "EVERYONE ELSE")
            return [];

        var spots = Formations.All[formation].Keys
            .ToDictionary(k => k, k => Spots.Map[formation][k].Name);

        if (label == "BOTH WIDES")
            return spots.Where(e => e.Value.StartsWith("WIDE", StringComparison.Ordinal))
                .Select(e => e.Key);

        var parts = label.Split('/').Select(p => p.Trim()).ToHashSet();
        var hits = spots.Where(e => parts.Contains(e.Value)).Select(e => e.Key).ToList();
        if (hits.Count == 0)
            throw new InvalidOperationException($"unmapped call label '{label}' in {formation}");
        return hits;
    }

    /// <summary>Builds the JSON document for the whole library.</summary>
    public static string Serialise(IReadOnlyList<Play> plays)
    {
        var j = new JsonWriter();
        j.StartObject();
        j.Pair("schemaVersion", 2);
        j.Pair("defaultDeck", DefaultDeck.Select(n => $"p_{n:00}"));
        j.Key("plays").StartArray();

        foreach (var p in plays)
        {
            var fm = p.Formation;
            var num = p.Num;
            var formation = Formations.All[fm];
            var txt = PlayTexts.All[num];
            var (mode, who) = Ball[num];

            if (!formation.ContainsKey(who))
                throw new InvalidOperationException($"play {num}: ball target {who} not in {fm}");

            var hasPath = p.Paths.Any(pa =>
                pa.Who == who && (mode != "carry" || pa.Type is PathType.Run or PathType.Route));
            if (!hasPath)
                throw new InvalidOperationException($"play {num}: no path for ball target {who}");

            var jobs = txt.Calls
                .SelectMany(c => KeysForLabel(c.Label, fm))
                .Distinct()
                .Order(StringComparer.Ordinal)
                .ToList();

            j.StartObject();
            j.Pair("id", $"p_{num:00}");
            j.Pair("num", num);
            j.Pair("coachName", p.Name);
            j.Pair("kidName", KidNames[num]);
            j.Pair("category", p.Category);
            j.Pair("formation", fm);
            j.Pair("tagline", Recolor(p.Tagline));

            j.Key("spots").StartObject();
            foreach (var (key, at) in formation)
            {
                var (tag, side, spoken) = NameMap[Spots.Map[fm][key].Name];
                j.Key(key).StartObject();
                j.Pair("tag", tag);
                j.Pair("side", side);
                j.Pair("name", spoken);
                j.Pair("x", at.X);
                j.Pair("y", at.Y);
                j.EndObject();
            }

            j.EndObject();

            j.Key("paths").StartArray();
            foreach (var path in p.Paths)
            {
                j.StartObject();
                j.Pair("who", path.Who);
                j.Pair("type", TypeName(path.Type));
                j.Key("pts").StartArray();
                foreach (var pt in path.Pts)
                {
                    j.StartArray();
                    j.Value(pt.X);
                    j.Value(pt.Y);
                    j.EndArray();
                }

                j.EndArray();
                if (path.End is { } end)
                    j.Pair("end", end == EndStyle.Bar ? "bar" : "arrow");
                if (path.Delay is true)
                    j.Pair("delay", true);
                j.EndObject();
            }

            j.EndArray();

            j.Key("calls").StartArray();
            foreach (var (label, job) in txt.Calls)
            {
                j.StartArray();
                j.Value(Recolor(label));
                j.Value(Recolor(job));
                j.EndArray();
            }

            j.EndArray();

            j.Key("assign").StartObject();
            foreach (var key in formation.Keys)
            {
                var full = Spots.Map[fm][key].Name;
                var line = txt.Assign
                    .FirstOrDefault(a => a.Who.Contains(full, StringComparison.Ordinal))?.Text
                    ?? DefaultJob;
                j.Pair(key, Recolor(line));
            }

            j.EndObject();

            j.Pair("jobs", jobs);
            j.Pair("primary", who);

            j.Key("ball").StartObject();
            j.Pair("mode", mode);
            j.Pair("who", who);
            j.EndObject();

            j.EndObject();
        }

        j.EndArray();
        j.EndObject();
        return j.ToString();
    }

    /// <summary>
    /// Checks the library and writes <c>dist/proto_data.json</c>. Returns a
    /// process exit code.
    /// </summary>
    public static int Run(TextWriter? output = null)
    {
        var o = output ?? Console.Out;

        // ------------------------------------- gate: never export a bad play
        if (PlayChecker.Run(quiet: true, o) != 0)
        {
            o.WriteLine("the checker found errors — fix them before exporting");
            return 1;
        }

        var plays = PlayLibrary.All;
        var json = Serialise(plays);
        var path = Path.Combine(Workspace.Ensure(Workspace.Dist), "proto_data.json");
        Workspace.WriteText(path, json);

        o.WriteLine("wrote proto_data.json — {0} plays, {1} KB", plays.Count,
            (json.Length / 1024.0).ToString("0.0", CultureInfo.InvariantCulture));
        foreach (var p in plays)
        {
            var txt = PlayTexts.All[p.Num];
            var jobs = txt.Calls.SelectMany(c => KeysForLabel(c.Label, p.Formation)).Distinct().Count();
            o.WriteLine("  {0,-2} {1,-12} {2,-15} ball->{3,-3} jobs:{4}",
                p.Num, KidNames[p.Num], p.Name, Ball[p.Num].Who, jobs);
        }

        return 0;
    }
}
