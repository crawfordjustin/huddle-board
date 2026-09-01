using System.Text;

using static HuddleBoard.Playbook.Print.FieldDiagram;

namespace HuddleBoard.Playbook.Print;

/// <summary>
/// The paper playbook: cover, how the system works, calling a play, the
/// formations, one page per play, and a game-plan sheet. Rendered to HTML here
/// and printed to PDF by <c>PrintPipeline</c>.
/// </summary>
internal static class PlaybookDocument
{
    public static string Build()
    {
        var body = new StringBuilder();
        body.Append(Cover()).Append(SystemPages()).Append(BasicsPage()).Append(FormationsPage());
        foreach (var p in PlayLibrary.All)
            body.Append(PlayPage(p));
        body.Append(CallSheet());

        return "<!doctype html><html><head><meta charset=\"utf-8\">"
            + "<title>8U Flag Football Playbook</title><style>" + Css + "</style></head>"
            + "<body>" + body + "</body></html>";
    }

    private static string PlayPage(Play play)
    {
        var colour = CategoryColours[play.Category];
        var txt = PlayTexts.All[play.Num];
        var rows = string.Concat(txt.Assign.Select(a => $"<li><b>{Esc(a.Who)}</b>{Esc(a.Text)}</li>"));
        var notes = string.Concat(txt.Notes.Select(n => $"<li>{Esc(n)}</li>"));
        var callrow = string.Concat(txt.Calls.Select(c => $"<span><b>{Esc(c.Label)}</b>{Esc(c.Job)}</span>"));

        return $"""

            <section class="page play">
              <header class="playhead" style="--accent:{colour}">
                <div class="pnum">{play.Num}</div>
                <div class="ptitle">
                  <h2>{Esc(play.Name)}</h2>
                  <p>{Esc(play.Tagline)}</p>
                </div>
                <div class="ptags">
                  <span class="tag cat">{Esc(play.Category)}</span>
                  <span class="tag form">{Esc(play.Formation)}</span>
                </div>
              </header>
              <div class="callstrip">{callrow}</div>
              <div class="diagwrap">{PlaySvg(play)}</div>
              {Legend()}
              <div class="cols">
                <div class="assign">
                  <h3>Who does what</h3>
                  <ul class="jobs">{rows}</ul>
                </div>
                <div class="coach">
                  <h3>Coaching it</h3>
                  <ul>{notes}</ul>
                </div>
              </div>
              <div class="mistake" style="--accent:{colour}">
                <b>Watch for</b><span>{Esc(txt.Mistake)}</span>
              </div>
              <footer class="pf">8U Flag Football Playbook &middot; 6-on-6 &middot; Play {play.Num} of {PlayLibrary.All.Count}</footer>
            </section>
            """;
    }

