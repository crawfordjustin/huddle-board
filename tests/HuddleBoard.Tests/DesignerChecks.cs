using System.Text.Json;
using System.Text.Json.Nodes;

using HuddleBoard.Playbook;

using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>
/// The play designer: a coach draws a play on the tablet, the tablet's own
/// copy of the checker holds it to the same rules as a shipped one, and the
/// play then lives in the library, the deck and the sync file like any other —
/// through an update, through Start over, and onto an assistant's tablet.
/// </summary>
/// <remarks>
/// The drawing checks drive the real pointer over the real field, in yards,
/// so a finger landing a yard off is the same miss here as on a tablet. The
/// storage checks seed a design directly, because they are about what happens
/// to a play after it exists, not about drawing one.
/// </remarks>
public sealed class DesignerChecks(AppFixture app) : IDisposable
{
    private static readonly Viewport Desk = new("landscape 16:10", 1600, 1000);

    private readonly PrivateBuild _build = new();

    public void Dispose() => _build.Dispose();

    /// <summary>
    /// A design built in the page from a shipped formation, with any spots,
    /// routes and ball given — the same shape the designer saves.
    /// </summary>
    private const string Seed = """
        (o) => {
          const spots = {};
          for (const k of Object.keys(FORMATIONS[o.base]))
            spots[k] = [FORMATIONS[o.base][k].x, FORMATIONS[o.base][k].y];
          Object.assign(spots, o.spots || {});
          const d = saneDesign({id: o.id, base: o.base, spots, routes: o.routes || {},
                                ball: o.ball, coachName: o.name, created: o.created || 1});
          designs[d.id] = d; saveDesigns(); refreshPlays();
          if (!deck.includes(d.id)){ deck.push(d.id); saveDeck(); }
          return d.id;
        }
        """;

    private static object SlotIn(string id = "u_slotin01", string name = "SLOT IN") => new
    {
        id,
        @base = "SPREAD",
        name,
        routes = new Dictionary<string, object> { ["Y"] = new { pts = new[] { new[] { -5.0, 5 }, new[] { -1.0, 5 } } } },
        ball = new { mode = "pass", who = "Y" },
    };

    // ------------------------------------------------------------ pointer

    private static async Task<(float X, float Y)> ScreenAsync(IPage page, double x, double y)
    {
        var r = await page.EvaluateAsync<double[]>("""
            ([x, y]) => {
              const m = document.getElementById("field").getScreenCTM();
              const s = F2S(x, y), p = new DOMPoint(s[0], s[1]).matrixTransform(m);
              return [p.x, p.y];
            }
            """, new object[] { x, y });
        return ((float)r[0], (float)r[1]);
    }

    private static async Task TapAsync(IPage page, double x, double y)
    {
        var (sx, sy) = await ScreenAsync(page, x, y);
        await page.Mouse.ClickAsync(sx, sy);
        await page.WaitForTimeoutAsync(80);
    }

    private static async Task DragAsync(IPage page, (double X, double Y) from, (double X, double Y) to)
    {
        var a = await ScreenAsync(page, from.X, from.Y);
        var b = await ScreenAsync(page, to.X, to.Y);
        await page.Mouse.MoveAsync(a.X, a.Y);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(b.X, b.Y, new MouseMoveOptions { Steps = 10 });
        await page.Mouse.UpAsync();
        await page.WaitForTimeoutAsync(120);
    }

    /// <summary>
    /// Picks a kid and draws his route a leg at a time, the way a coach does:
    /// the ring on the end of the route, then where the leg ends.
    /// </summary>
    private static async Task RouteAsync(IPage page, (double X, double Y) kid, params (double X, double Y)[] legs)
    {
        await TapAsync(page, kid.X, kid.Y);
        var end = kid;
        foreach (var leg in legs)
        {
            await TapAsync(page, end.X, end.Y);
            await TapAsync(page, leg.X, leg.Y);
            end = leg;
        }
    }

    private static async Task ToolAsync(IPage page, string tool)
    {
        await page.ClickAsync($"[data-tool=\"{tool}\"]");
        await page.WaitForTimeoutAsync(80);
    }

    private async Task<(IPage Page, List<string> Errors)> OpenDesignerAsync(
        Viewport size, string formation, int players = 6)
    {
        var (page, errors) = await app.OpenAppAsync(size, players: players);
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.ClickAsync("#lnew");
        await page.ClickAsync($".fmbtn[data-fm=\"{formation}\"]");
        await page.WaitForSelectorAsync(".designer #field");
        await page.WaitForTimeoutAsync(300);
        return (page, errors);
    }

    private static Task<string> CallsAsync(IPage page) => page.InnerTextAsync("#dz-calls");

    private static Task<int> ErrorCountAsync(IPage page) =>
        page.EvaluateAsync<int>("DZ.findings.filter(f => f.level === 'error').length");

    // ------------------------------------------------------------ the checks

