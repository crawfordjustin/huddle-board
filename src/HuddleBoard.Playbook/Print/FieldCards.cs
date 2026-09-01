using System.Text;

using static HuddleBoard.Playbook.Print.FieldDiagram;

namespace HuddleBoard.Playbook.Print;

/// <summary>
/// 7x5 inch field cards, two per Letter page. Cut them out, punch them, put
/// them on a ring, and hold one up in the huddle — at this age the picture
/// teaches faster than anything the coach says.
/// </summary>
internal static class FieldCards
{
    public static string Build()
    {
        var cards = new List<string> { SpotsCard(), ShapesCard() };
        cards.AddRange(PlayLibrary.All.Select(PlayCard));

        var sheets = new StringBuilder();
        for (var i = 0; i < cards.Count; i += 2)
        {
            sheets.Append("<section class=\"sheet\">")
                .Append(string.Concat(cards.Skip(i).Take(2)))
                .Append("</section>");
        }

        return "<!doctype html><html><head><meta charset=\"utf-8\"><title>Field Cards</title>"
            + "<style>" + Css + "</style></head><body>" + sheets + "</body></html>";
    }

    private static string PlayCard(Play play)
    {
        var colour = CategoryColours[play.Category];
        var txt = PlayTexts.All[play.Num];
        var calls = string.Concat(txt.Calls.Select(c =>
            $"<li><b>{Esc(c.Label)}</b><span>{Esc(c.Job)}</span></li>"));

        return $"""

            <div class="card" style="--accent:{colour}">
              <div class="chead">
                <div class="cnum">{play.Num}</div>
                <div class="cname"><h2>{Esc(play.Name)}</h2>
                  <p>{Esc(play.Formation)}</p></div>
                <div class="ccat">{Esc(play.Category)}</div>
              </div>
              <div class="cbody">
                <div class="cdiag">{PlaySvg(play)}</div>
                <div class="ccalls">
                  <ul>{calls}</ul>
                  <div class="cdef">Anyone with no job: <b>GO</b></div>
                </div>
              </div>
            </div>
            """;
    }

    private static string SpotsCard()
    {
        var rows = string.Concat(Spots.Glossary.Select(s =>
            $"<li><b>{Esc(s.Tag)}</b><span>{Esc(s.Name)}</span></li>"));

        return $"""

            <div class="card ref" style="--accent:#1f2933">
              <div class="chead">
                <div class="cnum">&#9679;</div>
                <div class="cname"><h2>THE SPOTS</h2><p>Point and say the name</p></div>
                <div class="ccat">REFERENCE</div>
              </div>
              <div class="spotsbody">
                <div class="spotsdiag">{SpotsSvg()}</div>
                <ul class="tight">{rows}</ul>
              </div>
            </div>
            """;
    }

    private static string ShapesCard()
    {
        var cells = string.Concat(Spots.Shapes.Select(s =>
            $"<div>{ShapeSvg(s.Pts, s.End)}<b>{Esc(s.Name)}</b></div>"));

        return $"""

            <div class="card ref" style="--accent:#1f2933">
              <div class="chead">
                <div class="cnum">&#9679;</div>
                <div class="cname"><h2>THE SHAPES</h2><p>Spot first, then shape, then number</p></div>
                <div class="ccat">REFERENCE</div>
              </div>
              <div class="shapesrow">{cells}</div>
              <div class="cdef wide"><b>Default rule:</b> {Esc(Spots.DefaultRule)}</div>
            </div>
            """;
    }

    private const string Css = """

        @page { size: Letter portrait; margin: 0; }
        * { box-sizing: border-box; }
        body { margin:0; font-family:"Helvetica Neue", Helvetica, Arial, sans-serif; color:#1f2933;
               -webkit-print-color-adjust:exact; print-color-adjust:exact; }
        .sheet { width:8.5in; height:11in; padding:0.5in 0.75in; page-break-after:always;
                 display:flex; flex-direction:column; justify-content:center; gap:0.5in; }
        .sheet:last-child { page-break-after:auto; }
        .card { width:7in; height:5in; border:1.5px dashed #b9c2cb; border-radius:10px;
                padding:0.22in 0.26in; display:flex; flex-direction:column; overflow:hidden; }
        .chead { display:flex; align-items:center; gap:11px; border-bottom:3px solid var(--accent);
                 padding-bottom:8px; flex:none; }
        .cnum { background:var(--accent); color:#fff; font-size:20pt; font-weight:800; width:46px;
                height:46px; border-radius:9px; display:flex; align-items:center; justify-content:center;
                flex:none; }
        .cname { flex:1; }
        .cname h2 { font-size:24pt; margin:0; letter-spacing:-.015em; line-height:1; }
        .cname p { font-size:8.4pt; color:#7b8794; margin:4px 0 0; letter-spacing:.12em;
                   font-weight:700; }
        .ccat { font-size:7pt; letter-spacing:.11em; font-weight:800; color:#fff; background:var(--accent);
                padding:4px 9px; border-radius:4px; flex:none; }
        .cbody { display:flex; gap:14px; flex:1; min-height:0; padding-top:10px; }
        .cdiag { flex:1.95; min-width:0; display:flex; align-items:center; }
        .cdiag svg { width:100%; height:auto; max-height:100%; display:block; }
        .ccalls { flex:1; min-width:0; display:flex; flex-direction:column; }
        .ccalls ul { list-style:none; margin:0; padding:0; flex:1; }
        .ccalls li { margin-bottom:11px; }
        .ccalls b { display:block; font-size:10.5pt; letter-spacing:.04em; color:#1f2933; }
        .ccalls span { display:block; font-size:10.5pt; color:#3e4c59; line-height:1.25; }
        .spotsbody { flex:1; min-height:0; display:flex; flex-direction:column; padding-top:10px; }
        .spotsdiag { flex:1; min-height:0; display:flex; align-items:center; justify-content:center; }
        .spotsdiag svg { width:100%; height:auto; max-height:100%; }
        ul.tight { list-style:none; margin:9px 0 0; padding:0; display:grid;
                   grid-template-columns:repeat(3,1fr); gap:5px 14px; flex:none; }
        ul.tight li { display:flex; gap:7px; align-items:baseline; }
        ul.tight b { font-size:8pt; color:#fff; background:#1f2933; border-radius:3px;
                     padding:2px 5px; min-width:26px; text-align:center; flex:none; }
        ul.tight span { font-size:9.4pt; font-weight:700; }
        .cdef { border-top:1px solid #e4e8ec; padding-top:7px; font-size:8.6pt; color:#7b8794; flex:none; }
        .cdef b { color:#127a4d; font-weight:800; }
        .cdef.wide { margin-top:8px; font-size:9.4pt; color:#3e4c59; line-height:1.4; }
        .shapesrow { display:grid; grid-template-columns:repeat(5,1fr); gap:14px 10px; padding-top:10px;
                     flex:1; align-content:center; }
        .shapesrow div { text-align:center; }
        .shapesrow svg { height:58px; width:auto; max-width:100%; display:block; margin:0 auto 3px; }
        .shapesrow b { font-size:9.2pt; letter-spacing:.03em; }

        """;
}
