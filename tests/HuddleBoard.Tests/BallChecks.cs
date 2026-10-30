using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuddleBoard.Tests;

/// <summary>
/// The ball marker: a football, pointed the way it is travelling, finishing
/// where the play says the ball finishes.
/// </summary>
/// <remarks>
/// It used to be a yellow dot with a pulsing ring under it. The kids already
/// know what a football looks like, so the marker is one now — and because the
/// players hold their spots through the whole animation, this marker is the
/// only thing on screen saying where the ball ended up. Landing it in the wrong
/// place, or in the right place only until the coach hits Mirror, is the
/// failure worth catching.
/// </remarks>
public sealed class BallChecks(AppFixture app)
{
    private sealed record Ball(
        [property: JsonPropertyName("circles")] int Circles,
        [property: JsonPropertyName("shapes")] int Shapes,
        [property: JsonPropertyName("opacity")] double Opacity,
        [property: JsonPropertyName("rotated")] bool Rotated,
        [property: JsonPropertyName("dx")] double Dx,
        [property: JsonPropertyName("dy")] double Dy,
        [property: JsonPropertyName("throwOpacity")] double ThrowOpacity,
        [property: JsonPropertyName("throwDx")] double ThrowDx,
        [property: JsonPropertyName("throwDy")] double ThrowDy);

    /// <summary>Jump the animation to just before it loops, so the ball has arrived.</summary>
    /// <summary>Opens a play from any format: its id says which.</summary>
    private static Task OpenAsync(Microsoft.Playwright.IPage page, string play)
    {
        var players = play[0] switch { 'f' => 5, 's' => 7, _ => 6 };
        return page.EvaluateAsync($"loadMode({players}); openPlay('{play}')");
    }

    private const string Settle = """
        () => { S.stage = 'run'; S.t0 = performance.now() - (S.tl.tEnd - 600); syncRail(); }
        """;

    /// <summary>Where the ball is, against where the play says it should be.</summary>
    private const string Measure = """
        () => {
          const b = S.tl.ball, path = S.play.paths[b.pathIdx];
          const dst = path.pts[path.pts.length - 1];
          const want = W2S(dst[0], dst[1]);
          const tr = SC.ball.getAttribute('transform') || '';
          const m = /translate\(\s*(-?[\d.]+)\s+(-?[\d.]+)\s*\)/.exec(tr);
          // the dashed throw line, if this play has one: where its last point is
          const th = SC.throw;
          const last = th ? (th.getAttribute('points') || '').trim().split(' ').pop() : '';
          const te = last ? last.split(',').map(Number) : null;
          return JSON.stringify({
            throwOpacity: th ? +(th.getAttribute('opacity') || 0) : -1,
            throwDx: te ? +(te[0] - want[0]).toFixed(2) : 9999,
            throwDy: te ? +(te[1] - want[1]).toFixed(2) : 9999,
            circles: SC.ball.querySelectorAll('circle').length,
            shapes: SC.ball.querySelectorAll('path').length,
            opacity: +(SC.ball.getAttribute('opacity') || 0),
            rotated: /rotate\(/.test(tr),
            dx: m ? +(parseFloat(m[1]) - want[0]).toFixed(2) : 9999,
            dy: m ? +(parseFloat(m[2]) - want[1]).toFixed(2) : 9999});
        }
        """;

    [Theory]
    [InlineData("p_18", "a pass")]
    [InlineData("p_27", "a pass off a sweep")]
    [InlineData("p_01", "a handoff")]
    [InlineData("p_22", "a reverse")]
    public async Task TheBallIsAFootballAndItLandsWhereThePlaySaysEvenMirrored(
        string play, string kind)
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await page.EvaluateAsync($"openPlay('{play}')");
        await page.WaitForTimeoutAsync(400);

