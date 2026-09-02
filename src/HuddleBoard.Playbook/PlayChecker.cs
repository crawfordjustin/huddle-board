using System.Globalization;

namespace HuddleBoard.Playbook;

/// <summary>How loudly a finding complains. An error stops the build.</summary>
public enum Severity
{
    Warning,
    Error,
}

/// <summary>One thing the checker noticed about one play.</summary>
public sealed record Finding(Severity Level, int Num, string Rule, string Message);

/// <summary>
/// Legality and legibility checker for the play library.
/// </summary>
/// <remarks>
/// Hand-drawing routes does not scale. Every bug found by eye on the first
/// fourteen plays is in here as a rule, so play 15 through play 150 get the same
/// read. Rules fall into three groups:
/// <list type="bullet">
///   <item>LEGAL — things that would draw a flag, or that the league forbids.</item>
///   <item>SAFE — things that get eight-year-olds tangled up: converging routes,
///   an arrowhead landing on somebody else's stem.</item>
///   <item>TEACHABLE — things that break the spot-and-shape vocabulary, which is
///   the part a seven-year-old actually holds in his head.</item>
/// </list>
/// This is a hard gate on the exporter: bad geometry cannot reach a tablet.
/// </remarks>
public static class PlayChecker
{
    /// <summary>Sideline, yards from the middle of the field.</summary>
    private const double Edge = 15.7;

    /// <summary>Goal line, yards downfield.</summary>
    private const double Goal = 15.0;

    /// <summary>Deepest a play may go behind the line.</summary>
    private const double Backfield = -7.0;

    /// <summary>Two arrowheads finishing this close read as a collision.</summary>
    private const double MinEndGap = 3.0;

    /// <summary>Closest two downfield routes may come to each other.</summary>
    private const double MinLane = 1.6;

    /// <summary>Farthest the receiver may be from the end of a handoff at the
    /// moment the giver gets there. A pitch is the long case.</summary>
    private const double MaxExchange = 2.0;

    /// <summary>A handoff has to start on the line the giver is actually running.</summary>
    private const double MaxOffLine = 0.5;

    /// <summary>Only judge separation past here; everyone is tight at the line.</summary>
    private const double Downfield = 1.5;

    /// <summary>Yards per second — an honest eight-year-old sprint.</summary>
    private const double Speed = 6.0;

    /// <summary>Sampling interval, in seconds.</summary>
    private const double Step = 0.06;

    /// <summary>Words that legitimately open an assignment without being one of
    /// the nine shapes.</summary>
    private static readonly HashSet<string> ExtraVerbs =
        ["MOTION", "FAKE", "SNAP", "BLOCK-FREE"];

    private static readonly char[] JobTrim = [' ', '.', ',', '—', '-'];

    private static HashSet<string> ShapeWords =>
        [.. Spots.Shapes.Select(s => s.Name)];

    // -------------------------------------------------------------- geometry

    private static double Or(double value, double fallback) =>
        value != 0 ? value : fallback;

    /// <summary>Shortest distance between segment ab and segment cd.</summary>
    private static double SegDist(Pt a, Pt b, Pt c, Pt d)
    {
        double ux = b.X - a.X, uy = b.Y - a.Y;
        double vx = d.X - c.X, vy = d.Y - c.Y;
        double wx = a.X - c.X, wy = a.Y - c.Y;

        double bigA = ux * ux + uy * uy;
        double bigB = ux * vx + uy * vy;
        double bigC = vx * vx + vy * vy;
        double bigD = ux * wx + uy * wy;
        double bigE = vx * wx + vy * wy;

        double den = bigA * bigC - bigB * bigB;
        double sN = 0.0, sD = Or(den, 1.0), tN = 0.0, tD = Or(den, 1.0);

        if (den < 1e-9)
        {
            sN = 0.0;
            sD = 1.0;
            tN = bigE;
            tD = Or(bigC, 1.0);
        }
        else
        {
            sN = bigB * bigE - bigC * bigD;
            tN = bigA * bigE - bigB * bigD;
            if (sN < 0)
            {
                sN = 0.0;
                tN = bigE;
                tD = Or(bigC, 1.0);
            }
            else if (sN > sD)
            {
                sN = sD;
                tN = bigE + bigB;
                tD = Or(bigC, 1.0);
            }
        }

        if (tN < 0)
        {
            tN = 0.0;
            sN = bigA != 0 ? Math.Min(Math.Max(-bigD, 0.0), bigA) : 0.0;
            sD = Or(bigA, 1.0);
        }
        else if (tN > tD)
        {
            tN = tD;
            sN = bigA != 0 ? Math.Min(Math.Max(bigB - bigD, 0.0), bigA) : 0.0;
            sD = Or(bigA, 1.0);
        }

        double sc = Math.Abs(sD) > 1e-9 ? sN / sD : 0.0;
        double tc = Math.Abs(tD) > 1e-9 ? tN / tD : 0.0;
        return double.Hypot(wx + sc * ux - tc * vx, wy + sc * uy - tc * vy);
    }

