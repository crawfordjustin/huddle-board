using System.Globalization;
using System.Text;

namespace HuddleBoard.Playbook.Print;

/// <summary>
/// Draws the field, the routes and the markers as SVG, for the paper playbook
/// and the field cards. The screen app draws its own; this is the print one.
/// </summary>
internal static class FieldDiagram
{
    /// <summary>Pixels per yard.</summary>
    private const double Sx = 15.0;

    /// <summary>Pixels per yard.</summary>
    private const double Sy = 15.0;

    /// <summary>x = 0 (the snapper) in pixels.</summary>
    private const double Cx = 244.0;

    /// <summary>y = 0 (the line of scrimmage) in pixels.</summary>
    private const double Los = 240.0;

    public const int W = 488;
    public const int H = 342;

    public static double Px(double x) => Cx + (x * Sx);

    public static double Py(double y) => Los - (y * Sy);

    public static readonly IReadOnlyDictionary<string, string> Colours = new Dictionary<string, string>
    {
        ["route"] = "#1b3c6e",
        ["run"] = "#127a4d",
        ["handoff"] = "#5b6472",
        ["motion"] = "#c9700d",
        ["fake"] = "#127a4d",
        ["rush"] = "#c0392b",
        ["field"] = "#f7f6f2",
        ["line"] = "#cfd6dd",
        ["los"] = "#7b8794",
    };

    public static readonly IReadOnlyDictionary<string, string> CategoryColours =
        new Dictionary<string, string>
        {
            ["RUN ZONE"] = "#127a4d",
            ["QUICK GAME"] = "#1b6ba8",
            ["SHOT PLAY"] = "#8a3ab0",
            ["NO-RUN ZONE"] = "#c9700d",
            ["GOAL LINE"] = "#c0392b",
        };

    // ------------------------------------------------------------ formatting

    /// <summary>One decimal place, rounding halves to even.</summary>
    public static string F(double v) =>
        Math.Round(v, 1, MidpointRounding.ToEven).ToString("F1", CultureInfo.InvariantCulture);

    /// <summary>Whole pixels, rounding halves to even.</summary>
    public static string F0(double v) =>
        Math.Round(v, 0, MidpointRounding.ToEven).ToString("F0", CultureInfo.InvariantCulture);

    /// <summary>The shortest form that round-trips, with a trailing .0 kept.</summary>
    public static string R(double v)
    {
        var s = v.ToString("R", CultureInfo.InvariantCulture);
        return s.Contains('.') || s.Contains('E') ? s : s + ".0";
    }