    private static string Cover()
    {
        var blocks = new StringBuilder();
        foreach (var group in PlayLibrary.All.GroupBy(p => p.Category))
        {
            var lis = string.Concat(group.Select(p =>
                $"<li><b>{p.Num}</b> {Esc(p.Name)}<span>{Esc(p.Formation)}</span></li>"));
            blocks.Append($"<div class=\"tocblock\"><h4 style=\"--accent:{CategoryColours[group.Key]}\">")
                .Append($"{Esc(group.Key)}</h4><ul>{lis}</ul></div>");
        }

        return $"""

            <section class="page cover">
              <div class="coverhero">
                <p class="kicker">6-ON-6 &middot; AGES 8 &amp; UNDER</p>
                <h1>Flag Football<br>Playbook</h1>
                <p class="sub">14 plays &middot; 4 formations &middot; every job named by a spot on the field,
                so any six kids can run any play and everybody gets their snaps</p>
              </div>
              <div class="coverrules">
                <h3>Built around these league rules</h3>
                <div class="rulegrid">
                  <div><b>6 on 6, and everybody rotates</b><span>Nobody is X or Y. Jobs belong to spots &mdash;
                  SNAPPER, THROWER, WIDE, SLOT, TIGHT, BACK &mdash; and you fill them from whoever is in the
                  game.</span></div>
                  <div><b>Rusher starts 7 yards back</b><span>You have roughly two to three seconds of clean
                  pocket. Quick-game plays get the ball out before the rusher arrives.</span></div>
                  <div><b>No blocking or screening</b><span>Nobody blocks and nobody sets a pick. Receivers
                  create space by <i>running away</i> from each other, never into each other.</span></div>
                  <div><b>Run and no-run zones</b><span>Plays 1&ndash;4 are run-zone calls. Plays 12&ndash;14 are
                  designed for no-run zones and the goal line, where you must throw it.</span></div>
                </div>
              </div>
              <div class="starthere">
                <h3>Start here &mdash; four plays for game one</h3>
                <div class="startgrid">
                  <div><span class="sn">1</span><b>22 Dive</b><i>Your run</i></div>
                  <div><span class="sn">5</span><b>Double Slant</b><i>Your quick pass</i></div>
                  <div><span class="sn">7</span><b>Stick</b><i>Your third down</i></div>
                  <div><span class="sn">13</span><b>Pylon Fade</b><i>Your goal line</i></div>
                </div>
                <p>Master these four before you add anything else. A team that runs four plays well beats a team
                that runs fourteen plays badly &mdash; and at 8U it is not close. Two companion sheets go with
                this book: a deck of field cards to hold up in the huddle, and a rotation sheet so playing time
                is decided before kickoff instead of during a close game.</p>
              </div>
              <div class="toc">{blocks}</div>
              <footer class="pf">Print double-sided, three-hole punch, and keep it on the sideline.</footer>
            </section>
            """;
    }