    /// <summary>
    /// Keep only the part of a path at or past <see cref="Downfield"/>, so the
    /// crowded line of scrimmage does not register as everybody colliding with
    /// everybody.
    /// </summary>
    private static List<(Pt A, Pt B)> ClipDownfield(IReadOnlyList<Pt> pts)
    {
        var segs = new List<(Pt, Pt)>();
        for (var i = 0; i < pts.Count - 1; i++)
        {
            Pt a = pts[i], b = pts[i + 1];
            if (a.Y >= Downfield || b.Y >= Downfield)
                segs.Add((a, b));
        }

        return segs;
    }

    // ------------------------------------------------ where everyone is, when
    // Two routes crossing on paper is not a collision if the players are there
    // at different moments — which is most of the time. So the collision rule
    // runs on position-at-time, and the pure-geometry rule below it only judges
    // whether the DRAWING is readable. Model: every kid runs the same speed
    // (they roughly do), each starts at the snap, and pre-snap motion is a head
    // start.

    /// <summary>Cumulative distance along a path, and its total length.</summary>
    private static (double[] Cum, double Total) Arc(IReadOnlyList<Pt> pts)
    {
        var cum = new double[pts.Count];
        var total = 0.0;
        for (var i = 0; i < pts.Count - 1; i++)
        {
            total += pts[i + 1].DistanceTo(pts[i]);
            cum[i + 1] = total;
        }

        return (cum, total);
    }

    /// <summary>Where a player is once he has run <paramref name="dist"/> yards.</summary>
    private static Pt At(IReadOnlyList<Pt> pts, double[] cum, double dist)
    {
        if (dist <= 0)
            return pts[0];
        if (dist >= cum[^1])
            return pts[^1];
        for (var i = 0; i < cum.Length - 1; i++)
        {
            if (dist > cum[i + 1])
                continue;
            var span = cum[i + 1] - cum[i];
            var f = span < 1e-9 ? 0.0 : (dist - cum[i]) / span;
            return new Pt(
                pts[i].X + f * (pts[i + 1].X - pts[i].X),
                pts[i].Y + f * (pts[i + 1].Y - pts[i].Y));
        }

        return pts[^1];
    }

    /// <summary>
    /// How far a point is from a path, and how far along the path the closest
    /// approach is — which, at <see cref="Speed"/>, is when the runner gets there.
    /// </summary>
    private static (double Gap, double Along) Nearest(IReadOnlyList<Pt> pts, double[] cum, Pt q)
    {
        if (pts.Count == 1)
            return (q.DistanceTo(pts[0]), 0.0);

        double best = double.MaxValue, along = 0.0;
        for (var i = 0; i < pts.Count - 1; i++)
        {
            Pt a = pts[i], b = pts[i + 1];
            double dx = b.X - a.X, dy = b.Y - a.Y;
            var len2 = (dx * dx) + (dy * dy);
            var f = len2 < 1e-9 ? 0.0
                : Math.Clamp((((q.X - a.X) * dx) + ((q.Y - a.Y) * dy)) / len2, 0.0, 1.0);
            var at = new Pt(a.X + (f * dx), a.Y + (f * dy));
            var gap = q.DistanceTo(at);
            if (gap < best)
            {
                best = gap;
                along = cum[i] + (f * Math.Sqrt(len2));
            }
        }

        return (best, along);
    }

    private static int TrackOrder(PathType type) => type switch
    {
        PathType.Motion => 0,
        PathType.Handoff => 1,
        _ => 2,
    };