        foreach (var mirrored in new[] { false, true })
        {
            if (mirrored)
            {
                // snap the mirror rather than easing it, so the reading below is
                // of a settled field and not a frame somewhere mid-flip
                await page.EvaluateAsync("() => { mTarget = 1; mAnim = 1; }");
                await page.WaitForTimeoutAsync(150);
            }

            await page.EvaluateAsync(Settle);
            await page.WaitForTimeoutAsync(150);

            var g = JsonSerializer.Deserialize<Ball>(
                await page.EvaluateAsync<string>(Measure))!;
            var at = $"{kind}{(mirrored ? ", mirrored" : "")}";

            // a football: filled shapes and laces, and not the old yellow dot
            Assert.Equal(0, g.Circles);
            Assert.True(g.Shapes >= 2, $"{at}: the ball is {g.Shapes} shape(s), expected a body and laces");

            Assert.Equal(1, g.Opacity);
            Assert.True(g.Rotated, $"{at}: the ball is not pointed along its flight");

            // and it is sitting on the end of the ball's path, not near it
            Assert.True(Math.Abs(g.Dx) < 1.5 && Math.Abs(g.Dy) < 1.5,
                $"{at}: the ball finished {g.Dx},{g.Dy} away from the end of its path");

            // A pass leaves a dashed throw line behind the ball, the way a handoff
            // leaves its dashed arrow, and it ends where the ball does. A run has
            // no such line — its ball rides the run and the handoff arrows.
            if (kind.StartsWith("a pass", StringComparison.Ordinal))
            {
                Assert.True(g.ThrowOpacity == 1, $"{at}: the throw line is not showing");
                Assert.True(Math.Abs(g.ThrowDx) < 1.5 && Math.Abs(g.ThrowDy) < 1.5,
                    $"{at}: the throw line ends {g.ThrowDx},{g.ThrowDy} away from the catch");
            }
            else
            {
                Assert.True(g.ThrowOpacity == -1, $"{at}: a run play drew a throw line");
            }
        }

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
    /// <summary>
    /// Routes are drawn with their corners rounded, and only drawn so: the line
    /// still starts and ends on the authored points, and goes round each cut
    /// close by rather than through it. Swept over every line of a play with
    /// cuts in it, and it has to have looked at some corners to pass.
    /// </summary>
    [Theory]
    [InlineData("p_05")]
    [InlineData("p_22")]
    public async Task RoutesAreDrawnWithRoundedCorners(string play)
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await page.EvaluateAsync($"openPlay('{play}')");
        await page.WaitForTimeoutAsync(400);
        await page.EvaluateAsync(Settle);
        await page.WaitForTimeoutAsync(150);

        var bad = await page.EvaluateAsync<string[]>("""
            () => {
              const out = [], d = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1]);
              let corners = 0;
              S.play.paths.forEach((path, i) => {
                const drawn = (SC.paths[i].line.getAttribute('points') || '').trim()
                  .split(' ').map(q => q.split(',').map(Number));
                const raw = path.pts.map(q => W2S(q[0], q[1]));
                if (d(drawn[0], raw[0]) > 1 || d(drawn[drawn.length - 1], raw[raw.length - 1]) > 1)
                  out.push(`${path.who}: the line does not start and end on its points`);
                for (let j = 1; j < path.pts.length - 1; j++){
                  const a = path.pts[j-1], v = path.pts[j], b = path.pts[j+1];
                  const u = [a[0]-v[0], a[1]-v[1]], w = [b[0]-v[0], b[1]-v[1]];
                  const cos = (u[0]*w[0] + u[1]*w[1]) / (Math.hypot(...u) * Math.hypot(...w));
                  const turn = Math.PI - Math.acos(Math.max(-1, Math.min(1, cos)));
                  if (turn < 0.25 || turn > 3.1) continue;
                  corners++;
                  const near = Math.min(...drawn.map(q => d(q, raw[j])));
                  if (near < 1) out.push(`${path.who}: corner ${j} is drawn as a kink`);
                  if (near > R_CUT * SCALE) out.push(`${path.who}: corner ${j} is rounded ${near.toFixed(1)}px off`);
                }
              });
              if (!corners) out.push('no corners were looked at');
              return out;
            }
            """);

        await page.CloseAsync();
        Assert.True(bad.Length == 0, string.Join("\n", bad));
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A play opens on its finished picture — every route drawn, the ball at
    /// the catch — with no tap. Line up goes back to the spots, and Run it
    /// plays it once and stops on that same last frame rather than looping:
    /// Replay is there for another look.
    /// </summary>
    [Fact]
    public async Task APlayOpensFinishedAndARunStopsAtTheEnd()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await page.ClickAsync(".tile[data-id='p_05']");
        await page.WaitForTimeoutAsync(300);