    [Fact]
    public async Task MoveHighlightsTheSelectedPlayerAndFollowsTheDrag()
    {
        var (page, errors) = await OpenDesignerAsync(Desk, "SPREAD");
        await ToolAsync(page, "move");
        await TapAsync(page, -9, 0);
        Assert.Equal(1, await page.Locator("#dz-over .dz-selected").CountAsync());
        await DragAsync(page, (-9, 0), (-8, -2));
        Assert.True(await page.EvaluateAsync<bool>("""
            (() => {
              const ring = document.querySelector('#dz-over .dz-selected');
              const spot = F2S(...DZ.d.spots[DZ.moveSel]);
              return ring && +ring.getAttribute('cx') === spot[0] && +ring.getAttribute('cy') === spot[1];
            })()
            """));
        await ToolAsync(page, "ball");
        Assert.Equal(0, await page.Locator("#dz-over .dz-selected").CountAsync());
        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Two copies of one rulebook are only safe while something proves they
    /// agree. Every finding PlayChecker makes on every format's shipped
    /// library, the tablet's checker makes too — no more, no fewer.
    /// </summary>
    [Fact]
    public async Task TheTabletsCheckerFindsWhatPlayCheckerFinds()
    {
        static string Level(Severity s) => s == Severity.Error ? "error" : "warning";
        var (page, errors) = await app.OpenAppAsync(Desk);

        foreach (var format in Formats.All)
        {
            var wanted = PlayChecker.Check(format.Plays, format)
                .Where(f => f.Num > 0 && f.Rule != "TEACHABLE/text")
                .Select(f => $"{f.Num}|{Level(f.Level)}|{f.Rule}")
                .Order(StringComparer.Ordinal)
                .ToList();

            var found = (await page.EvaluateAsync<string[]>($$"""
                ALL.formats[{{format.Players}}].plays.filter(p => !p.custom)
                  .flatMap(p => checkPlay(p).map(f => p.num + "|" + f.level + "|" + f.rule))
                """)).Order(StringComparer.Ordinal).ToList();

            // every shipped library has warnings, so an agreement on nothing cannot pass
            Assert.NotEmpty(wanted);
            Assert.Equal(wanted, found);
        }

        // and it is not agreeing by finding nothing: a real collision is caught
        var collide = await page.EvaluateAsync<string[]>("""
            (() => {
              const p = JSON.parse(JSON.stringify(DATA.plays.find(q => q.num === 15)));
              p.paths.find(q => q.who === "Y").pts = [[-10, 0], [-10, 5]];
              return checkPlay(p).filter(f => f.level === "error").map(f => f.rule);
            })()
            """);
        Assert.Contains("SAFE/collide", collide);

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public async Task ACoachDrawsAPassAndItLandsInTheDeck()
    {
        var (page, errors) = await OpenDesignerAsync(Desk, "SPREAD");

        // SLOT BLUE lines up at (-5, 0): tap him, then run five and break inside
        await RouteAsync(page, (-5, 0), (-5, 5), (-1, 5));
        Assert.Contains("IN at 5", await CallsAsync(page));
        Assert.True(await page.Locator("#dz-save").IsDisabledAsync(), "Save is open with nobody getting the ball");

        await ToolAsync(page, "ball");
        await TapAsync(page, -5, 0);
        Assert.Equal(0, await ErrorCountAsync(page));
        Assert.False(await page.Locator("#dz-save").IsDisabledAsync(), "Save is shut on a clean play");
        Assert.Equal("Y", await page.EvaluateAsync<string>("S.play.primary"));

        // the names come last, in the sheet
        await page.ClickAsync("#dz-save");
        await page.WaitForSelectorAsync("#dzsheet");
        await page.FillAsync("#dz-real", "Slot In");
        await page.FillAsync("#dz-fun", "Boomerang Two");
        await page.ClickAsync("#dz-cat [data-cat=\"QUICK GAME\"]");
        await page.ClickAsync("#dz-ok");
        await page.WaitForSelectorAsync(".lib");
        await page.WaitForTimeoutAsync(300);

        var id = await page.EvaluateAsync<string>("Object.keys(designs)[0]");
        Assert.StartsWith("u_", id);
        var row = $".lrow[data-id=\"{id}\"]";
        Assert.Equal("SLOT IN", (await page.InnerTextAsync(row + " .lname b")).Trim());
        Assert.Equal(1, await page.Locator(row + ".on").CountAsync());     // straight into the deck
        Assert.Equal(1, await page.Locator(row + " .lnum.mine").CountAsync());

        // on the deck, filed where the coach put it, saying it is his
        await page.ClickAsync("#done");
        await page.WaitForTimeoutAsync(400);
        var tile = $".tile[data-id=\"{id}\"]";
        Assert.Equal("QUICK GAME", (await page.InnerTextAsync(tile + " .situation")).Trim());
        Assert.Equal(1, await page.Locator(tile + " .catchip.mine").CountAsync());

        // and the play screen runs it like any other
        await page.ClickAsync(tile);
        await page.WaitForSelectorAsync("#calls");
        Assert.Contains("SLOT BLUE", await page.InnerTextAsync("#calls"));
        Assert.Contains("IN at 5", await page.InnerTextAsync("#calls"));
        await page.ClickAsync("#stage");
        await page.WaitForTimeoutAsync(1500);
        await page.ClickAsync("#mirror");
        await page.WaitForTimeoutAsync(800);
        Assert.Contains("SLOT ORANGE", await page.InnerTextAsync("#calls"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// The designer is the format's: it offers that format's four formations
    /// and nobody else's, a drawn play has that format's roster, and it is
    /// stored with that format's plays — so it is not in the other formats.
    /// </summary>
    [Theory]
    [InlineData(5, "EMPTY", 5.0, "H", "SLOT ORANGE")]
    [InlineData(7, "FIVE WIDE", -7.5, "F", "SLOT BLUE")]
    public async Task ACoachDrawsAPlayInEachFormat(int players, string formation, double x, string key, string name)
    {
        var format = Formats.All.Single(f => f.Players == players);
        var (page, errors) = await app.OpenAppAsync(Desk, players: players);
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.ClickAsync("#lnew");
        await page.WaitForSelectorAsync(".fmbtn");
        var offered = await page.EvalOnSelectorAllAsync<string[]>(".fmbtn", "b => b.map(x => x.dataset.fm)");
        Assert.Equal(format.Formations.Keys, offered);
        await page.ClickAsync($".fmbtn[data-fm=\"{formation}\"]");
        await page.WaitForSelectorAsync(".designer #field");
        await page.WaitForTimeoutAsync(300);

        // the slot runs five and breaks inside, and he gets the ball
        var inside = x > 0 ? x - 4 : x + 4;
        await RouteAsync(page, (x, 0), (x, 5), (inside, 5));
        Assert.Contains("IN at 5", await CallsAsync(page));
        await ToolAsync(page, "ball");
        await TapAsync(page, x, 0);
        Assert.Equal(0, await ErrorCountAsync(page));
        Assert.Equal(key, await page.EvaluateAsync<string>("S.play.primary"));
        Assert.Equal(players, await page.EvaluateAsync<int>("Object.keys(S.play.spots).length"));

        await page.ClickAsync("#dz-save");
        await page.WaitForSelectorAsync("#dzsheet");
        await page.FillAsync("#dz-real", "Slot In");
        await page.ClickAsync("#dz-ok");
        await page.WaitForSelectorAsync(".lib");
        await page.WaitForTimeoutAsync(300);

        var id = await page.EvaluateAsync<string>("Object.keys(designs)[0]");
        Assert.True(await page.EvaluateAsync<bool>($"deck.includes('{id}')"), "a new play did not go into the deck");
        Assert.Contains(name, await page.EvaluateAsync<string>(
            $"DATA.plays.find(p => p.id === '{id}').calls.flat().join(' ')"));

        // stored with this format's plays, and in no other format
        Assert.True(await page.EvaluateAsync<bool>(
            $"!!JSON.parse(localStorage.getItem('hb.plays.{players}')).byId['{id}']"));
        await page.EvaluateAsync("loadMode(6)");
        Assert.Equal(0, await page.EvaluateAsync<int>("customCount()"));
        Assert.False(await page.EvaluateAsync<bool>($"DATA.plays.some(p => p.id === '{id}')"));
        await page.EvaluateAsync($"loadMode({players})");
        Assert.Equal(1, await page.EvaluateAsync<int>("customCount()"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
    }

    /// <summary>
    /// A leg is drawn on purpose, one at a time: tap inside the dotted ring to
    /// start it, tap where it ends, and the kid is back to just picked with the
    /// ring on the new end. So a coach can pick somebody else without a leg
    /// chasing his pointer, and while a leg IS being drawn a tap near a
    /// teammate is still a point rather than a reselection.
    /// </summary>
    [Fact]
    public async Task ALegStartsOnlyFromTheDottedRing()
    {
        var (page, errors) = await OpenDesignerAsync(Desk, "SPREAD");
        Task<int> Points(string key) =>
            page.EvaluateAsync<int>($"DZ.d.routes.{key} ? DZ.d.routes.{key}.pts.length : 0");
        Task<bool> Armed() => page.EvaluateAsync<bool>("DZ.armed");
        async Task<bool> GhostShowing(double x, double y)
        {
            var (sx, sy) = await ScreenAsync(page, x, y);
            await page.Mouse.MoveAsync(sx, sy);
            return await page.EvaluateAsync<bool>(
                "(() => { const g = document.getElementById('dz-next'); return !!g && g.getAttribute('opacity') === '1'; })()");
        }

        await TapAsync(page, -5, 0);                               // pick SLOT BLUE
        Assert.Equal("Y", await page.EvaluateAsync<string>("DZ.sel"));
        Assert.False(await Armed(), "picking a kid started a leg");
        Assert.False(await GhostShowing(-3, 4), "a leg follows the pointer before one was started");

        await TapAsync(page, -5, 0);                               // inside his ring: start a leg
        Assert.True(await Armed());
        await TapAsync(page, -5, 0);                               // the ring again: never mind
        Assert.False(await Armed());

        await TapAsync(page, -5, 0);
        Assert.True(await GhostShowing(-3, 4), "no preview of the leg being drawn");
        await TapAsync(page, -5, 2);                               // where it ends
        Assert.Equal(1, await Points("Y"));
        Assert.False(await Armed(), "a leg was placed and the next one started on its own");

        await TapAsync(page, -5, 4);                               // not from the ring: nothing
        Assert.Equal(1, await Points("Y"));
        Assert.Contains("dotted circle", await page.InnerTextAsync("#dz-line"));

        await TapAsync(page, -5, 2);                               // the ring on the end of the leg
        Assert.True(await Armed());
        await TapAsync(page, -9, 1);                               // near WIDE BLUE, not on him: a point
        Assert.Equal(2, await Points("Y"));
        Assert.Equal("Y", await page.EvaluateAsync<string>("DZ.sel"));

        // with no leg started, a tap on a teammate just picks him, and nothing follows
        await TapAsync(page, 10, 0);
        Assert.Equal("Z", await page.EvaluateAsync<string>("DZ.sel"));
        Assert.Equal(2, await Points("Y"));
        Assert.False(await GhostShowing(6, 6));

        // a press in the ring that drags pulls the first leg straight out of him
        await DragAsync(page, (10, 0), (10, 4));
        Assert.Equal(new[] { 10.0, 4 }, await page.EvaluateAsync<double[]>("DZ.d.routes.Z.pts[0]"));
        Assert.False(await Armed());
        // ...and dragging the ring on the end moves the end, it does not add one
        await DragAsync(page, (10, 4), (11, 5));
        Assert.Equal(1, await Points("Z"));
        Assert.Equal(new[] { 11.0, 5 }, await page.EvaluateAsync<double[]>("DZ.d.routes.Z.pts[0]"));

        Assert.Equal(1, await page.Locator("#dz-over .dznext").CountAsync());

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A trick alignment: the kid keeps the name he was given in the huddle,
    /// takes his route with him, and is told where to stand.
    /// </summary>
    [Fact]
    public async Task AMovedKidKeepsHisNameAndHisRouteAndIsToldWhereToStand()
    {
        var (page, errors) = await OpenDesignerAsync(Desk, "TWINS RIGHT");

        // WIDE BLUE (-9, 0): six yards and out
        await RouteAsync(page, (-9, 0), (-9, 6), (-12, 6));
        Assert.Contains("OUT at 6", await CallsAsync(page));

        // ...then stacked two yards behind SLOT ORANGE (6, 0), on the other side
        await ToolAsync(page, "move");
        await DragAsync(page, (-9, 0), (6, -2));
        Assert.Equal(new[] { 6.0, -2 }, await page.EvaluateAsync<double[]>("DZ.d.spots.X"));
        // the route came with him, shape and all
        Assert.Equal(new[] { 6.0, 4 }, await page.EvaluateAsync<double[]>("DZ.d.routes.X.pts[0]"));

        var calls = await CallsAsync(page);
        Assert.Contains("WIDE BLUE", calls);
        Assert.Contains("LINE UP behind SLOT ORANGE, then OUT at 4", calls);
        Assert.Contains("SHIFTED", await page.InnerTextAsync("#dz-fm"));

        // the snapper stays on the ball whatever a finger does
        await DragAsync(page, (0, 0), (4, -2));
        Assert.Equal(new[] { 0.0, 0 }, await page.EvaluateAsync<double[]>("DZ.d.spots.C"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A coach never draws a handoff. The designer puts the exchange where the
    /// thrower and the carrier actually meet, so it passes the checker's
    /// timing rule by construction, and the ball rides the carrier from there.
    /// </summary>
    [Fact]
    public async Task ARunHandsOffWhereTheTwoKidsMeet()
    {
        var (page, errors) = await OpenDesignerAsync(Desk, "TWINS RIGHT");

        await ToolAsync(page, "ball");
        await page.ClickAsync("[data-mode=\"carry\"]");
        await TapAsync(page, -2.6, -4.4);                          // the BACK
        await ToolAsync(page, "route");
        await RouteAsync(page, (-2.6, -4.4), (1, -1), (2, 7));

        Assert.Equal(0, await ErrorCountAsync(page));
        var handoff = await page.EvaluateAsync<JsonElement>(
            "S.play.paths.find(p => p.type === 'handoff')");
        Assert.Equal("QB", handoff.GetProperty("who").GetString());
        Assert.Equal("H", handoff.GetProperty("to").GetString());
        Assert.Equal("carry", await page.EvaluateAsync<string>("S.play.ball.mode"));
        Assert.Contains("CARRY", await CallsAsync(page));
        Assert.Contains("hand it to BACK", await CallsAsync(page));

        // the preview is the real animation: the ball is in the back's hands mid-run
        await page.ClickAsync("#dz-prev");
        await page.WaitForTimeoutAsync(1600);
        Assert.True(await page.EvaluateAsync<bool>("S.stage === 'run' && +SC.ball.getAttribute('opacity') === 1"),
            "the preview never put the ball on the field");
        await page.ClickAsync("#dz-prev");

        // a run cannot be filed where the ball may not be run
        await page.ClickAsync("#dz-save");
        await page.WaitForSelectorAsync("#dzsheet");
        Assert.True(await page.Locator("#dz-cat [data-cat=\"NO-RUN ZONE\"]").IsDisabledAsync());

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A rollout: on a pass the thrower can be given a line, the ball rides
    /// it, and the throw leaves from where it ends. That end cannot be drawn
    /// past the line, and a rollout is a pass, so it can be filed under
    /// NO-RUN ZONE. On a handoff his line is the exchange and is not his to draw.
    /// </summary>
    [Fact]
    public async Task TheThrowerCanRollOutOnAPass()
    {
        var (page, errors) = await OpenDesignerAsync(Desk, "SPREAD");

        await ToolAsync(page, "ball");
        await TapAsync(page, -10, 0);                              // WIDE BLUE, deep
        await ToolAsync(page, "route");
        await RouteAsync(page, (0, -3), (-4, -4), (-7, -3));       // THROWER rolls to blue

        Assert.Equal(0, await ErrorCountAsync(page));
        Assert.Equal("run", await page.EvaluateAsync<string>("S.play.paths.find(p => p.who === 'QB').type"));
        Assert.Equal("QB", await page.EvaluateAsync<string>("S.play.ball.legs[0].who"));
        Assert.Equal(new[] { -7.0, -3 }, await page.EvaluateAsync<double[]>("throwArc(S.play).src"));
        var calls = await CallsAsync(page);
        Assert.Contains("THROWER", calls);
        Assert.Contains("roll to blue", calls);

        // past the line is a run, so a rollout's end stops at it
        await TapAsync(page, -7, -3);
        await TapAsync(page, -9, 3);
        Assert.Equal(new[] { -9.0, 0 }, await page.EvaluateAsync<double[]>("DZ.d.routes.QB.pts[2]"));
        Assert.Equal(0, await ErrorCountAsync(page));

        // the preview carries it in his hands before it flies
        await page.ClickAsync("#dz-prev");
        await page.WaitForTimeoutAsync(400);
        Assert.True(await page.EvaluateAsync<bool>("""
            (() => {
              const b = S.tl.ball, leg = b.legs[0], seg = S.tl.paths[leg.pathIdx];
              S.t0 = 0; frame(seg.start + seg.dur * 0.5);
              const m = /translate\(\s*(-?[\d.]+)\s+(-?[\d.]+)/.exec(SC.ball.getAttribute('transform'));
              const e = walk(S.play.paths[leg.pathIdx].pts, 0.5).end, s = F2S(e[0], e[1]);
              return +SC.ball.getAttribute('opacity') === 1 && Math.hypot(+m[1] - s[0], +m[2] - s[1]) < 3;
            })()
            """), "mid-roll the ball is not in the thrower's hands");
        await page.ClickAsync("#dz-prev");

        // a rollout is a pass: it can go in the no-run zone
        await page.ClickAsync("#dz-save");
        await page.WaitForSelectorAsync("#dzsheet");
        Assert.False(await page.Locator("#dz-cat [data-cat=\"NO-RUN ZONE\"]").IsDisabledAsync());
        await page.ClickAsync("#dz-cat [data-cat=\"NO-RUN ZONE\"]");
        await page.FillAsync("#dz-real", "Boot Blue");
        await page.ClickAsync("#dz-ok");
        await page.WaitForSelectorAsync(".lib");
        Assert.Equal(1, await page.EvaluateAsync<int>(
            "DATA.plays.filter(p => p.custom && p.category === 'NO-RUN ZONE' && !p.problems).length"));

        // on a handoff the thrower's line is the exchange, not his to draw
        await page.ClickAsync(".lrow[data-id^=\"u_\"] [data-ren]");
        await page.WaitForSelectorAsync(".designer #field");
        await ToolAsync(page, "ball");
        await page.ClickAsync("[data-mode=\"carry\"]");
        await TapAsync(page, 5, 0);                                // SLOT ORANGE carries
        await ToolAsync(page, "route");
        await TapAsync(page, 0, -3);
        Assert.NotEqual("QB", await page.EvaluateAsync<string>("DZ.sel || ''"));
        Assert.Contains("handoff", await page.InnerTextAsync("#dz-line"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// PITCH REVERSE, drawn from scratch: the back takes the pitch going to
    /// orange and hands it to the wide orange coming back. The coach draws
    /// two runs and names two carriers; both exchanges are placed for him,
    /// they pass the checker, and the ball changes hands where they meet.
    /// </summary>
    [Fact]
    public async Task AReverseHandsItOnWhereTheTwoRunnersMeet()
    {
        var (page, errors) = await OpenDesignerAsync(Desk, "ACE");

        await ToolAsync(page, "ball");
        await page.ClickAsync("[data-mode=\"carry\"]");
        await TapAsync(page, -1.5, -5);                            // the BACK takes it first
        await page.ClickAsync("#dz-hand");
        await TapAsync(page, 9, 0);                                // ...and gives it to WIDE ORANGE
        Assert.Equal(new[] { "H" }, await page.EvaluateAsync<string[]>("DZ.d.ball.via"));
        Assert.Equal("Z", await page.EvaluateAsync<string>("DZ.d.ball.who"));

        await ToolAsync(page, "route");
        await RouteAsync(page, (-1.5, -5), (2, -4), (5, -3), (8.5, -2), (11, 0.5));
        await RouteAsync(page, (9, 0), (8, -2.5), (4.5, -4), (0, -4.5), (-5, -4), (-10, -0.5), (-12, 7));

        Assert.Equal(0, await ErrorCountAsync(page));
        var hands = await page.EvaluateAsync<string[]>(
            "S.play.paths.filter(p => p.type === 'handoff').map(p => p.who + '>' + p.to)");
        Assert.Equal(new[] { "QB>H", "H>Z" }, hands);
        Assert.Equal(new[] { "H", "Z" }, await page.EvaluateAsync<string[]>("S.play.ball.legs.map(l => l.who)"));

        var calls = await CallsAsync(page);
        Assert.Contains("take it from BACK and CARRY to blue", calls);
        Assert.Contains("CARRY to orange, then hand it off", calls);
        Assert.Contains("hand it to BACK", calls);
        // the carriers lead the strip in the order they have it, as play 26's does
        Assert.Equal(new[] { "BACK", "WIDE ORANGE", "THROWER" },
            (await page.EvaluateAsync<string[]>("S.play.calls.map(c => c[0])")).Take(3).ToArray());

        // a frame either side of the second exchange: the back has it, then the wide orange
        var sides = await page.EvaluateAsync<double[]>("""
            (() => {
              S.stage = "run"; S.tl = buildTimeline(S.play);
              const legs = S.tl.ball.legs, at = legs[1].start;
              const off = (t, leg) => {
                S.t0 = 0; frame(t);
                const m = /translate\(\s*(-?[\d.]+)\s+(-?[\d.]+)/.exec(SC.ball.getAttribute('transform'));
                const seg = S.tl.paths[leg.pathIdx];
                const e = walk(S.play.paths[leg.pathIdx].pts, Math.max(0, Math.min(1, (t - seg.start) / seg.dur))).end;
                const s = F2S(e[0], e[1]);
                return Math.hypot(+m[1] - s[0], +m[2] - s[1]);
              };
              return [off(at - 200, legs[0]), off(at + 300, legs[1])];
            })()
            """);
        Assert.True(sides[0] < 3, $"before the exchange the ball is {sides[0]} off the back");
        Assert.True(sides[1] < 3, $"after the exchange the ball is {sides[1]} off the wide orange");
        await page.EvaluateAsync("dzRefresh()");

        // it survives the trip through storage, chain and all
        Assert.True(await page.EvaluateAsync<bool>(
            "(() => { const d = saneDesign(Object.assign({}, DZ.d, {id: 'u_reverse1'})); " +
            "return !!d && d.ball.via.join() === 'H' && d.ball.who === 'Z'; })()"));

        // the thrower only ever starts a chain; Take back undoes the last handoff
        await ToolAsync(page, "ball");
        await page.ClickAsync("#dz-hand");
        await TapAsync(page, 0, -3);
        Assert.Contains("thrower", await page.InnerTextAsync("#dz-line"));
        Assert.Equal("Z", await page.EvaluateAsync<string>("DZ.d.ball.who"));
        await page.ClickAsync("#dz-unhand");
        Assert.Equal("H", await page.EvaluateAsync<string>("DZ.d.ball.who"));
        Assert.True(await page.EvaluateAsync<bool>("!DZ.d.ball.via"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// JET PASS, drawn: the THROWER hands it to the back, the back runs to
    /// orange, stops behind the line and throws it to the wide orange. The
    /// handoff is placed for the coach, the ball rides the back's run and
    /// flies from where it ends, his line cannot be drawn past the line, and
    /// a handoff keeps it out of the no-run zone even though it is a pass.
    /// </summary>
    [Fact]
    public async Task ABackCanTakeAHandoffAndThrowIt()
    {
        var (page, errors) = await OpenDesignerAsync(Desk, "TWINS RIGHT");

        await ToolAsync(page, "ball");
        Assert.True(await page.Locator("#dz-hand").IsDisabledAsync(), "a handoff with nobody to throw to");
        await TapAsync(page, 11, 0);                               // WIDE ORANGE catches it
        await page.ClickAsync("#dz-hand");
        await TapAsync(page, -2.6, -4.4);                          // ...from the BACK
        Assert.Equal(new[] { "H" }, await page.EvaluateAsync<string[]>("DZ.d.ball.via"));
        Assert.Equal("Z", await page.EvaluateAsync<string>("DZ.d.ball.who"));

        await ToolAsync(page, "route");
        await RouteAsync(page, (-2.6, -4.4), (1, -3.8), (5, -2.5));

        Assert.Equal(0, await ErrorCountAsync(page));
        var hand = await page.EvaluateAsync<JsonElement>("S.play.paths.find(p => p.type === 'handoff')");
        Assert.Equal("QB>H", hand.GetProperty("who").GetString() + ">" + hand.GetProperty("to").GetString());
        Assert.False(hand.TryGetProperty("pitch", out _));
        Assert.Equal("pass", await page.EvaluateAsync<string>("S.play.ball.mode"));
        Assert.Equal("H", await page.EvaluateAsync<string>("S.play.ball.thrower"));
        Assert.Equal(new[] { 5.0, -2.5 }, await page.EvaluateAsync<double[]>("throwArc(S.play).src"));

        var calls = await CallsAsync(page);
        Assert.Contains("CARRY to orange, stop, throw it to WIDE ORANGE", calls);
        Assert.Contains("hand it to BACK", calls);
        Assert.Equal(new[] { "BACK", "WIDE ORANGE", "THROWER" },
            (await page.EvaluateAsync<string[]>("S.play.calls.map(c => c[0])")).Take(3).ToArray());
        Assert.Contains("from BACK", await page.EvaluateAsync<string>("S.play.assign.Z"));

        // mid-run the ball is in the back's hands; after the catch it is with the wide orange
        var off = await page.EvaluateAsync<double[]>("""
            (() => {
              S.stage = "run"; S.tl = buildTimeline(S.play);
              const b = S.tl.ball, leg = b.legs[0], seg = S.tl.paths[leg.pathIdx];
              const at = t => {
                S.t0 = 0; frame(t);
                const m = /translate\(\s*(-?[\d.]+)\s+(-?[\d.]+)/.exec(SC.ball.getAttribute('transform'));
                return [+m[1], +m[2]];
              };
              const mid = at(seg.start + seg.dur * 0.6), e = walk(S.play.paths[leg.pathIdx].pts, 0.6).end;
              const s = F2S(e[0], e[1]), done = at(b.start + b.dur + 50), d = F2S(b.dst[0], b.dst[1]);
              return [Math.hypot(mid[0] - s[0], mid[1] - s[1]), Math.hypot(done[0] - d[0], done[1] - d[1])];
            })()
            """);
        Assert.True(off[0] < 3, $"mid-run the ball is {off[0]} off the back");
        Assert.True(off[1] < 3, $"after the catch the ball is {off[1]} off the catch point");
        await page.EvaluateAsync("dzRefresh()");

        // whoever throws it throws from behind the line
        await TapAsync(page, 5, -2.5);
        await TapAsync(page, 8, 3);
        Assert.Equal(new[] { 8.0, 0 }, await page.EvaluateAsync<double[]>("DZ.d.routes.H.pts[2]"));

        // the THROWER's line is the exchange; the back cannot be his own target
        await TapAsync(page, 0, -3);
        Assert.NotEqual("QB", await page.EvaluateAsync<string>("DZ.sel || ''"));
        await ToolAsync(page, "ball");
        await TapAsync(page, -2.6, -4.4);
        Assert.Contains("throws it", await page.InnerTextAsync("#dz-line"));
        await TapAsync(page, 6, 0);                                // a new target keeps the thrower
        Assert.Equal("Y", await page.EvaluateAsync<string>("DZ.d.ball.who"));
        Assert.Equal(new[] { "H" }, await page.EvaluateAsync<string[]>("DZ.d.ball.via"));

        // it survives storage, and an exchange on a pass is still out of the no-run zone
        Assert.True(await page.EvaluateAsync<bool>(
            "(() => { const d = saneDesign(Object.assign({}, DZ.d, {id: 'u_jetpass1'})); " +
            "return !!d && d.ball.mode === 'pass' && d.ball.via.join() === 'H' && !d.ball.pitch; })()"));
        await page.ClickAsync("#dz-save");
        await page.WaitForSelectorAsync("#dzsheet");
        Assert.True(await page.Locator("#dz-cat [data-cat=\"NO-RUN ZONE\"]").IsDisabledAsync());
        await page.ClickAsync("#dz-cancel");

        // Take back: the THROWER throws it again, to the same kid
        await page.ClickAsync("#dz-unhand");
        Assert.True(await page.EvaluateAsync<bool>("!DZ.d.ball.via && DZ.d.ball.who === 'Y'"));
        Assert.True(await page.EvaluateAsync<bool>("!S.play.paths.some(p => p.type === 'handoff')"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Every shipped play in every format turns back into a drawing the
    /// designer can open and save: the same kids, the ball to the same kids in
    /// the same order, every route still drawn. Motion and fake handoffs do not
    /// come across, so this holds what does.
    /// </summary>
    [Fact]
    public async Task EveryShippedPlayCopiesIntoADrawing()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        var bad = await page.EvaluateAsync<string[]>("""
            (() => {
              const out = [];
              let seen = 0;
              for (const n of FORMAT_IDS){
                loadMode(n);
                for (const p of DATA.plays.filter(q => !q.custom)){
                  seen++;
                  const d = designFrom(p);
                  const clean = d && saneDesign(Object.assign({}, d, {id: "u_copycheck"}));
                  if (!clean){ out.push(p.id + " is not a design"); continue; }
                  const c = compileDesign(clean, 0);
                  const holders = q => q.ball.legs ? q.ball.legs.map(l => l.who).join(">") : "";
                  if (c.ball.mode !== p.ball.mode || c.ball.who !== p.ball.who || holders(c) !== holders(p)
                      || (c.ball.thrower || "") !== (p.ball.thrower || ""))
                    out.push(p.id + " ball " + JSON.stringify(c.ball) + " not " + JSON.stringify(p.ball));
                  for (const path of p.paths)
                    if (path.type === "route" && !clean.routes[path.who]) out.push(p.id + " lost " + path.who);
                  if (c.formation !== p.formation) out.push(p.id + " moved somebody: " + c.formation);
                  const e = errorsIn(c);
                  if (e.length) out.push(p.id + " " + e.map(f => f.rule + ": " + f.msg).join("; "));
                }
              }
              loadMode(cfg.players);
              return seen > 60 ? out : ["only saw " + seen + " plays"];
            })()
            """);
        Assert.True(bad.Length == 0, string.Join("\n", bad));
        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Copy to edit, from the library's preview: the play opens in the designer
    /// as a new drawing with its names, saves as the coach's own, goes into the
    /// deck, and leaves the shipped play exactly as it was. Leaving the copy
    /// unsaved asks first, and a drawn play copies as well.
    /// </summary>
    [Fact]
    public async Task ACoachCopiesAShippedPlayAndSavesItAsHisOwn()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.ClickAsync(".lthumb[data-prev=\"p_01\"]");
        await page.ClickAsync("#prev-copy");
        await page.WaitForSelectorAsync(".designer #field");
        await page.WaitForTimeoutAsync(300);

        Assert.Equal("22 DIVE", (await page.InnerTextAsync("#dz-title")).Trim());
        Assert.Equal("TWINS RIGHT", (await page.InnerTextAsync("#dz-fm")).Trim());
        Assert.Equal(0, await ErrorCountAsync(page));
        Assert.Equal("H", await page.EvaluateAsync<string>("S.play.ball.who"));
        Assert.Contains("CARRY", await CallsAsync(page));

        // a copy is work: leaving it asks first
        await page.ClickAsync("#dz-back");
        Assert.Equal("DISCARD?", (await page.InnerTextAsync("#dz-back")).Trim().ToUpperInvariant());

        // the wide blue breaks out instead, and it saves as a play of his own
        await TapAsync(page, -9, 0);                               // WIDE BLUE
        await TapAsync(page, -9, 10);                              // the ring on the end of his GO
        await TapAsync(page, -13, 10);
        Assert.Contains("OUT at 10", await CallsAsync(page));
        Assert.True(await ErrorCountAsync(page) == 0, await page.InnerTextAsync("#dz-line"));
        await page.ClickAsync("#dz-save");
        await page.WaitForSelectorAsync("#dzsheet");
        Assert.Equal("22 DIVE", await page.InputValueAsync("#dz-real"));
        Assert.Equal("BULLDOZER", await page.InputValueAsync("#dz-fun"));
        Assert.Equal(0, await page.Locator("#dz-del").CountAsync());
        await page.FillAsync("#dz-real", "22 Dive Post");
        await page.ClickAsync("#dz-ok");
        await page.WaitForSelectorAsync(".lib");
        await page.WaitForTimeoutAsync(300);

        var id = await page.EvaluateAsync<string>("Object.keys(designs)[0]");
        Assert.StartsWith("u_", id);
        Assert.True(await page.EvaluateAsync<bool>($"deck.includes('{id}')"), "the copy did not go into the deck");
        Assert.Equal("22 DIVE POST", (await page.InnerTextAsync($".lrow[data-id=\"{id}\"] .lname b")).Trim());
        // the shipped play is untouched: its own name, its own route
        Assert.Equal("22 DIVE", (await page.InnerTextAsync(".lrow[data-id=\"p_01\"] .lname b")).Trim());
        Assert.Equal(2, await page.EvaluateAsync<int>(
            "DATA.plays.find(p => p.id === 'p_01').paths.find(p => p.who === 'X').pts.length"));

        // a drawn play copies as the drawing it is, into a second play
        await page.ClickAsync($".lthumb[data-prev=\"{id}\"]");
        await page.ClickAsync("#prev-copy");
        await page.WaitForSelectorAsync(".designer #field");
        Assert.Equal(new[] { -13.0, 10 }, await page.EvaluateAsync<double[]>("DZ.d.routes.X.pts[1]"));
        Assert.True(await page.EvaluateAsync<bool>("DZ.id === null && DZ.d.id === null"));
        await page.ClickAsync("#dz-save");
        await page.WaitForSelectorAsync("#dzsheet");
        await page.ClickAsync("#dz-ok");
        await page.WaitForSelectorAsync(".lib");
        Assert.Equal(2, await page.EvaluateAsync<int>("customCount()"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// The same trick off a pitch: a soft toss back to the back already going
    /// to the edge — PITCH RIGHT until he stops and throws. The pitch is
    /// placed out in front of him and behind the THROWER. One that would have
    /// to go forward is a forward pass, and the checker will not let the kid
    /// who caught it throw another.
    /// </summary>
    [Fact]
    public async Task ABackCanTakeAPitchAndThrowIt()
    {
        var (page, errors) = await OpenDesignerAsync(Desk, "ACE");

        await ToolAsync(page, "ball");
        await TapAsync(page, 9, 0);                                // WIDE ORANGE catches it
        await page.ClickAsync("#dz-pitch");
        await TapAsync(page, -1.5, -5);                            // ...from the BACK
        Assert.True(await page.EvaluateAsync<bool>("DZ.d.ball.pitch === true"));

        await ToolAsync(page, "route");
        await RouteAsync(page, (-1.5, -5), (2, -4.5), (6, -3), (8, -2));

        Assert.Equal(0, await ErrorCountAsync(page));
        var pts = await page.EvaluateAsync<double[][]>(
            "S.play.paths.find(p => p.type === 'handoff' && p.pitch).pts");
        Assert.True(pts[1][1] <= pts[0][1], "the pitch went forward");
        var toss = Math.Sqrt(Math.Pow(pts[1][0] - pts[0][0], 2) + Math.Pow(pts[1][1] - pts[0][1], 2));
        Assert.True(toss >= 3, $"a {toss:0.0} yd pitch is a handoff");
        Assert.Contains("soft pitch to BACK", await CallsAsync(page));
        Assert.Contains("stop, throw it to WIDE ORANGE", await CallsAsync(page));

        // a slot who never comes back behind the THROWER cannot be pitched to
        await ToolAsync(page, "ball");
        Assert.Contains("the pitch", await page.InnerTextAsync("#dz-unhand"), StringComparison.OrdinalIgnoreCase);
        await page.ClickAsync("#dz-unhand");
        await page.ClickAsync("#dz-pitch");
        await TapAsync(page, 3, 0);                                // SLOT ORANGE
        await ToolAsync(page, "route");
        await RouteAsync(page, (3, 0), (6, 0));
        Assert.Contains("LEGAL/pitch", await page.EvaluateAsync<string[]>("DZ.findings.map(f => f.rule)"));
        Assert.True(await page.Locator("#dz-save").IsDisabledAsync());

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A drawing gone wrong goes back to the formation it started from without
    /// leaving the screen: every kid on his spot, no routes, nobody with the
    /// ball, on the second tap. The names stay, and on a saved play the id, so
    /// Save still overwrites the play it came from.
    /// </summary>
    [Fact]
    public async Task StartOverPutsEverybodyBackOnTheFormation()
    {
        var (page, errors) = await OpenDesignerAsync(Desk, "TWINS RIGHT");
        Assert.True(await page.Locator("#dz-reset").IsDisabledAsync(), "Start over is live on an untouched play");

        await RouteAsync(page, (-9, 0), (-9, 6), (-12, 6));
        await ToolAsync(page, "move");
        await DragAsync(page, (6, 0), (4, -2));
        await ToolAsync(page, "ball");
        await TapAsync(page, 11, 0);
        Assert.False(await page.Locator("#dz-reset").IsDisabledAsync());

        await page.ClickAsync("#dz-reset");                        // one tap only arms it
        Assert.Equal("SURE?", (await page.InnerTextAsync("#dz-reset")).Trim().ToUpperInvariant());
        Assert.Equal("Z", await page.EvaluateAsync<string>("DZ.d.ball && DZ.d.ball.who"));
        await page.ClickAsync("#dz-reset");
        await page.WaitForTimeoutAsync(150);

        Assert.True(await page.EvaluateAsync<bool>("""
            (() => {
              const d = DZ.d, f = FORMATIONS[d.base];
              return d.ball === null && Object.keys(d.routes).length === 0 && DZ.sel === null
                && Object.keys(f).every(k => d.spots[k][0] === f[k].x && d.spots[k][1] === f[k].y);
            })()
            """), "Start over left part of the old drawing behind");
        Assert.DoesNotContain("SHIFTED", await page.InnerTextAsync("#dz-fm"));
        Assert.Equal("EVERYONE ELSE", (await CallsAsync(page)).Split('\n')[0].Trim());
        Assert.True(await page.Locator("#dz-reset").IsDisabledAsync());
        Assert.Equal("route", await page.EvaluateAsync<string>("DZ.tool"));

        // a new play started over is a new play again: leaving costs one tap
        await page.ClickAsync("#dz-back");
        await page.WaitForSelectorAsync(".lib");

        // a saved play started over keeps who it is, so Save overwrites it
        await page.EvaluateAsync(Seed, SlotIn());
        await page.EvaluateAsync("renderLibrary()");
        await page.ClickAsync(".lrow[data-id=\"u_slotin01\"] [data-ren]");
        await page.WaitForSelectorAsync(".designer #field");
        await page.ClickAsync("#dz-reset");
        await page.ClickAsync("#dz-reset");
        await page.WaitForTimeoutAsync(150);
        Assert.Equal("u_slotin01", await page.EvaluateAsync<string>("DZ.id"));
        Assert.Equal("SLOT IN", await page.EvaluateAsync<string>("DZ.d.coachName"));
        Assert.True(await page.EvaluateAsync<bool>("DZ.dirty"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    [Fact]
    public async Task SaveStaysShutUntilTheCheckerIsHappy()
    {
        var (page, errors) = await OpenDesignerAsync(Desk, "SPREAD");

        Assert.True(await page.Locator("#dz-save").IsDisabledAsync());
        Assert.Contains("nobody gets the ball", await page.InnerTextAsync("#dz-line"));

        await ToolAsync(page, "ball");
        await TapAsync(page, 10, 0);                               // WIDE ORANGE, deep
        Assert.False(await page.Locator("#dz-save").IsDisabledAsync());

        // SLOT BLUE dragged on top of WIDE BLUE: they run up the same lane together
        await ToolAsync(page, "move");
        await DragAsync(page, (-5, 0), (-9.5, 0));
        Assert.True(await page.Locator("#dz-save").IsDisabledAsync(), "Save is open on a collision");
        var line = await page.InnerTextAsync("#dz-line");
        Assert.Contains("WIDE BLUE", line);
        Assert.Contains("SLOT BLUE", line);
        Assert.Equal(1, await page.Locator("#dz-line.err").CountAsync());

        await DragAsync(page, (-9.5, 0), (-5, 0));
        Assert.False(await page.Locator("#dz-save").IsDisabledAsync());

        // leaving with work on the screen is two taps
        await page.ClickAsync("#dz-back");
        await page.WaitForTimeoutAsync(150);
        Assert.Equal(1, await page.Locator(".designer").CountAsync());
        await page.ClickAsync("#dz-back");
        await page.WaitForSelectorAsync(".lib");
        Assert.Equal(0, await page.EvaluateAsync<int>("customCount()"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Every control on the designer is on screen at every tablet shape, the
    /// field has room to draw on, and the formation picker fits.
    /// </summary>
    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task TheDesignerFitsEveryTablet(string label, int width, int height)
    {
        var size = new Viewport(label, width, height);
        var (page, errors) = await app.OpenAppAsync(size);
        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.ClickAsync("#lnew");
        await page.WaitForSelectorAsync("#fmsheet");
        foreach (var b in await page.Locator(".fmbtn").AllAsync())
        {
            await b.ScrollIntoViewIfNeededAsync();
            var box = await b.BoundingBoxAsync();
            Assert.True(box is not null && box.Y >= 0 && box.Y + box.Height <= height + 1,
                $"a formation is off the picker at {size}");
        }

        await page.ClickAsync(".fmbtn[data-fm=\"TWINS RIGHT\"]");
        await page.WaitForSelectorAsync(".designer #field");
        await page.WaitForTimeoutAsync(300);

        async Task OnScreen(string sel)
        {
            var el = page.Locator(sel);
            await el.ScrollIntoViewIfNeededAsync();
            var box = await el.BoundingBoxAsync();
            Assert.True(box is not null && box.Width > 0 && box.X >= 0 && box.Y >= 0
                        && box.X + box.Width <= width + 1 && box.Y + box.Height <= height + 1,
                $"{sel} is off the screen at {size}");
        }

        var field = await page.Locator("#field").BoundingBoxAsync();
        Assert.True(field is not null && field.Height > 200, $"no room to draw at {size}");
        await OnScreen("#dz-save");
        await OnScreen("#dz-reset");
        await OnScreen("#dz-back");
        await OnScreen("#dz-line");
        await OnScreen("#dz-calls");

        // the fullest the rail gets: a kid selected with a route drawn, and the ball tool
        await RouteAsync(page, (-9, 0), (-9, 5));
        foreach (var sel in new[] { "[data-tool=\"move\"]", "[data-tool=\"route\"]", "[data-tool=\"ball\"]",
                                    "#dz-undo", "#dz-clear", "#dz-end", "#dz-prev" })
            await OnScreen(sel);
        await ToolAsync(page, "ball");
        await OnScreen("[data-mode=\"pass\"]");
        await OnScreen("[data-mode=\"carry\"]");
        // the fullest the ball tool gets: a run, with a handoff to hand on
        await page.ClickAsync("[data-mode=\"carry\"]");
        await TapAsync(page, -2.6, -4.4);
        await OnScreen("#dz-hand");
        await OnScreen("#dz-unhand");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// The plays are the coach's, not the build's: a real new build, taken
    /// through the service worker, leaves them where they were. Then they go
    /// to an assistant's tablet in the sync file, which replaces the plays he
    /// had, trusts nothing in the file, and escapes what it shows.
    /// </summary>
    [Fact]
    public async Task PlaysSurviveAnUpdateAndTravelBySync()
    {
        // ---- the head coach's tablet, with a play drawn on it
        var (a, pageA, errorsA) = await OpenHostedAsync();
        await pageA.EvaluateAsync(Seed, SlotIn());
        await pageA.EvaluateAsync("renderDeck()");
        await pageA.WaitForTimeoutAsync(300);
        Assert.Equal(1, await pageA.Locator(".tile[data-id=\"u_slotin01\"]").CountAsync());

        Assert.Equal(0, _build.Ship("9.9.9-designer"));
        await pageA.EvaluateAsync("navigator.serviceWorker.getRegistration().then(r => r.update())");
        await pageA.WaitForTimeoutAsync(2500);
        await pageA.ClickAsync("#upd");
        await pageA.WaitForTimeoutAsync(2500);
        await pageA.ClickAsync("#start");
        await pageA.WaitForSelectorAsync(".deck");
        await pageA.WaitForTimeoutAsync(400);
        Assert.Equal("9.9.9-designer", await pageA.EvaluateAsync<string>("APP_BUILD"));
        Assert.Equal(1, await pageA.Locator(".tile[data-id=\"u_slotin01\"]").CountAsync());
        Assert.Equal("SLOT IN", (await pageA.InnerTextAsync(".tile[data-id=\"u_slotin01\"] .kid")).Trim());

        await pageA.ClickAsync("#ham");
        await pageA.ClickAsync("#setup");
        await AppFixture.SetupTabAsync(pageA, "tablet");
        await pageA.WaitForSelectorAsync("#syncfile", new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached });
        var download = await pageA.RunAndWaitForDownloadAsync(() => pageA.ClickAsync("#syncexp"));
        var dir = Path.Combine(Path.GetTempPath(), "hb-design-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, download.SuggestedFilename);
        await download.SaveAsAsync(path);
        await a.CloseAsync();
        Assert.True(errorsA.Count == 0, string.Join("\n", errorsA));

        // the file carries the drawing, not a compiled play
        var file = JsonNode.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken))!.AsObject();
        // a drawn play travels in its own format's section of the file
        var plays = file["modes"]!["6"]!["plays"]!.AsArray();
        Assert.Single(plays);
        Assert.Equal("SPREAD", plays[0]!["base"]!.GetValue<string>());
        Assert.Null(plays[0]!["calls"]);

        // one more in the file that collides, and one whose name is markup
        var bad = JsonNode.Parse(plays[0]!.ToJsonString())!.AsObject();
        bad["id"] = "u_badone01";
        bad["spots"]!["Y"] = new JsonArray(-9.5, 0);
        plays.Add(bad);
        var markup = JsonNode.Parse(plays[0]!.ToJsonString())!.AsObject();
        markup["id"] = "u_markup01";
        markup["coachName"] = "<b>X</b> & CO";
        plays.Add(markup);
        file["modes"]!["6"]!["deck"]!.AsArray().Add("u_markup01");
        file["modes"]!["6"]!["deck"]!.AsArray().Add("u_badone01");
        var tampered = Path.Combine(dir, "tampered.json");
        await File.WriteAllTextAsync(tampered, file.ToJsonString(), TestContext.Current.CancellationToken);

        // ---- the assistant's tablet, with a play of its own that is about to go
        var (b, pageB, errorsB) = await OpenHostedAsync();
        await pageB.EvaluateAsync(Seed, SlotIn("u_mineonly", "MY OWN"));
        await pageB.ClickAsync("#ham");
        await pageB.ClickAsync("#setup");
        await AppFixture.SetupTabAsync(pageB, "tablet");
        await pageB.WaitForSelectorAsync("#syncfile", new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached });
        await pageB.WaitForTimeoutAsync(300);
        Assert.Contains("including the 1 play drawn here", await pageB.InnerTextAsync(".setrows"));
        // with a play on the tablet, Import arms first
        await pageB.ClickAsync("#syncimp");
        Assert.Equal("SURE?", (await pageB.InnerTextAsync("#syncimp")).Trim().ToUpperInvariant());
        await pageB.SetInputFilesAsync("#syncfile", tampered);
        await pageB.WaitForSelectorAsync("#syncmsg");
        await pageB.WaitForTimeoutAsync(300);

        var msg = await pageB.InnerTextAsync("#syncmsg");
        Assert.Contains("2 plays of your own", msg);
        Assert.Contains("not recognised", msg);
        var ids = await pageB.EvaluateAsync<string[]>("Object.keys(designs).sort()");
        Assert.Equal(new[] { "u_markup01", "u_slotin01" }, ids);
        Assert.False(await pageB.EvaluateAsync<bool>("deck.includes('u_badone01') || deck.includes('u_mineonly')"));

        await pageB.ClickAsync("#sdone");
        await pageB.WaitForTimeoutAsync(400);
        var tile = ".tile[data-id=\"u_markup01\"]";
        Assert.Equal("<B>X</B> & CO", (await pageB.InnerTextAsync(tile + " .kid")).Trim());
        Assert.Equal(0, await pageB.Locator(tile + " .kid b").CountAsync());

        // and it sticks
        await pageB.ReloadAsync();
        await pageB.WaitForTimeoutAsync(1000);
        await pageB.ClickAsync("#start");
        await pageB.WaitForSelectorAsync(".deck");
        Assert.Equal(1, await pageB.Locator(".tile[data-id=\"u_slotin01\"]").CountAsync());

        await b.CloseAsync();
        Directory.Delete(dir, recursive: true);
        Assert.True(errorsB.Count == 0, string.Join("\n", errorsB));
    }

    /// <summary>
    /// A play the checker rejects — because a later build is stricter, say —
    /// is still a coach's work. It stays, marked, and opens to be fixed. One
    /// this build cannot read at all is carried through untouched.
    /// </summary>
    [Fact]
    public async Task AStoredPlayTheCheckerRejectsIsKeptAndMarked()
    {
        var page = await app.Browser.NewPageAsync(new BrowserNewPageOptions
        {
            ViewportSize = new ViewportSize { Width = Desk.Width, Height = Desk.Height },
        });
        await AppFixture.MarkTourSeenAsync(page);
        await page.AddInitScriptAsync("""
            try {
              localStorage.setItem("hb.plays", JSON.stringify({v: 1, byId: {
                u_broken01: {id: "u_broken01", v: 1, base: "SPREAD", coachName: "PILEUP",
                  spots: {C: [0, 0], QB: [0, -3], X: [-10, 0], Y: [-9.5, 0], H: [5, 0], Z: [10, 0]},
                  routes: {}, ball: {mode: "pass", who: "Z"}, category: "CUSTOM",
                  complexity: "simple", created: 1},
                u_future01: {id: "u_future01", v: 9, base: "WISHBONE"}}}));
            } catch(e){}
            """);
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);
        await page.GotoAsync(app.AppUri);
        await page.WaitForTimeoutAsync(400);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");

        await page.ClickAsync("#ham");
        await page.ClickAsync("#edit");
        await page.WaitForSelectorAsync(".lrow[data-id=\"u_broken01\"]");
        Assert.Contains("NEEDS FIXING",
            (await page.InnerTextAsync(".lrow[data-id=\"u_broken01\"] .lname i")).ToUpperInvariant());
        Assert.Equal(0, await page.Locator(".lrow[data-id=\"u_future01\"]").CountAsync());

        await page.ClickAsync(".lrow[data-id=\"u_broken01\"] [data-ren]");
        await page.WaitForSelectorAsync(".designer #field");
        Assert.True(await page.Locator("#dz-save").IsDisabledAsync());

        // fix it, save it, and the unreadable one is still in storage beside it
        await ToolAsync(page, "move");
        await DragAsync(page, (-9.5, 0), (-5, 0));
        await page.ClickAsync("#dz-save");
        await page.WaitForSelectorAsync("#dzsheet");
        await page.ClickAsync("#dz-ok");
        await page.WaitForSelectorAsync(".lib");
        Assert.DoesNotContain("NEEDS FIXING",
            (await page.InnerTextAsync(".lrow[data-id=\"u_broken01\"] .lname i")).ToUpperInvariant());
        Assert.True(await page.EvaluateAsync<bool>(
            "!!JSON.parse(localStorage.getItem('hb.plays')).byId.u_future01"),
            "a design this build could not read was thrown away on save");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Start over puts back what shipped, and a coach's play is not something
    /// that shipped — it stays. Clear all is the one thing that removes them,
    /// on the second tap, and it leaves no hole in the deck or a saved week.
    /// </summary>
    [Fact]
    public async Task StartOverKeepsThemAndClearAllRemovesThem()
    {
        var (page, errors) = await app.OpenAppAsync(Desk);
        await page.EvaluateAsync(Seed, SlotIn());
        await page.EvaluateAsync("""
            () => { deck = ['u_slotin01']; saveDeck();
                    setPack('week2', ['p_01', 'u_slotin01']); }
            """);

        await page.ClickAsync("#ham");
        await page.ClickAsync("#setup");
        await AppFixture.SetupTabAsync(page, "tablet");
        await page.WaitForSelectorAsync("#allreset");
        await page.ClickAsync("#allreset");
        await page.ClickAsync("#allreset");
        await page.WaitForTimeoutAsync(300);
        Assert.Equal(1, await page.EvaluateAsync<int>("customCount()"));
        Assert.Equal("intro", await page.EvaluateAsync<string>("S.screen"));

        await page.EvaluateAsync("() => { deck = ['u_slotin01']; saveDeck(); " +
                                 "setPack('week2', ['p_01', 'u_slotin01']); renderSettings('yours'); }");
        await page.WaitForTimeoutAsync(200);
        Assert.False(await page.Locator("#dzclear").IsDisabledAsync());
        await page.ClickAsync("#dzclear");
        Assert.Equal(1, await page.EvaluateAsync<int>("customCount()"));   // one tap only arms it
        await page.ClickAsync("#dzclear");
        await page.WaitForTimeoutAsync(300);

        Assert.Equal(0, await page.EvaluateAsync<int>("customCount()"));
        Assert.True(await page.Locator("#dzclear").IsDisabledAsync());
        Assert.True(await page.EvaluateAsync<bool>(
            // the deck was only that play, so it is empty now — not refilled with Week 1
            "deck.length === 0 && " +
            "!(packSaves.week2 || []).includes('u_slotin01')"));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    private async Task<(IBrowserContext Context, IPage Page, List<string> Errors)> OpenHostedAsync()
    {
        var context = await app.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = Desk.Width, Height = Desk.Height },
            AcceptDownloads = true,
        });
        await AppFixture.MarkTourSeenAsync(context);
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);
        await page.GotoAsync(_build.Site.Origin + "/index.html");
        await page.WaitForTimeoutAsync(1500);
        await page.ClickAsync("#start");
        await page.WaitForSelectorAsync(".deck");
        return (context, page, errors);
    }
}