    /// <summary>
    /// Stitch one player's segments into a single walked path, plus how much of
    /// it he covers before the snap (motion).
    /// </summary>
    private static (List<Pt> Pts, double Head) PlayerTrack(IEnumerable<PathSeg> paths)
    {
        var pts = new List<Pt>();
        var head = 0.0;
        var segs = paths.ToList();
        // a handoff by somebody who also runs is the exchange, not his movement —
        // his run already says where he is
        if (segs.Any(q => q.Type is PathType.Run or PathType.Route or PathType.Fake))
            segs.RemoveAll(q => q.Type == PathType.Handoff);
        foreach (var seg in segs.OrderBy(q => TrackOrder(q.Type)))
        {
            var chunk = seg.Pts.AsEnumerable();
            if (pts.Count > 0 && seg.Pts[0].DistanceTo(pts[^1]) < 0.4)
                chunk = chunk.Skip(1);
            if (seg.Type == PathType.Motion)
                head += Arc(seg.Pts).Total;
            pts.AddRange(chunk);
        }

        return (pts, head);
    }

    /// <summary>Smallest gap between two players while the play is live.</summary>
    private static (double Gap, double When) ClosestInTime(
        IReadOnlyList<Pt> t1, double h1, IReadOnlyList<Pt> t2, double h2)
    {
        var (c1, l1) = Arc(t1);
        var (c2, l2) = Arc(t2);
        var duration = (Math.Max(l1 - h1, l2 - h2) / Speed) + 0.4;
        double best = 99.0, when = 0.0;

        for (var t = 0.0; t <= duration; t += Step)
        {
            var a = At(t1, c1, h1 + (Speed * t));
            var b = At(t2, c2, h2 + (Speed * t));
            var gap = a.DistanceTo(b);
            if (gap < best)
            {
                best = gap;
                when = t;
            }
        }

        return (best, when);
    }

    // ----------------------------------------------------------------- rules

    private sealed class Report
    {
        public List<Finding> Rows { get; } = [];

        public void Err(int num, string rule, string message) =>
            Rows.Add(new Finding(Severity.Error, num, rule, message));

        public void Warn(int num, string rule, string message) =>
            Rows.Add(new Finding(Severity.Warning, num, rule, message));

        public int ErrorCount => Rows.Count(r => r.Level == Severity.Error);
    }