    private static string SystemPages()
    {
        var glos = string.Concat(Spots.Glossary.Select(s =>
            $"<tr><td class=\"tagcell\">{Esc(s.Tag)}</td><td class=\"spotname\">{Esc(s.Name)}</td>"
            + $"<td>{Esc(s.Where)}</td></tr>"));
        var shapes = string.Concat(Spots.Shapes.Select(s =>
            $"<div class=\"shapecard\">{ShapeSvg(s.Pts, s.End)}<b>{Esc(s.Name)}</b>"
            + $"<span>{Esc(s.Teach)}</span></div>"));

        return $"""

            <section class="page">
              <header class="secthead"><h2>How this playbook works</h2>
                <p>Nobody in here is a &ldquo;receiver&rdquo; or a &ldquo;running back.&rdquo; Every job belongs
                to a <b>spot on the field</b>, and you put whichever six kids are in the game into those spots.
                That is what lets you rotate freely without re-teaching anything.</p></header>

              <h3>The spots</h3>
              <div class="spotwrap">
                <div class="spotdiag">{SpotsSvg()}
                  <p class="cap">All nine spots shown at once. Only six are on the field at a time &mdash; each
                  formation uses the SNAPPER, the THROWER, and four of the rest.</p></div>
                <table class="glos">{glos}</table>
              </div>

              <h3>The shapes &mdash; this is the whole route tree</h3>
              <div class="shapegrid">{shapes}</div>
              <p class="body">Every one of the 14 plays is just these shapes handed out to spots. A shape with a
              number after it means the depth: <b>OUT at 5</b> means run 5 yards, then break to the sideline.</p>

              <div class="defaultbox">
                <b>The default rule</b>
                <span>{Esc(Spots.DefaultRule)}</span>
                <i>This is the one that saves you. It means you only have to tell the one or two kids who
                actually matter on that play &mdash; everybody else already knows what to do.</i>
              </div>
              <footer class="pf">8U Flag Football Playbook &middot; 6-on-6</footer>
            </section>

            <section class="page">
              <header class="secthead"><h2>Calling a play on the field</h2>
                <p>You are allowed out there with them &mdash; use it. The goal is to get six rotating kids
                lined up and moving in under fifteen seconds, without anybody having to remember what letter
                they are.</p></header>
              <div class="two">
                <div>
                  <h3>The four-step huddle</h3>
                  <ol class="steps">
                    <li><b>Name the play, hold up the card.</b> Say the number and the name once
                      (&ldquo;Twelve &mdash; Spacing&rdquo;) and show them the picture. At this age the picture
                      teaches faster than any words you will say.</li>
                    <li><b>Point each kid to a spot.</b> Tap a shoulder, say the spot: &ldquo;Wide left.&rdquo;
                      &ldquo;Slot right.&rdquo; &ldquo;You're snapping.&rdquo; Two words each, six kids,
                      five seconds.</li>
                    <li><b>Give jobs only to the kids who have one.</b> On most plays that is one or two of
                      them. Everybody else falls under the default rule and runs GO.</li>
                    <li><b>Walk to the ball with them and stand where the play goes.</b> If it is a run, stand
                      in the hole. If it is a pass, stand where the ball is going. They will look at you, and
                      that is fine &mdash; it is the fastest coaching you will ever do.</li>
                  </ol>
                  <h3>Say it the same way every time</h3>
                  <p class="body">Spot first, then shape, then number: <b>&ldquo;Slot right &mdash; out at
                  five.&rdquo;</b> Never vary the order. Kids this age lock onto the pattern long before they
                  understand the football.</p>
                </div>
                <div>
                  <h3>Why not wristbands?</h3>
                  <p class="body">Wristbands work at 12U and up, but at 8U they add a reading step under
                  pressure and they still do not tell a kid where to stand. A picture card in your hand does
                  both jobs at once and never gets lost in a sleeve.</p>
                  <h3>Put the newest kids in the safest spots</h3>
                  <table class="script">
                    <tr><th>Spot</th><th>How hard is it?</th></tr>
                    <tr><td>WIDE LEFT / WIDE RIGHT</td><td>Easiest. Usually just &ldquo;GO.&rdquo;
                      Perfect first spot.</td></tr>
                    <tr><td>BACK</td><td>Easy on runs, and it is where the ball touches are. Great for
                      getting a quiet kid involved.</td></tr>
                    <tr><td>SLOT / TIGHT</td><td>Medium. Real route depths and inside traffic.</td></tr>
                    <tr><td>SNAPPER</td><td>Harder than it looks. A bad snap kills the play &mdash; train
                      three of them.</td></tr>
                    <tr><td>THROWER</td><td>Hardest. Train two or three, rotate them by series, not
                      by play.</td></tr>
                  </table>
                  <h3>The one thing to protect</h3>
                  <p class="body">Rotation is about playing time, but kids measure fairness in <b>ball
                  touches</b>, not snaps. Track carries and targets, not just series. The rotation sheet has a
                  column for exactly that.</p>
                </div>
              </div>
              <div class="sayingbox">
                <h3>Five things worth saying out loud</h3>
                <div class="sayings">
                  <div><b>&ldquo;Find your spot, not your friend.&rdquo;</b><span>The single most common 8U
                    alignment error is copying the kid next to you.</span></div>
                  <div><b>&ldquo;You don't have a job &mdash; so GO.&rdquo;</b><span>Reinforces the default rule
                    every single time it comes up.</span></div>
                  <div><b>&ldquo;Run to the number.&rdquo;</b><span>Depth is the thing that breaks first when a
                    new kid rotates in.</span></div>
                  <div><b>&ldquo;Turn around and find the ball.&rdquo;</b><span>Say it as they break the huddle,
                    not after the incompletion.</span></div>
                  <div><b>&ldquo;Great route&rdquo; &mdash; to the kid who didn't get the ball.</b><span>This is
                    how you keep nine kids running hard for a ball that goes to one of them.</span></div>
                </div>
              </div>
              <footer class="pf">8U Flag Football Playbook &middot; 6-on-6</footer>
            </section>
            """;
    }