    /// <summary>Escapes text for HTML the same way Python's html.escape does.</summary>
    public static string Esc(string t) => t
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal)
        .Replace("'", "&#x27;", StringComparison.Ordinal);

    // -------------------------------------------------------------- geometry

    /// <summary>Moves p0 toward p1 by <paramref name="dist"/> pixels.</summary>
    private static (double X, double Y) Trim((double X, double Y) p0, (double X, double Y) p1, double dist)
    {
        double dx = p1.X - p0.X, dy = p1.Y - p0.Y;
        var d = double.Hypot(dx, dy);
        if (d < dist || d == 0)
            return p0;
        return (p0.X + (dx / d * dist), p0.Y + (dy / d * dist));
    }

    /// <summary>The cross-bar that means "stop here and turn around".</summary>
    private static (double X1, double Y1, double X2, double Y2) Bar(
        (double X, double Y) prev, (double X, double Y) end, double length = 13)
    {
        double dx = end.X - prev.X, dy = end.Y - prev.Y;
        var d = double.Hypot(dx, dy);
        if (d == 0)
            d = 1;
        double nx = -dy / d, ny = dx / d;
        var h = length / 2;
        return (end.X - (nx * h), end.Y - (ny * h), end.X + (nx * h), end.Y + (ny * h));
    }

    // ------------------------------------------------------------------ svg

    public static string Defs()
    {
        var b = new StringBuilder("<defs>");
        foreach (var key in new[] { "route", "run", "handoff", "motion", "fake", "rush" })
        {
            b.Append($"<marker id=\"ah-{key}\" viewBox=\"0 0 10 10\" refX=\"8.5\" refY=\"5\" ")
                .Append("markerWidth=\"5.2\" markerHeight=\"5.2\" orient=\"auto-start-reverse\">")
                .Append($"<path d=\"M 0 0 L 10 5 L 0 10 z\" fill=\"{Colours[key]}\"/></marker>");
        }

        return b.Append("</defs>").ToString();
    }

    public static string FieldBackground()
    {
        var b = new StringBuilder();
        b.Append($"<rect x=\"0\" y=\"0\" width=\"{W}\" height=\"{H}\" rx=\"10\" fill=\"{Colours["field"]}\"/>");

        // sidelines
        foreach (var xv in new[] { -15.6, 15.6 })
        {
            b.Append($"<line x1=\"{F(Px(xv))}\" y1=\"10\" x2=\"{F(Px(xv))}\" y2=\"{H - 10}\" ")
                .Append("stroke=\"#b9c2cb\" stroke-width=\"2.5\"/>");
        }

        // yard lines downfield
        foreach (var yv in new[] { 5, 10, 15 })
        {
            b.Append($"<line x1=\"{F(Px(-15.6))}\" y1=\"{F(Py(yv))}\" x2=\"{F(Px(15.6))}\" y2=\"{F(Py(yv))}\" ")
                .Append($"stroke=\"{Colours["line"]}\" stroke-width=\"1.2\" stroke-dasharray=\"5 6\"/>");
            b.Append($"<text x=\"{F(Px(-15.6) + 7)}\" y=\"{F(Py(yv) - 4)}\" font-size=\"10.5\" ")
                .Append($"fill=\"#9aa5b1\" font-weight=\"600\">{yv} yd</text>");
        }

        b.Append($"<line x1=\"{F(Px(-5.5))}\" y1=\"{F(Py(-5))}\" x2=\"{F(Px(5.5))}\" y2=\"{F(Py(-5))}\" ")
            .Append($"stroke=\"{Colours["line"]}\" stroke-width=\"1\" stroke-dasharray=\"3 5\"/>");

        // line of scrimmage
        b.Append($"<line x1=\"{F(Px(-15.6))}\" y1=\"{F(Los)}\" x2=\"{F(Px(15.6))}\" y2=\"{F(Los)}\" ")
            .Append($"stroke=\"{Colours["los"]}\" stroke-width=\"2.6\"/>");
        return b.ToString();
    }

    /// <summary>The rusher, seven yards back — the clock every play runs against.</summary>
    public static string RusherMark()
    {
        double rx = Px(0), ry = Py(7);
        return $"<g><circle cx=\"{R(rx)}\" cy=\"{R(ry)}\" r=\"14\" fill=\"{Colours["field"]}\" opacity=\"0.92\"/>"
            + $"<g stroke=\"{Colours["rush"]}\" stroke-width=\"3\" stroke-linecap=\"round\">"
            + $"<line x1=\"{R(rx - 8)}\" y1=\"{R(ry - 8)}\" x2=\"{R(rx + 8)}\" y2=\"{R(ry + 8)}\"/>"
            + $"<line x1=\"{R(rx + 8)}\" y1=\"{R(ry - 8)}\" x2=\"{R(rx - 8)}\" y2=\"{R(ry + 8)}\"/></g></g>";
    }

    private static string StyleFor(PathType type) => type switch
    {
        PathType.Route => $"stroke=\"{Colours["route"]}\" stroke-width=\"2.6\"",
        PathType.Run => $"stroke=\"{Colours["run"]}\" stroke-width=\"3.4\"",
        PathType.Handoff => $"stroke=\"{Colours["handoff"]}\" stroke-width=\"2.2\" stroke-dasharray=\"6 4\"",
        PathType.Motion => $"stroke=\"{Colours["motion"]}\" stroke-width=\"2.2\" stroke-dasharray=\"7 5\"",
        PathType.Fake => $"stroke=\"{Colours["run"]}\" stroke-width=\"2.4\" stroke-dasharray=\"8 5\"",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private static string Key(PathType type) => type.ToString().ToLowerInvariant();

    public static string DrawPath(PathSeg p)
    {
        var pts = p.Pts.Select(q => (X: Px(q.X), Y: Py(q.Y))).ToList();
        pts[0] = Trim(pts[0], pts[1], 14);
        var end = p.End ?? EndStyle.Arrow;
        var style = StyleFor(p.Type);
        var marker = end == EndStyle.Bar ? "" : $" marker-end=\"url(#ah-{Key(p.Type)})\"";
        var d = string.Join(" ", pts.Select(q => $"{F(q.X)},{F(q.Y)}"));

        var b = new StringBuilder();
        b.Append($"<polyline points=\"{d}\" fill=\"none\" {style} stroke-linejoin=\"round\" ")
            .Append($"stroke-linecap=\"round\"{marker}/>");

        if (end == EndStyle.Bar)
        {
            var (x1, y1, x2, y2) = Bar(pts[^2], pts[^1]);
            var plain = style.Split(" stroke-dasharray")[0];
            b.Append($"<line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" ")
                .Append($"{plain} stroke-linecap=\"round\"/>");
        }

        if (p.Delay is true)
        {
            b.Append($"<text x=\"{F(pts[0].X - 20)}\" y=\"{F(pts[0].Y - 4)}\" font-size=\"10\" ")
                .Append($"text-anchor=\"end\" fill=\"{Colours["route"]}\" font-weight=\"700\">count 1-2</text>");
        }

        return b.ToString();
    }

    public static string DrawPlayer(string label, double x, double y, string? formation = null)
    {
        if (formation is not null)
            label = Spots.Map[formation][label].Tag;

        double cx = Px(x), cy = Py(y);
        if (label == "QB")
        {
            return $"<g><rect x=\"{F(cx - 12.5)}\" y=\"{F(cy - 12.5)}\" width=\"25\" height=\"25\" rx=\"5\" "
                + "fill=\"#ffffff\" stroke=\"#1f2933\" stroke-width=\"2.2\"/>"
                + $"<text x=\"{F(cx)}\" y=\"{F(cy + 4.2)}\" text-anchor=\"middle\" font-size=\"11.5\" "
                + "font-weight=\"800\" fill=\"#1f2933\">QB</text></g>";
        }

        var fill = label == "SN" ? "#eef2f6" : "#ffffff";
        var fs = label.Length < 2 ? 12.5 : 10.8;
        return $"<g><circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"12.5\" fill=\"{fill}\" "
            + "stroke=\"#1f2933\" stroke-width=\"2.2\"/>"
            + $"<text x=\"{F(cx)}\" y=\"{F(cy + 4.2)}\" text-anchor=\"middle\" font-size=\"{R(fs)}\" "
            + $"font-weight=\"800\" fill=\"#1f2933\">{label}</text></g>";
    }

    public static string PlaySvg(Play play, bool showRush = true, string? viewBox = null)
    {
        var pos = Formations.All[play.Formation];
        viewBox ??= $"0 0 {W} {H}";

        var b = new StringBuilder();
        b.Append($"<svg viewBox=\"{viewBox}\" xmlns=\"http://www.w3.org/2000/svg\" class=\"diagram\">");
        b.Append(Defs()).Append(FieldBackground());
        foreach (var p in play.Paths)
            b.Append(DrawPath(p));
        if (showRush)
            b.Append(RusherMark());
        foreach (var (label, at) in pos)
            b.Append(DrawPlayer(label, at.X, at.Y, play.Formation));
        return b.Append("</svg>").ToString();
    }

    /// <summary>An empty formation, cropped to the part that has players in it.</summary>
    public static string FormationSvg(string name)
    {
        var top = Py(6.5);
        var empty = new Play(0, name, name, "RUN ZONE", "", "", [], [], []);
        return PlaySvg(empty, showRush: false, viewBox: $"0 {F0(top)} {W} {F0(H - top)}");
    }

    /// <summary>All nine spots at once, for the reference page and card.</summary>
    private static readonly (string Label, double X, double Y)[] MasterSpots =
    [
        ("WL", -13, 0), ("SL", -8, 0), ("TL", -3.5, 0), ("SN", 0, 0),
        ("TR", 3.5, 0), ("SR", 8, 0), ("WR", 13, 0), ("QB", 0, -3),
        ("B", -2.6, -4.4),
    ];

    public static string SpotsSvg()
    {
        var top = Py(3.0);
        var b = new StringBuilder();
        b.Append($"<svg viewBox=\"0 {F0(top)} {W} {F0(H - top)}\" xmlns=\"http://www.w3.org/2000/svg\" ")
            .Append("class=\"diagram\">");
        b.Append(Defs()).Append(FieldBackground());
        foreach (var (label, x, y) in MasterSpots)
            b.Append(DrawPlayer(label, x, y));
        return b.Append("</svg>").ToString();
    }

    /// <summary>One of the nine shapes, drawn on its own with a start dot.</summary>
    public static string ShapeSvg(IReadOnlyList<Pt> pts, EndStyle end)
    {
        const double pad = 15, sc = 9.0;
        var xs = pts.Select(p => p.X.Value).ToList();
        var ys = pts.Select(p => p.Y.Value).ToList();
        double minX = xs.Min(), maxX = xs.Max(), minY = ys.Min(), maxY = ys.Max();

        var w = ((maxX - minX) * sc) + (pad * 2);
        var h = ((maxY - minY) * sc) + (pad * 2);
        w = Math.Max(w, 46);
        var offset = (w - (((maxX - minX) * sc) + (pad * 2))) / 2;

        var p = pts.Select(q => (
            X: pad + ((q.X.Value - minX) * sc) + offset,
            Y: h - pad - ((q.Y.Value - minY) * sc))).ToList();

        var st = $"stroke=\"{Colours["route"]}\" stroke-width=\"2.6\"";
        var mk = end == EndStyle.Bar ? "" : " marker-end=\"url(#ah-route)\"";

        var body = new StringBuilder();
        body.Append($"<polyline points=\"{string.Join(" ", p.Select(q => $"{F(q.X)},{F(q.Y)}"))}\" ")
            .Append($"fill=\"none\" {st} stroke-linejoin=\"round\" stroke-linecap=\"round\"{mk}/>");
        if (end == EndStyle.Bar)
        {
            var (x1, y1, x2, y2) = Bar(p[^2], p[^1]);
            body.Append($"<line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" {st} ")
                .Append("stroke-linecap=\"round\"/>");
        }

        body.Append($"<circle cx=\"{F(p[0].X)}\" cy=\"{F(p[0].Y)}\" r=\"5.5\" fill=\"#fff\" ")
            .Append("stroke=\"#1f2933\" stroke-width=\"2\"/>");

        return $"<svg viewBox=\"0 0 {F0(w)} {F0(h)}\" xmlns=\"http://www.w3.org/2000/svg\" "
            + $"class=\"shapesvg\">{Defs()}{body}</svg>";
    }

    /// <summary>What every line on a diagram means.</summary>
    public static string Legend()
    {
        var items = new[]
        {
            ("route", "Pass route"), ("run", "Ball carrier / run"),
            ("fake", "Fake (no ball)"), ("handoff", "Handoff or pitch"),
            ("motion", "Pre-snap motion"),
        };

        var b = new StringBuilder("<div class=\"legend\">");
        foreach (var (k, label) in items)
        {
            var dash = k is "handoff" or "motion" or "fake" ? " stroke-dasharray=\"6 4\"" : "";
            b.Append("<span class=\"lg\"><svg width=\"34\" height=\"12\" viewBox=\"0 0 34 12\">")
                .Append(Defs())
                .Append($"<line x1=\"1\" y1=\"6\" x2=\"27\" y2=\"6\" stroke=\"{Colours[k]}\" stroke-width=\"2.8\"")
                .Append($"{dash} marker-end=\"url(#ah-{k})\"/></svg>{Esc(label)}</span>");
        }

        b.Append("<span class=\"lg\"><svg width=\"20\" height=\"14\" viewBox=\"0 0 20 14\">")
            .Append($"<g stroke=\"{Colours["rush"]}\" stroke-width=\"2.6\" stroke-linecap=\"round\">")
            .Append("<line x1=\"4\" y1=\"3\" x2=\"15\" y2=\"11\"/><line x1=\"15\" y1=\"3\" x2=\"4\" y2=\"11\"/>")
            .Append("</g></svg>Rusher (starts 7 yds back)</span>");
        b.Append("<span class=\"lg\"><svg width=\"26\" height=\"14\" viewBox=\"0 0 26 14\">")
            .Append("<line x1=\"4\" y1=\"7\" x2=\"18\" y2=\"7\" stroke=\"#1b3c6e\" stroke-width=\"2.6\"/>")
            .Append("<line x1=\"18\" y1=\"2\" x2=\"18\" y2=\"12\" stroke=\"#1b3c6e\" stroke-width=\"2.6\"/>")
            .Append("</svg>Stop &amp; face the thrower</span>");
        return b.Append("</div>").ToString();
    }
}