    private static string F(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

    private static void CheckPlay(Play p, Report rep)
    {
        var num = p.Num;
        var fm = p.Formation;
        var spots = Formations.All[fm];

        // ---- every spot on the field has exactly one job, and it starts where
        // he stands
        var seen = new OrderedDictionary<string, List<PathSeg>>();
        foreach (var path in p.Paths)
        {
            if (!seen.TryGetValue(path.Who, out var list))
                seen[path.Who] = list = [];
            list.Add(path);
        }

        var throws = !p.Paths.Any(q => q.Type is PathType.Run or PathType.Handoff);
        foreach (var key in spots.Keys)
        {
            if (seen.ContainsKey(key))
                continue;

            // the thrower having no drawn path is correct on a pass — he throws
            if (key == "QB" && throws)
                continue;

            rep.Err(num, "SAFE/no-job",
                $"{key} has no path — he will stand still while five kids run");
        }

        foreach (var (key, paths) in seen)
        {
            if (!spots.TryGetValue(key, out var origin))
            {
                rep.Err(num, "LEGAL/ghost", $"{key} is not in formation {fm}");
                continue;
            }

            // motion comes first; a mid-run handoff is never where he starts
            var first = paths.FirstOrDefault(q => q.Type == PathType.Motion)
                ?? paths.FirstOrDefault(q => q.Type != PathType.Handoff)
                ?? paths[0];
            var start = first.Pts[0];
            if (start.DistanceTo(origin) > 0.35)
            {
                rep.Err(num, "SAFE/start",
                    $"{key}'s route starts at ({F(start.X)}, {F(start.Y)}) but he lines up at " +
                    $"({F(origin.X)}, {F(origin.Y)})");
            }
        }

        // ---- a handoff starts in the giver's hands and ends in the receiver's,
        // at the same moment. Two lines crossing on paper is not an exchange if
        // one kid is not there yet, and on a reverse the second exchange is the
        // whole play, so this is judged on position-at-time like the collision
        // rule below.
        foreach (var path in p.Paths.Where(q => q.Type == PathType.Handoff))
        {
            if (path.To is null)
            {
                rep.Err(num, "LEGAL/handoff",
                    $"{path.Who} hands the ball to nobody — every handoff names who takes it");
                continue;
            }

            if (path.To == path.Who || !spots.ContainsKey(path.To))
            {
                rep.Err(num, "LEGAL/handoff",
                    $"{path.Who} hands the ball to {path.To}, who is not in formation {fm}");
                continue;
            }

            if (!seen.TryGetValue(path.To, out var takerPaths)
                || !takerPaths.Any(q => q.Type is PathType.Run or PathType.Route))
            {
                rep.Err(num, "LEGAL/handoff",
                    $"{path.To} takes the ball from {path.Who} and has nowhere to run — draw him a run");
                continue;
            }

            var giver = PlayerTrack(seen[path.Who]);
            var taker = PlayerTrack(takerPaths);
            var (giverCum, _) = Arc(giver.Pts);
            var (takerCum, _) = Arc(taker.Pts);

            var (offLine, _) = Nearest(giver.Pts, giverCum, path.Pts[0]);
            if (offLine > MaxOffLine)
            {
                rep.Err(num, "LEGAL/handoff",
                    $"{path.Who}'s handoff starts {F(offLine)} yd from anywhere he actually runs");
            }

            var (_, along) = Nearest(giver.Pts, giverCum, path.Pts[^1]);
            var when = Math.Max(0.0, (along - giver.Head) / Speed);
            var there = At(taker.Pts, takerCum, taker.Head + (Speed * when));
            var gap = there.DistanceTo(path.Pts[^1]);
            if (gap > MaxExchange)
            {
                rep.Err(num, "LEGAL/handoff",
                    $"{path.Who} hands off at ({F(path.Pts[^1].X)}, {F(path.Pts[^1].Y)}) {F(when)}s " +
                    $"into the play, but {path.To} is {F(gap)} yd away at that moment (needs {F(MaxExchange)})");
            }
        }

        // ---- everything stays on the field
        foreach (var path in p.Paths)
        {
            foreach (var pt in path.Pts)
            {
                if (Math.Abs(pt.X) > Edge)
                {
                    rep.Err(num, "LEGAL/out",
                        $"{path.Who} runs out of bounds at x={F(pt.X)} (sideline is {F(Edge)})");
                }

                if (pt.Y > Goal + 4)
                {
                    rep.Warn(num, "field/deep",
                        $"{path.Who} runs to {F(pt.Y)} yards — past the back of the end zone");
                }

                if (pt.Y < Backfield)
                {
                    rep.Err(num, "field/deep",
                        $"{path.Who} drops to {F(pt.Y)} yards behind the line");
                }
            }
        }

        // ---- ball handling pairs are allowed to be close; nobody else is
        var exempt = new HashSet<string> { "QB", "C" };
        foreach (var path in p.Paths)
        {
            if (path.Type is PathType.Handoff or PathType.Motion or PathType.Run)
                exempt.Add(path.Who);
        }

        // who is where, when — the collision rule
        var tracks = new SortedDictionary<string, (List<Pt> Pts, double Head)>(StringComparer.Ordinal);
        foreach (var (key, paths) in seen)
        {
            if (spots.ContainsKey(key))
                tracks[key] = PlayerTrack(paths);
        }

        var keys = tracks.Keys.ToList();
        for (var i = 0; i < keys.Count; i++)
        {
            for (var j = i + 1; j < keys.Count; j++)
            {
                string ka = keys[i], kb = keys[j];
                if (exempt.Contains(ka) && exempt.Contains(kb))
                    continue;

                var (pa, ha) = tracks[ka];
                var (pb, hb) = tracks[kb];
                var (gap, when) = ClosestInTime(pa, ha, pb, hb);
                if (gap < MinLane)
                {
                    rep.Err(num, "SAFE/collide",
                        $"{ka} and {kb} are {F(gap)} yd apart {F(when)}s into the play " +
                        $"(needs {F(MinLane)}) — at 8U that is a pileup, and a rub is illegal");
                }
            }
        }

        var routes = p.Paths.Where(q => q.Type is PathType.Route or PathType.Run).ToList();
        for (var i = 0; i < routes.Count; i++)
        {
            for (var j = i + 1; j < routes.Count; j++)
            {
                PathSeg a = routes[i], b = routes[j];
                if (a.Who == b.Who)
                    continue;
                if (exempt.Contains(a.Who) && exempt.Contains(b.Who))
                    continue;

                // an arrowhead landing on somebody else's stem reads as one route
                foreach (var (lead, other) in new[] { (a, b), (b, a) })
                {
                    var tip = lead.Pts[^1];
                    if (tip.Y < Downfield)
                        continue;

                    var segs = ClipDownfield(other.Pts);
                    if (segs.Count == 0)
                        continue;

                    var dd = segs.Min(s => SegDist(tip, tip, s.A, s.B));
                    if (dd < MinEndGap && (lead.End ?? EndStyle.Arrow) == EndStyle.Arrow)
                    {
                        rep.Warn(num, "SAFE/arrowhead",
                            $"{lead.Who}'s arrowhead finishes {F(dd)} yd off {other.Who}'s line — " +
                            "hard to tell whose route is whose");
                    }
                }
            }
        }

        // ---- the vocabulary: every job must be one of the nine shapes
        if (!PlayTexts.All.TryGetValue(num, out var txt))
        {
            rep.Err(num, "TEACHABLE/text", "no spot-language text — the app has nothing to say");
            return;
        }

        var shapeWords = ShapeWords;
        foreach (var (label, job) in txt.Calls)
        {
            var words = job
                .Replace("→", " ")
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim(JobTrim).ToUpperInvariant())
                .ToHashSet();

            if (words.Overlaps(shapeWords) || words.Overlaps(ExtraVerbs))
                continue;
            if (label is "THROWER" or "SNAPPER")
                continue;

            rep.Warn(num, "TEACHABLE/vocab",
                $"{label} is told \"{job}\" — not one of the nine shapes");
        }

        // ---- the call strip must name real spots in this formation
        var names = spots.Keys.Select(k => Spots.Map[fm][k].Name).ToHashSet();
        foreach (var (label, _) in txt.Calls)
        {
            if (label is "EVERYONE ELSE" or "BOTH WIDES")
                continue;
            foreach (var part in label.Split('/').Select(x => x.Trim()))
            {
                if (!names.Contains(part))
                    rep.Err(num, "LEGAL/label", $"{part} is not a spot in {fm}");
            }
        }

        // ---- category promises
        var ballIsRun = p.Paths.Any(q => q.Type is PathType.Run or PathType.Handoff);
        if (p.Category == "NO-RUN ZONE" && ballIsRun)
        {
            rep.Err(num, "LEGAL/zone",
                "categorised NO-RUN ZONE but the ball is handed off — that is a dead ball");
        }
    }

