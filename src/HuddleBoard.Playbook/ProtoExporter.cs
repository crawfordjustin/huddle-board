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
        [24] = "FISHHOOK", [25] = "YO-YO", [26] = "U-TURN", [27] = "CATAPULT",
    };

    /// <summary>
    /// How the ball gets there, and to whom. The target is always the thrower's
    /// FIRST read, or the kid who ENDS UP with the ball — one rule for every play,
    /// no judgement calls. On a reverse that is the second runner; the exchanges
    /// on the way come from the handoff segments and are checked against him.
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
            [24] = ("pass", "Z"), [25] = ("carry", "H"), [26] = ("carry", "Z"),
            [27] = ("pass", "H"),
        };

    /// <summary>
    /// Who throws it, when it is not the THROWER. On an ordinary pass a handoff
    /// is a fake and the ball never leaves the thrower's hands; on a play that
    /// names somebody else here, the handoffs are how the ball reaches him, and
    /// the exporter follows that chain from the snap exactly as it does for a
    /// carry. He then throws from wherever his run ends, which has to be behind
    /// the line — a forward pass from past it is a flag.
    /// </summary>
    private static readonly IReadOnlyDictionary<int, string> Throwers = new Dictionary<int, string>
    {
        [27] = "Y",
    };

    /// <summary>The deck a new team starts with: the first play pack, which is
    /// the playbook's "start here" page. One definition, so Start over and
    /// Week 1 cannot disagree.</summary>
    private static IReadOnlyList<int> DefaultDeck => PlayPacks.Starting;

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

    /// <summary>
    /// The order the ball changes hands: from the thrower, along each handoff's
    /// <see cref="PathSeg.To"/>, until somebody keeps it. That somebody has to
    /// be the play's ball target — or, on a pass thrown by somebody other than
    /// the THROWER, the kid who throws it — which is what makes a reverse
    /// honest: the second exchange is data the tablet animates, not a note in
    /// the margin.
    /// </summary>
    /// <param name="carrier">Who must be holding it when the handoffs run out.</param>
    /// <param name="role">What <paramref name="carrier"/> is, for the error.</param>
    private static List<(string Who, int Path, int Handoff)> Exchanges(Play p, string carrier, string role)
    {
        var legs = new List<(string, int, int)>();
        var holder = "QB";
        while (true)
        {
            var hand = IndexOf(p.Paths, q => q.Type == PathType.Handoff && q.Who == holder);
            if (hand < 0)
                break;

            var to = p.Paths[hand].To
                ?? throw new InvalidOperationException($"play {p.Num}: {holder}'s handoff names nobody");
            var run = IndexOf(p.Paths, q => q.Who == to && q.Type is PathType.Run or PathType.Route);
            if (run < 0)
                throw new InvalidOperationException($"play {p.Num}: {to} takes the ball and has no run");
            if (legs.Any(l => l.Item1 == to))
                throw new InvalidOperationException($"play {p.Num}: the ball goes round in circles");

            legs.Add((to, run, hand));
            holder = to;
        }

        if (holder != carrier)
        {
            throw new InvalidOperationException(
                $"play {p.Num}: the handoffs end with {holder} but the {role} is {carrier}");
        }

        if (legs.Count == 0)
        {
            // a keeper: the thrower is the carrier and the ball never leaves him
            var run = IndexOf(p.Paths, q => q.Who == carrier && q.Type is PathType.Run or PathType.Route);
            if (run < 0)
                throw new InvalidOperationException($"play {p.Num}: no path for ball target {carrier}");
            legs.Add((carrier, run, -1));
        }

        return legs;
    }

    private static int IndexOf(IReadOnlyList<PathSeg> paths, Func<PathSeg, bool> test)
    {
        for (var i = 0; i < paths.Count; i++)
        {
            if (test(paths[i]))
                return i;
        }

        return -1;
    }

    /// <summary>Builds the JSON document for the whole library.</summary>
    public static string Serialise(IReadOnlyList<Play> plays)
    {
        var j = new JsonWriter();
        j.StartObject();
        j.Pair("schemaVersion", 2);
        j.Pair("defaultDeck", DefaultDeck.Select(n => $"p_{n:00}"));

        // the saved decks a coach can start from, by play id so the tablet can
        // resolve them against the library it actually has
        j.Key("packs").StartArray();
        foreach (var pack in PlayPacks.All)
        {
            j.StartObject();
            j.Pair("id", pack.Id);
            j.Pair("name", pack.Name);
            j.Pair("blurb", pack.Blurb);
            j.Pair("plays", pack.Plays.Select(n => $"p_{n:00}"));
            j.EndObject();
        }

        j.EndArray();
        j.Key("plays").StartArray();

        foreach (var p in plays)
        {
            var fm = p.Formation;
            var num = p.Num;
            var formation = Formations.All[fm];
            var txt = PlayTexts.All[num];
            var (mode, who) = Ball[num];
            var thrower = Throwers.GetValueOrDefault(num, "QB");

            if (!formation.ContainsKey(who))
                throw new InvalidOperationException($"play {num}: ball target {who} not in {fm}");
            if (!formation.ContainsKey(thrower))
                throw new InvalidOperationException($"play {num}: thrower {thrower} not in {fm}");

            // a carry is a chain of exchanges ending with the carrier. A pass just
            // needs its first read to be running something — unless somebody other
            // than the THROWER throws it, in which case the chain has to reach him
            // first, and he has to throw from behind the line
            var legs = mode == "carry" ? Exchanges(p, who, "ball target")
                : thrower != "QB" ? Exchanges(p, thrower, "thrower")
                : [];
            if (mode != "carry")
            {
                if (!p.Paths.Any(pa => pa.Who == who))
                    throw new InvalidOperationException($"play {num}: no path for ball target {who}");
                if (legs.Any(l => l.Who == who))
                    throw new InvalidOperationException($"play {num}: {who} cannot both carry the ball and catch it");
                if (legs.Count > 0)
                {
                    var from = p.Paths[legs[^1].Path].Pts[^1];
                    if (from.Y > 0)
                    {
                        throw new InvalidOperationException(
                            $"play {num}: {thrower} throws from {from.Y} yd past the line — " +
                            "a forward pass has to leave from behind it");
                    }
                }
            }

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
                if (path.To is { } to)
                    j.Pair("to", to);
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
            if (thrower != "QB")
                j.Pair("thrower", thrower);
            if (legs.Count > 0)
            {
                j.Key("legs").StartArray();
                foreach (var (legWho, legPath, legHand) in legs)
                {
                    j.StartObject();
                    j.Pair("who", legWho);
                    j.Pair("path", legPath);
                    if (legHand >= 0)
                        j.Pair("handoff", legHand);
                    j.EndObject();
                }

                j.EndArray();
            }

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