        Assert.Equal("run", await page.EvaluateAsync<string>("S.stage"));
        Assert.Contains("Line up", await page.InnerTextAsync("#stage"), StringComparison.OrdinalIgnoreCase);
        Assert.True(await page.IsVisibleAsync("#replay"), "Replay is hidden on the finished play");
        var g = JsonSerializer.Deserialize<Ball>(await page.EvaluateAsync<string>(Measure))!;
        Assert.True(g.Opacity == 1 && Math.Abs(g.Dx) < 1.5 && Math.Abs(g.Dy) < 1.5,
            $"the play opened with the ball {g.Dx},{g.Dy} off the catch (opacity {g.Opacity})");
        Assert.True(g.ThrowOpacity == 1, "the play opened without its throw line");

        await page.ClickAsync("#stage");
        Assert.Equal("lineup", await page.EvaluateAsync<string>("S.stage"));
        Assert.Contains("Run it", await page.InnerTextAsync("#stage"), StringComparison.OrdinalIgnoreCase);

        // run it, and look again well past the end: still the last frame, not
        // the snap of a second time round
        await page.ClickAsync("#stage");
        var end = await page.EvaluateAsync<double>("S.tl.tEnd");
        await page.WaitForTimeoutAsync((int)end + 900);
        g = JsonSerializer.Deserialize<Ball>(await page.EvaluateAsync<string>(Measure))!;
        Assert.True(g.Opacity == 1 && Math.Abs(g.Dx) < 1.5 && Math.Abs(g.Dy) < 1.5,
            $"after the run the ball is {g.Dx},{g.Dy} off the catch (opacity {g.Opacity}) — it looped");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Slow runs the play at half speed: a whole play's worth of time in, the
    /// ball is still in the air, and it lands after twice that. Switching it
    /// mid-run keeps the play where it was, a play opened with it on still
    /// opens finished, and going back through the intro turns it off.
    /// </summary>
    [Fact]
    public async Task SlowRunsThePlayAtHalfSpeed()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await page.ClickAsync(".tile[data-id='p_05']");
        await page.WaitForTimeoutAsync(300);
        Assert.Contains("full speed", await page.InnerTextAsync("#slow"), StringComparison.OrdinalIgnoreCase);

        // switched on with the play held finished: it stays finished
        await page.ClickAsync("#slow");
        await page.WaitForTimeoutAsync(100);
        Assert.Contains("half speed", await page.InnerTextAsync("#slow"), StringComparison.OrdinalIgnoreCase);
        var g = JsonSerializer.Deserialize<Ball>(await page.EvaluateAsync<string>(Measure))!;
        Assert.True(Math.Abs(g.Dx) < 1.5 && Math.Abs(g.Dy) < 1.5,
            $"switching Slow on moved a finished play's ball {g.Dx},{g.Dy} off the catch");

        // half a play's time in at half speed is the frame full speed draws a
        // quarter of the way in — and that frame has the ball on its way, so
        // the comparison is not two balls sitting still
        var drift = await page.EvaluateAsync<double[]>("""
            async () => {
              const drawn = () => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
              const at = async (slow, part) => {
                S.slow = slow; S.stage = 'run'; S.t0 = performance.now() - S.tl.tEnd * part;
                await drawn();
                return (SC.ball.getAttribute('transform') || '').match(/-?[\d.]+/g).slice(0, 2).map(Number);
              };
              const slow = await at(true, 0.5), full = await at(false, 0.25), snap = await at(false, 0);
              S.slow = true;
              return [Math.hypot(slow[0] - full[0], slow[1] - full[1]), Math.hypot(full[0] - snap[0], full[1] - snap[1])];
            }
            """);
        Assert.True(drift[1] > 20, $"the ball has only moved {drift[1]:0.0} by a quarter of the play, so this compares nothing");
        Assert.True(drift[0] < 8, $"at half speed the ball is {drift[0]:0.0} from where full speed has it at half the time");