    /// <summary>Check every play and return what was found.</summary>
    public static IReadOnlyList<Finding> Check(IReadOnlyList<Play> plays)
    {
        var rep = new Report();

        var dupes = plays.GroupBy(p => p.Num).Where(g => g.Count() > 1)
            .Select(g => g.Key).Order().ToList();
        if (dupes.Count > 0)
            rep.Err(0, "LEGAL/dupe", $"play numbers used twice: [{string.Join(", ", dupes)}]");

        foreach (var name in plays.GroupBy(p => p.Name).Where(g => g.Count() > 1).Select(g => g.Key))
            rep.Err(0, "TEACHABLE/dupe", $"two plays are both called {name}");

        foreach (var p in plays.OrderBy(q => q.Num))
            CheckPlay(p, rep);

        return rep.Rows;
    }

    /// <summary>
    /// Check the library and print the result. Returns a process exit code: 1 if
    /// any error fired, so it can gate a build.
    /// </summary>
    /// <param name="quiet">
    /// When called as a build gate, stay silent unless something is actually wrong.
    /// </param>
    public static int Run(bool quiet = false, TextWriter? output = null)
    {
        var o = output ?? Console.Out;
        var plays = PlayLibrary.All;
        var rows = Check(plays);
        var errors = rows.Count(r => r.Level == Severity.Error);
        var show = !quiet || errors > 0;

        if (show)
        {
            var title = plays.ToDictionary(p => p.Num, p => p.Name);
            foreach (var group in rows.GroupBy(r => r.Num).OrderBy(g => g.Key))
            {
                o.WriteLine();
                o.WriteLine("{0,-3} {1}", group.Key,
                    title.TryGetValue(group.Key, out var t) ? t : "");
                foreach (var row in group)
                {
                    o.WriteLine("   {0,-5} {1,-20} {2}",
                        row.Level == Severity.Error ? "ERROR" : "WARN", row.Rule, row.Message);
                }
            }

            o.WriteLine();
            o.WriteLine("{0} plays checked — {1} errors, {2} warnings",
                plays.Count, errors, rows.Count - errors);
        }

        return errors > 0 ? 1 : 0;
    }
}