    private static string BasicsPage() => """

        <section class="page">
          <header class="secthead"><h2>Before the first play</h2></header>
          <div class="two">
            <div>
              <h3>Rotating without losing the plot</h3>
              <p class="body">With 11&ndash;14 kids you are subbing roughly half the field every series.
              Two things make that painless: <b>rotate by series, not by play</b> (a kid who changes spots
              mid-drive is a kid who lines up wrong), and <b>fill the sheet in before kickoff</b> so you are
              never making fairness decisions while losing.</p>
              <p class="body">Keep the THROWER and SNAPPER on their own rotation &mdash; train two or three
              at each and swap them by series. Everything else can be genuinely random.</p>
              <h3>Five rules the kids can remember</h3>
              <ol class="bigrules">
                <li><b>Run your route all the way.</b> The ball may not come to you, but your route is what
                  gets someone else open.</li>
                <li><b>Turn and find the ball.</b> When you stop, face the quarterback with your hands up.</li>
                <li><b>Never run into a teammate.</b> No screening is allowed, and it also gets you both
                  covered by one defender.</li>
                <li><b>Two seconds.</b> Quarterback, if nobody is open in two seconds, throw it away toward
                  the sideline. A punt beats an interception.</li>
                <li><b>Protect your flags.</b> Ball carriers: no spinning, no stiff-arming, no guarding the
                  flag with your hand. Run to open grass.</li>
              </ol>
              <h3>A 60-minute practice</h3>
              <table class="script">
                <tr><th>Time</th><th>What you do</th></tr>
                <tr><td>0:00 &ndash; 0:10</td><td>Warm up, then snap-and-catch in pairs</td></tr>
                <tr><td>0:10 &ndash; 0:20</td><td>Flag pulling: two lines, live one-on-one</td></tr>
                <tr><td>0:20 &ndash; 0:35</td><td>Routes on cones at 3, 5, and 8 yards. No defense</td></tr>
                <tr><td>0:35 &ndash; 0:50</td><td>Run this week's plays against air, then vs. a walk-through defense</td></tr>
                <tr><td>0:50 &ndash; 1:00</td><td>Live reps of the four game-day calls, then finish on a completion</td></tr>
              </table>
            </div>
            <div>
              <h3>How to call a game</h3>
              <p class="body">Do not try to use all fourteen plays. Pick four for the first game: one run
              (<b>22 Dive</b>), one quick pass (<b>Double Slant</b>), one out (<b>Stick</b>), and one goal-line
              call (<b>Pylon Fade</b>). Add a play a week once those four are automatic.</p>
              <h3>A simple script that works</h3>
              <table class="script">
                <tr><th>Situation</th><th>Call</th></tr>
                <tr><td>1st down, run zone</td><td>22 Dive (1) or Jet Sweep (2)</td></tr>
                <tr><td>2nd and short</td><td>Stick (7) or Pitch Right (4)</td></tr>
                <tr><td>2nd and long</td><td>Flood (9)</td></tr>
                <tr><td>3rd and short</td><td>Stick (7) &mdash; set the break past the sticks</td></tr>
                <tr><td>3rd and long</td><td>Spacing (12) or Smash (6)</td></tr>
                <tr><td>Rusher is winning</td><td>Double Slant (5)</td></tr>
                <tr><td>Defense chases motion</td><td>Counter Keep (3)</td></tr>
                <tr><td>Defense crowds the middle</td><td>Pitch Right (4) or Post / Wheel (10)</td></tr>
                <tr><td>Run has been working</td><td>Play-Action Cross (11)</td></tr>
                <tr><td>No-run zone</td><td>Spacing (12)</td></tr>
                <tr><td>Inside the 5</td><td>Pylon Fade (13)</td></tr>
                <tr><td>Extra point</td><td>Triple Out (14)</td></tr>
                <tr><td>Need a change-up</td><td>Snapper Delay (8) &mdash; once per game</td></tr>
              </table>
              <h3>Practice this, not that</h3>
              <p class="body">Fifteen minutes of snap-and-catch beats an hour of new plays. The three drills
              that move the needle at this age: <b>snap to thrower</b> (50 reps), <b>route depth on a marked
              line</b> (cones at 3, 5, and 8 yards), and <b>catch and turn upfield</b>. Everything in this
              book fails if the snap is bad or the route is the wrong depth.</p>
              <p class="body small">One more thing: read your own league's rulebook before the first game.
              Rules on thrower runs, snapper eligibility, motion, and zone sizes vary between leagues, and a few
              plays here have notes about exactly that.</p>
            </div>
          </div>
          <footer class="pf">8U Flag Football Playbook &middot; 6-on-6</footer>
        </section>
        """;