        // switching off halfway keeps it halfway, at full speed from there
        await page.EvaluateAsync("() => { S.t0 = performance.now() - S.tl.tEnd; }");
        await page.ClickAsync("#slow");
        var played = await page.EvaluateAsync<double>("(performance.now() - S.t0) / S.tl.tEnd");
        Assert.True(played > 0.45 && played < 0.75, $"switching Slow off jumped the play to {played:0.00} of the way");

        // twice the length: landed
        await page.ClickAsync("#slow");
        await page.EvaluateAsync("() => { S.t0 = performance.now() - 2 * S.tl.tEnd - 100; }");
        await page.WaitForTimeoutAsync(100);
        g = JsonSerializer.Deserialize<Ball>(await page.EvaluateAsync<string>(Measure))!;
        Assert.True(Math.Abs(g.Dx) < 1.5 && Math.Abs(g.Dy) < 1.5,
            $"at half speed, after twice the play, the ball is {g.Dx},{g.Dy} off the catch");

        // it carries to the next play, and the intro puts it back
        await page.EvaluateAsync("openPlay('p_01')");
        Assert.Contains("half speed", await page.InnerTextAsync("#slow"), StringComparison.OrdinalIgnoreCase);
        await page.EvaluateAsync("() => { renderIntro(); openPlay('p_01'); }");
        Assert.Contains("full speed", await page.InnerTextAsync("#slow"), StringComparison.OrdinalIgnoreCase);

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// The rail at its fullest — Line up, Mirror, Replay, Slow and Notes — is
    /// on screen at every size, above the calls and not overlapping.
    /// </summary>
    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task TheRailFitsAtEverySize(string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height), settle: 350);
        await page.ClickAsync(".tile[data-id='p_05']");
        await page.WaitForTimeoutAsync(300);
        var problems = await page.EvaluateAsync<string[]>("""
            () => {
              const out = [], rail = document.querySelector('.rail').getBoundingClientRect();
              const ids = ['stage', 'mirror', 'replay', 'slow', 'notes'];
              let last = -1;
              for (const id of ids){
                const r = document.getElementById(id).getBoundingClientRect();
                if (r.height === 0) out.push(`${id} is hidden`);
                if (r.top < rail.top - 0.5 || r.bottom > rail.bottom + 0.5) out.push(`${id} runs off the rail`);
                if (r.top < last - 0.5) out.push(`${id} overlaps the button above it`);
                last = r.bottom;
              }
              return out;
            }
            """);
        Assert.True(problems.Length == 0, $"{label}:\n" + string.Join("\n", problems));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    private sealed record Snap(
        [property: JsonPropertyName("preOnSnapper")] double PreOnSnapper,
        [property: JsonPropertyName("heldOnThrower")] double HeldOnThrower,
        [property: JsonPropertyName("heldOffCarrier")] double HeldOffCarrier,
        [property: JsonPropertyName("afterOnCarrier")] double AfterOnCarrier);

    /// <summary>
    /// Draw a frame just before the snap, one just after it, and on a run one
    /// after the first exchange, and read where the ball is against the
    /// SNAPPER, the THROWER (the tip of his handoff arrow, or his spot on a
    /// pass) and the carrier. Distances are in viewBox units; a yard is 31.25.
    /// </summary>
    private const string FromTheSnap = """
        () => {
          const tl = S.tl, b = tl.ball, p = S.play, legs = b.legs || [];
          const ball = (t) => {
            S.stage = 'run'; S.t0 = 0; frame(t);
            const m = /translate\(\s*(-?[\d.]+)\s+(-?[\d.]+)\s*\)/.exec(SC.ball.getAttribute('transform') || '');
            return m ? [+m[1], +m[2]] : [9999, 9999];
          };
          const tip = (i, t) => {
            const seg = tl.paths[i];
            const e = walk(p.paths[i].pts, Math.max(0, Math.min(1, (t - seg.start) / seg.dur))).end;
            return W2S(e[0], e[1]);
          };
          const gap = (a, c) => +Math.hypot(a[0]-c[0], a[1]-c[1]).toFixed(1);
          const first = legs[0], run = b.mode === 'carry' && first && first.handoff != null;
          const t0 = tl.tSnap - 100, t1 = tl.tSnap + SNAP + 10;
          const b0 = ball(t0), b1 = ball(t1);
          const thrower = run ? tip(first.handoff, t1) : W2S(p.spots.QB.x, p.spots.QB.y);
          const out = {preOnSnapper: gap(b0, W2S(p.spots.C.x, p.spots.C.y)),
                       heldOnThrower: gap(b1, thrower), heldOffCarrier: 9999, afterOnCarrier: 0};
          if (run){
            const t2 = first.start + 300, b2 = ball(t2);
            out.heldOffCarrier = gap(b1, tip(first.pathIdx, t1));
            out.afterOnCarrier = gap(b2, tip(first.pathIdx, t2));
          }
          return JSON.stringify(out);
        }
        """;

    /// <summary>
    /// The ball starts with the SNAPPER, goes back to the THROWER at the snap,
    /// and on a run is his until he hands it off. It used to appear in the
    /// carrier's hands at the snap, on his own spot, while the THROWER's
    /// handoff arrow drew out empty-handed beside it.
    /// </summary>
    [Theory]
    [InlineData("p_01")]
    [InlineData("f_01")]
    [InlineData("s_01")]
    [InlineData("p_02")]
    [InlineData("p_18")]
    public async Task TheBallStartsWithTheSnapperAndIsTheThrowersUntilHeHandsItOff(string play)
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await OpenAsync(page, play);
        await page.WaitForTimeoutAsync(400);

        var g = JsonSerializer.Deserialize<Snap>(
            await page.EvaluateAsync<string>(FromTheSnap))!;

        Assert.True(g.PreOnSnapper < 3, $"{play}: before the snap the ball is {g.PreOnSnapper} off the SNAPPER");
        Assert.True(g.HeldOnThrower < 3, $"{play}: after the snap the ball is {g.HeldOnThrower} off the THROWER");
        // half a yard, not a whole one: on JET SWEEP the carrier is running
        // past the THROWER at the snap, so he really is that close to the ball
        Assert.True(g.HeldOffCarrier > 15, $"{play}: the carrier has the ball before the handoff ({g.HeldOffCarrier} away)");
        Assert.True(g.AfterOnCarrier < 3, $"{play}: after the handoff the ball is {g.AfterOnCarrier} off the carrier");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    private sealed record Hands(
        [property: JsonPropertyName("legs")] int Legs,
        [property: JsonPropertyName("first")] string First,
        [property: JsonPropertyName("last")] string Last,
        [property: JsonPropertyName("beforeGiver")] double BeforeGiver,
        [property: JsonPropertyName("beforeTaker")] double BeforeTaker,
        [property: JsonPropertyName("afterTaker")] double AfterTaker,
        [property: JsonPropertyName("afterGiver")] double AfterGiver);

    /// <summary>
    /// Draw a frame just before and just after the second exchange, and read
    /// how far the ball is from each of the two runners at both moments. The
    /// distances are in viewBox units; a yard is 31.25 of them.
    /// </summary>
    private const string Exchange = """
        () => {
          const tl = S.tl, b = tl.ball, p = S.play;
          const legs = b.legs || [];
          const kid = (leg, t) => {
            const seg = tl.paths[leg.pathIdx];
            const prog = Math.max(0, Math.min(1, (t - seg.start) / seg.dur));
            const e = walk(p.paths[leg.pathIdx].pts, prog).end;
            return W2S(e[0], e[1]);
          };
          const ball = (t) => {
            S.stage = 'run'; S.t0 = 0; frame(t);
            const m = /translate\(\s*(-?[\d.]+)\s+(-?[\d.]+)\s*\)/.exec(SC.ball.getAttribute('transform') || '');
            return m ? [+m[1], +m[2]] : [9999, 9999];
          };
          const gap = (a, c) => +Math.hypot(a[0]-c[0], a[1]-c[1]).toFixed(1);
          if (legs.length < 2) return JSON.stringify({legs: legs.length, first: '', last: ''});
          const g = legs[0], k = legs[1];
          const t1 = k.start - 150, t2 = k.start + 400;
          const b1 = ball(t1), b2 = ball(t2);
          return JSON.stringify({legs: legs.length, first: g.who, last: k.who,
            beforeGiver: gap(b1, kid(g, t1)), beforeTaker: gap(b1, kid(k, t1)),
            afterTaker: gap(b2, kid(k, t2)), afterGiver: gap(b2, kid(g, t2))});
        }
        """;

    /// <summary>
    /// A reverse used to show the second runner holding the ball from the snap,
    /// because a play had one ball target and the marker rode that kid's whole
    /// path. Now the ball is wherever the kid holding it is, and it changes
    /// hands where the second handoff arrow says it does.
    /// </summary>
    [Theory]
    [InlineData("p_22", "H", "Y")]
    [InlineData("p_25", "Y", "H")]
    [InlineData("p_26", "H", "Z")]
    [InlineData("f_20", "H", "Z")]
    [InlineData("s_20", "H", "Z")]
    public async Task OnAReverseTheBallChangesHandsWhereTheSecondArrowSaysItDoes(
        string play, string giver, string taker)
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await OpenAsync(page, play);
        await page.WaitForTimeoutAsync(400);

        var g = JsonSerializer.Deserialize<Hands>(
            await page.EvaluateAsync<string>(Exchange))!;

        Assert.Equal(2, g.Legs);
        Assert.Equal(giver, g.First);
        Assert.Equal(taker, g.Last);

        // a moment before the exchange the ball is in the first runner's hands,
        // and clearly not in the second's
        Assert.True(g.BeforeGiver < 3, $"{play}: before the exchange the ball is {g.BeforeGiver} off {giver}");
        Assert.True(g.BeforeTaker > 25, $"{play}: before the exchange the ball is already on {taker} ({g.BeforeTaker} away)");

        // and a moment after, the other way round
        Assert.True(g.AfterTaker < 3, $"{play}: after the exchange the ball is {g.AfterTaker} off {taker}");
        Assert.True(g.AfterGiver > 25, $"{play}: after the exchange the ball is still on {giver} ({g.AfterGiver} away)");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    private sealed record Sweep(
        [property: JsonPropertyName("legs")] int Legs,
        [property: JsonPropertyName("runner")] string Runner,
        [property: JsonPropertyName("thrower")] string Thrower,
        [property: JsonPropertyName("midOnRunner")] double MidOnRunner,
        [property: JsonPropertyName("midOffQb")] double MidOffQb,
        [property: JsonPropertyName("midOffTarget")] double MidOffTarget,
        [property: JsonPropertyName("endOnTarget")] double EndOnTarget,
        [property: JsonPropertyName("lineFromSet")] double LineFromSet,
        [property: JsonPropertyName("lineFromQb")] double LineFromQb);

    /// <summary>
    /// Draw a frame halfway through the runner's sweep and one after the
    /// catch, and read where the ball is against the runner, the THROWER's
    /// spot, the receiver, and where the throw line starts. Distances are in
    /// viewBox units; a yard is 31.25 of them.
    /// </summary>
    private const string SweepThenThrow = """
        () => {
          const tl = S.tl, b = tl.ball, p = S.play;
          const legs = b.legs || [];
          if (b.mode !== 'pass' || legs.length === 0)
            return JSON.stringify({legs: legs.length, runner: '', thrower: b.thrower || ''});
          const ball = (t) => {
            S.stage = 'run'; S.t0 = 0; frame(t);
            const m = /translate\(\s*(-?[\d.]+)\s+(-?[\d.]+)\s*\)/.exec(SC.ball.getAttribute('transform') || '');
            return m ? [+m[1], +m[2]] : [9999, 9999];
          };
          const kid = (leg, t) => {
            const seg = tl.paths[leg.pathIdx];
            const prog = Math.max(0, Math.min(1, (t - seg.start) / seg.dur));
            const e = walk(p.paths[leg.pathIdx].pts, prog).end;
            return W2S(e[0], e[1]);
          };
          const gap = (a, c) => +Math.hypot(a[0]-c[0], a[1]-c[1]).toFixed(1);
          const at = pts => { const e = pts[pts.length - 1]; return W2S(e[0], e[1]); };
          const runner = legs[legs.length - 1];
          const qb = W2S(p.spots.QB.x, p.spots.QB.y);
          const set = at(p.paths[runner.pathIdx].pts);
          const target = at(p.paths[b.pathIdx].pts);
          const seg = tl.paths[runner.pathIdx];
          const t1 = seg.start + seg.dur * 0.5, t2 = b.start + b.dur + 50;
          const b1 = ball(t1), b2 = ball(t2);
          const first = (SC.throw.getAttribute('points') || '').trim().split(' ')[0].split(',').map(Number);
          return JSON.stringify({legs: legs.length, runner: runner.who, thrower: b.thrower,
            midOnRunner: gap(b1, kid(runner, t1)), midOffQb: gap(b1, qb), midOffTarget: gap(b1, target),
            endOnTarget: gap(b2, target), lineFromSet: gap(first, set), lineFromQb: gap(first, qb)});
        }
        """;

    /// <summary>
    /// A pass thrown by somebody other than the THROWER: the ball rides the
    /// sweep in the runner's hands, and the throw leaves from where his run
    /// ends, not from the THROWER's spot. Before this the only thrower a play
    /// could have was the kid under centre, so a handoff that turned into a
    /// pass had no honest picture.
    /// </summary>
    [Theory]
    [InlineData("p_27", "Y")]
    [InlineData("f_09", "H")]
    [InlineData("s_14", "F")]
    public async Task OnAJetPassTheBallRidesTheSweepAndLeavesFromWhereTheRunnerStops(string play, string runner)
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await OpenAsync(page, play);
        await page.WaitForTimeoutAsync(400);

        var g = JsonSerializer.Deserialize<Sweep>(
            await page.EvaluateAsync<string>(SweepThenThrow))!;

        Assert.Equal(1, g.Legs);
        Assert.Equal(runner, g.Runner);
        Assert.Equal(runner, g.Thrower);

        // halfway through the sweep the ball is in the runner's hands, and
        // nowhere near the THROWER or the receiver
        Assert.True(g.MidOnRunner < 3, $"mid-sweep the ball is {g.MidOnRunner} off the runner");
        Assert.True(g.MidOffQb > 25, $"mid-sweep the ball is still on the THROWER ({g.MidOffQb} away)");
        Assert.True(g.MidOffTarget > 25, $"mid-sweep the ball is already on the receiver ({g.MidOffTarget} away)");

        // after the throw it is on the receiver, and the throw line starts
        // where the runner stopped, not where the THROWER stood
        Assert.True(g.EndOnTarget < 3, $"after the throw the ball is {g.EndOnTarget} off the receiver");
        Assert.True(g.LineFromSet < 3, $"the throw line starts {g.LineFromSet} from where the runner stopped");
        Assert.True(g.LineFromQb > 25, $"the throw line starts at the THROWER's spot ({g.LineFromQb} away)");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A rollout: the THROWER carries it along his roll and throws from where
    /// it ends. TRIPLE OUT used to throw from the spot he had already left,
    /// with his roll line drawing out empty-handed beside the ball.
    /// </summary>
    [Theory]
    [InlineData("p_14")]
    [InlineData("f_13")]
    [InlineData("s_07")]
    public async Task OnARolloutTheBallRidesTheThrowerAndLeavesFromWhereHeStops(string play)
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await OpenAsync(page, play);
        await page.WaitForTimeoutAsync(400);

        var g = JsonSerializer.Deserialize<Sweep>(
            await page.EvaluateAsync<string>(SweepThenThrow))!;

        Assert.Equal(1, g.Legs);
        Assert.Equal("QB", g.Runner);
        Assert.Equal("QB", g.Thrower);
        Assert.True(g.MidOnRunner < 3, $"mid-roll the ball is {g.MidOnRunner} off the thrower");
        Assert.True(g.MidOffQb > 25, $"mid-roll the ball is still on the spot he left ({g.MidOffQb} away)");
        Assert.True(g.MidOffTarget > 25, $"mid-roll the ball is already on the receiver ({g.MidOffTarget} away)");
        Assert.True(g.EndOnTarget < 3, $"after the throw the ball is {g.EndOnTarget} off the receiver");
        Assert.True(g.LineFromSet < 3, $"the throw line starts {g.LineFromSet} from where the roll ended");
        Assert.True(g.LineFromQb > 25, $"the throw line starts at the spot he left ({g.LineFromQb} away)");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