    private static string FormationsPage()
    {
        string[] order = ["QB", "C", "X", "Y", "Z", "H"];
        var cards = new StringBuilder();
        foreach (var name in Formations.All.Keys)
        {
            var chips = string.Concat(order
                .Where(k => Formations.All[name].ContainsKey(k))
                .Select(k => $"<i>{Esc(Spots.Map[name][k].Name)}</i>"));
            cards.Append($"<div class=\"fcard\"><h4>{Esc(name)}</h4>{FormationSvg(name)}")
                .Append($"<div class=\"chips\">{chips}</div>")
                .Append($"<p>{Esc(Formations.Notes[name])}</p></div>");
        }

        return $"""

            <section class="page">
              <header class="secthead"><h2>The four formations</h2>
                <p>Everything in this playbook comes out of these four looks. Each one uses the SNAPPER, the
                THROWER, and four of the other spots &mdash; the spots each formation needs are listed under
                its picture. Teach the formations first: kids who line up correctly are already halfway to
                running the play right.</p></header>
              <div class="fgrid">{cards}</div>
              <div class="two mirrorbox">
                <div>
                  <h3>Every formation flips</h3>
                  <p class="body">Each of these four looks has a mirror image, and so does every play in this book.
                  Call &ldquo;Trips <b>Right</b> &mdash; Flood&rdquo; and the whole thing runs to the other side of
                  the field. That turns 14 plays into 28 without teaching anything new.</p>
                  <p class="body">Flip toward the wide side of the field, or away from the defense's best player.
                  At 8U, one kid is usually the entire defense &mdash; find him and run away from him.</p>
                </div>
                <div>
                  <h3>Getting lined up</h3>
                  <ul class="bigrules">
                    <li><b>Line up fast.</b> Sprint to your spot. Time spent standing around is time the defense
                      uses to figure you out.</li>
                    <li><b>Know your spot by name, not by neighbor.</b> If a teammate lines up wrong, do not follow
                      him.</li>
                    <li><b>Only one player moves before the snap.</b> Everyone else must be set and still.</li>
                    <li><b>Check the sideline.</b> Receivers on the outside need at least a couple of yards of
                      room to run their route without stepping out.</li>
                  </ul>
                </div>
              </div>
              <footer class="pf">8U Flag Football Playbook &middot; 6-on-6</footer>
            </section>
            """;
    }

    private static string CallSheet()
    {
        var rows = string.Concat(PlayLibrary.All.Select(p =>
            $"<tr><td class=\"n\" style=\"--accent:{CategoryColours[p.Category]}\">{p.Num}</td>"
            + $"<td class=\"nm\">{Esc(p.Name)}</td>"
            + $"<td>{Esc(p.Formation)}</td>"
            + $"<td>{Esc(p.Category)}</td>"
            + $"<td class=\"tl\">{Esc(p.Tagline)}</td></tr>"));

        return $"""

            <section class="page">
              <header class="secthead"><h2>Game plan sheet</h2>
                <p>The field cards are what you carry in the huddle &mdash; this page is for planning. Before
                the game, circle the five or six plays you actually intend to call, and pull only those cards
                off the ring. A short deck you can find fast beats a complete one you have to search.</p></header>
              <table class="sheet">
                <tr><th>#</th><th>Play</th><th>Formation</th><th>Type</th><th>What it is</th></tr>
                {rows}
              </table>
              <div class="notesbox"><h4>Game notes</h4><div class="lines"></div></div>
              <footer class="pf">8U Flag Football Playbook &middot; 6-on-6</footer>
            </section>
            """;
    }

    private const string Css = """

        @page { size: Letter; margin: 0; }
        * { box-sizing: border-box; }
        body { margin:0; font-family: "Helvetica Neue", Helvetica, Arial, sans-serif;
               color:#1f2933; -webkit-print-color-adjust:exact; print-color-adjust:exact; }
        .page { width:8.5in; height:11in; padding:0.46in 0.5in 0.4in; page-break-after:always;
                position:relative; overflow:hidden; display:flex; flex-direction:column; }
        .page:last-child { page-break-after:auto; }
        .pf { position:absolute; bottom:0.26in; left:0.5in; right:0.5in; font-size:7.6pt; color:#9aa5b1;
              border-top:1px solid #e4e8ec; padding-top:5px; letter-spacing:.02em; }

        /* cover */
        .cover { background:#0f1b2b; color:#fff; }
        .cover .pf { color:#5b6b80; border-top-color:#22344a; }
        .coverhero { padding-top:0.35in; }
        .kicker { font-size:10pt; letter-spacing:.22em; color:#5fbf8f; font-weight:800; margin:0 0 12px; }
        .cover h1 { font-size:46pt; line-height:1.02; margin:0 0 14px; letter-spacing:-.02em; }
        .cover .sub { font-size:11.5pt; color:#a8b6c6; margin:0; max-width:5.6in; line-height:1.5; }
        .coverrules { margin-top:auto; border-top:1px solid #22344a; padding-top:14px; }
        .coverrules h3 { font-size:9pt; letter-spacing:.16em; color:#5fbf8f; margin:0 0 12px;
                         text-transform:uppercase; }
        .rulegrid { display:grid; grid-template-columns:1fr 1fr; gap:12px 22px; }
        .rulegrid div { font-size:8.8pt; line-height:1.45; color:#a8b6c6; }
        .rulegrid b { display:block; color:#fff; font-size:10pt; margin-bottom:3px; }
        .starthere { margin-top:auto; border-top:1px solid #22344a; padding-top:14px; }
        .starthere h3 { font-size:9pt; letter-spacing:.16em; color:#5fbf8f; margin:0 0 12px;
                        text-transform:uppercase; }
        .startgrid { display:grid; grid-template-columns:repeat(4,1fr); gap:10px; }
        .startgrid div { background:#17263a; border-radius:8px; padding:11px 12px; }
        .startgrid .sn { display:inline-block; background:#5fbf8f; color:#0f1b2b; font-weight:800;
                         font-size:8.5pt; width:19px; height:19px; line-height:19px; text-align:center;
                         border-radius:5px; margin-bottom:7px; }
        .startgrid b { display:block; color:#fff; font-size:11pt; }
        .startgrid i { display:block; color:#7d90a6; font-size:8pt; font-style:normal; margin-top:2px; }
        .starthere p { font-size:8.8pt; color:#a8b6c6; line-height:1.5; margin:12px 0 0; max-width:6.2in; }
        .toc { margin-top:auto; padding-top:16px; display:grid; grid-template-columns:repeat(5,1fr);
               gap:14px; border-top:1px solid #22344a; }
        .tocblock h4 { font-size:7.6pt; letter-spacing:.1em; margin:0 0 8px; color:var(--accent);
                       text-transform:uppercase; border-bottom:2px solid var(--accent); padding-bottom:5px; }
        .tocblock ul { list-style:none; margin:0; padding:0; }
        .tocblock li { font-size:8.4pt; color:#dbe3ec; margin-bottom:6px; line-height:1.25; }
        .tocblock li b { color:#5fbf8f; margin-right:4px; }
        .tocblock li span { display:block; font-size:6.8pt; color:#6d7f93; letter-spacing:.04em; }

        /* section pages */
        .secthead { border-bottom:2.5px solid #1f2933; padding-bottom:9px; margin-bottom:16px; }
        .secthead h2 { font-size:21pt; margin:0; letter-spacing:-.01em; }
        .secthead p { font-size:9.2pt; color:#616e7c; margin:7px 0 0; max-width:6.4in; line-height:1.45; }
        h3 { font-size:8.4pt; letter-spacing:.13em; text-transform:uppercase; color:#127a4d;
             margin:16px 0 8px; }
        h3:first-child { margin-top:0; }
        .two { display:grid; grid-template-columns:1fr 1fr; gap:26px; }
        .body { font-size:9pt; line-height:1.52; color:#3e4c59; margin:0 0 9px; }
        .body.small { font-size:8.2pt; color:#616e7c; }
        table { width:100%; border-collapse:collapse; }
        .spots td, .script td, .script th { font-size:8.6pt; padding:5px 7px; border-bottom:1px solid #e4e8ec;
             line-height:1.4; vertical-align:top; }
        .script th { text-align:left; font-size:7.4pt; letter-spacing:.1em; text-transform:uppercase;
             color:#7b8794; border-bottom:2px solid #1f2933; }
        .script td:first-child { color:#616e7c; width:44%; }
        .script td:last-child { font-weight:600; }
        td.who { font-weight:800; color:#127a4d; width:52px; white-space:nowrap; }
        .bigrules { margin:0; padding-left:17px; }
        .bigrules li { font-size:8.8pt; line-height:1.45; margin-bottom:7px; color:#3e4c59; }
        .bigrules b { color:#1f2933; }

        /* formations */
        .fgrid { display:grid; grid-template-columns:1fr 1fr; gap:14px 20px; }
        .fcard { border:1px solid #e4e8ec; border-radius:9px; padding:10px 12px 12px; }
        .fcard h4 { font-size:10.5pt; margin:0 0 6px; letter-spacing:.02em; }
        .fcard svg { width:100%; height:auto; display:block; }
        .chips { display:flex; flex-wrap:wrap; gap:4px; margin:8px 0 0; }
        .chips i { font-style:normal; font-size:6.9pt; letter-spacing:.05em; font-weight:700;
                   background:#eef2f6; color:#3e4c59; border-radius:4px; padding:3px 6px; }
        .fcard p { font-size:8pt; line-height:1.42; color:#616e7c; margin:6px 0 0; }

        .mirrorbox { margin-top:auto; margin-bottom:0.36in; border-top:1px solid #e4e8ec;
                     padding-top:14px; gap:26px; }

        /* play pages */
        .playhead { display:flex; align-items:flex-start; gap:13px;
                    border-bottom:3px solid var(--accent); padding-bottom:10px; }
        .pnum { background:var(--accent); color:#fff; font-size:19pt; font-weight:800; width:44px;
                height:44px; border-radius:9px; display:flex; align-items:center; justify-content:center;
                flex:none; }
        .ptitle { flex:1; }
        .ptitle h2 { font-size:23pt; margin:0; letter-spacing:-.015em; line-height:1.05; }
        .ptitle p { font-size:9.4pt; color:#616e7c; margin:4px 0 0; }
        .ptags { text-align:right; flex:none; padding-top:2px; }
        .tag { display:block; font-size:7.2pt; letter-spacing:.1em; font-weight:800; padding:3px 8px;
               border-radius:4px; margin-bottom:4px; }
        .tag.cat { background:var(--accent); color:#fff; }
        .tag.form { background:#eef2f6; color:#52606d; }
        .callstrip { display:flex; flex-wrap:wrap; gap:6px; margin:11px 0 2px; }
        .callstrip span { flex:1 1 auto; background:#f2f5f7; border-radius:6px; padding:6px 10px;
                          font-size:8.6pt; color:#3e4c59; white-space:nowrap; }
        .callstrip b { display:block; font-size:7.2pt; letter-spacing:.09em; color:#127a4d;
                       margin-bottom:2px; }
        .diagwrap { margin:9px auto 0; width:5.5in; }
        .diagram { width:100%; height:auto; display:block; }
        .legend { display:flex; flex-wrap:wrap; gap:5px 14px; justify-content:center; margin:9px 0 11px;
                  font-size:7.4pt; color:#52606d; font-weight:600; }
        .lg { display:inline-flex; align-items:center; gap:5px; }
        .cols { display:grid; grid-template-columns:1.15fr 1fr; gap:20px; border-top:1px solid #e4e8ec;
                padding-top:12px; }
        .jobs { list-style:none; margin:0; padding:0; }
        .jobs li { font-size:8.4pt; line-height:1.42; color:#3e4c59; padding:0 0 7px;
                   margin-bottom:7px; border-bottom:1px solid #eef1f4; }
        .jobs li:last-child { border-bottom:0; }
        .jobs b { display:block; font-size:7.6pt; letter-spacing:.09em; color:#127a4d; margin-bottom:2px; }
        .coach ul { margin:0; padding-left:15px; }
        .coach li { font-size:8.4pt; line-height:1.45; color:#3e4c59; margin-bottom:8px; }

        .mistake { margin-top:auto; margin-bottom:0.34in; border-left:4px solid var(--accent);
                   background:#f7f9fa; border-radius:0 8px 8px 0; padding:11px 14px; display:flex;
                   gap:14px; align-items:baseline; }
        .mistake b { font-size:7.6pt; letter-spacing:.13em; text-transform:uppercase; color:var(--accent);
                     flex:none; }
        .mistake span { font-size:9pt; line-height:1.45; color:#3e4c59; }

        .spotwrap { display:grid; grid-template-columns:1.15fr 1fr; gap:20px; align-items:start; }
        .spotdiag svg { width:100%; height:auto; display:block; }
        .cap { font-size:7.8pt; color:#7b8794; line-height:1.4; margin:7px 0 0; }
        .glos td { font-size:8.4pt; padding:4px 6px; border-bottom:1px solid #eef1f4; line-height:1.35;
                   vertical-align:top; }
        .tagcell { font-weight:800; color:#fff; background:#1f2933; text-align:center; width:26px;
                   border-radius:4px; font-size:7.6pt; }
        .spotname { font-weight:800; white-space:nowrap; width:74px; }
        .shapegrid { display:grid; grid-template-columns:repeat(5,1fr); gap:10px; margin-bottom:10px; }
        .shapecard { border:1px solid #e4e8ec; border-radius:8px; padding:8px 7px 9px; text-align:center; }
        .shapesvg { height:64px; width:auto; max-width:100%; display:block; margin:0 auto 5px; }
        .shapecard b { display:block; font-size:9pt; letter-spacing:.04em; }
        .shapecard span { display:block; font-size:6.9pt; color:#7b8794; line-height:1.32; margin-top:3px; }
        .defaultbox { margin-top:auto; margin-bottom:0.36in; background:#0f1b2b; color:#fff;
                      border-radius:10px; padding:15px 18px; }
        .defaultbox b { display:block; font-size:8pt; letter-spacing:.15em; text-transform:uppercase;
                        color:#5fbf8f; margin-bottom:7px; }
        .defaultbox span { display:block; font-size:12pt; line-height:1.4; font-weight:600; }
        .defaultbox i { display:block; font-style:normal; font-size:8.6pt; color:#a8b6c6; margin-top:9px;
                        line-height:1.45; }
        .steps { margin:0; padding-left:17px; }
        .steps li { font-size:8.8pt; line-height:1.45; margin-bottom:9px; color:#3e4c59; }
        .steps b { color:#1f2933; }

        .sayingbox { margin-top:auto; margin-bottom:0.36in; border-top:1px solid #e4e8ec; padding-top:14px; }
        .sayings { display:grid; grid-template-columns:repeat(5,1fr); gap:12px; }
        .sayings div { font-size:7.8pt; line-height:1.4; }
        .sayings b { display:block; color:#1f2933; font-size:8.4pt; margin-bottom:4px; line-height:1.3; }
        .sayings span { color:#7b8794; }

        /* call sheet */
        .sheet td, .sheet th { font-size:8.6pt; padding:6px 7px; border-bottom:1px solid #e4e8ec;
                               text-align:left; vertical-align:middle; }
        .sheet th { font-size:7.4pt; letter-spacing:.1em; text-transform:uppercase; color:#7b8794;
                    border-bottom:2px solid #1f2933; }
        .sheet td.n { font-weight:800; color:#fff; background:var(--accent); text-align:center; width:26px;
                      border-radius:4px; }
        .sheet td.nm { font-weight:800; }
        .sheet td.tl { color:#616e7c; font-size:8pt; }
        .notesbox { margin-top:20px; margin-bottom:0.36in; border:1px solid #e4e8ec; border-radius:9px; padding:11px 13px 12px; flex:1; display:flex; flex-direction:column; }
        .notesbox h4 { font-size:7.6pt; letter-spacing:.12em; text-transform:uppercase; color:#7b8794;
                       margin:0 0 8px; }
        .lines { flex:1; background:repeating-linear-gradient(to bottom, transparent 0,
                 transparent 26px, #e4e8ec 26px, #e4e8ec 27px); }

        """;
}
