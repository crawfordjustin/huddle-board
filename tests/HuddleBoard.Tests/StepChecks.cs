using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuddleBoard.Tests;

/// <summary>
/// Tapping a kid shows his job one step at a time: line up, the snap if it is
/// his, then each leg of his route and each handoff, throw and catch, with
/// Next on the rail and his marker running the step.
/// </summary>
/// <remarks>
/// Before this a tap faded everyone else and put his written job under the
/// field, which is a paragraph for the coach to read out. The steps are worked
/// out from the play's own timeline, so every shipped play and every drawn
/// one has them, and a leg is named in the nine shapes — the vocabulary check
/// here is rule 1 reaching a screen that writes its own words.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class StepChecks(AppFixture app)
{
    private static Task OpenAsync(Microsoft.Playwright.IPage page, string play)
    {
        var players = play[0] switch { 'f' => 5, 's' => 7, _ => 6 };
        return page.EvaluateAsync($"loadMode({players}); openPlay('{play}')");
    }

    private static Task TapAsync(Microsoft.Playwright.IPage page, string key) =>
        page.EvaluateAsync("k => SC.players[k].g.dispatchEvent(new MouseEvent('click', {bubbles: true}))", key);

    /// <summary>Finish the step that is playing, and let a frame draw it.</summary>
    private static async Task SettleStepAsync(Microsoft.Playwright.IPage page)
    {
        await page.EvaluateAsync("() => { S.step.t0 = performance.now() - S.step.ms - 10; }");
        await page.WaitForTimeoutAsync(120);
    }

    private sealed record Panel(
        [property: JsonPropertyName("steps")] string[] Steps,
        [property: JsonPropertyName("now")] string Now,
        [property: JsonPropertyName("next")] bool Next,
        [property: JsonPropertyName("stage")] bool Stage,
        [property: JsonPropertyName("replay")] bool Replay,
        [property: JsonPropertyName("text")] string Text);

    private const string ReadPanel = """
        () => {
          const shown = id => { const e = document.getElementById(id); return !!e && e.offsetParent !== null; };
          return JSON.stringify({
            steps: [...document.querySelectorAll('#calls .spill')].map(b => b.textContent.replace(/^\d+/, '')),
            now: (document.querySelector('#calls .spill.now') || {}).textContent || '',
            next: shown('snext'), stage: shown('stage'), replay: shown('replay'),
            text: (document.querySelector('#calls .snow span') || {}).textContent || ''});
        }
        """;

    [Theory]
    [InlineData("p_01", "H", "LINE UP|TAKE IT|CARRY|CARRY")]
    [InlineData("p_01", "C", "LINE UP|SNAP|GO")]
    [InlineData("p_01", "QB", "LINE UP|SNAP|HAND OFF")]
    [InlineData("p_05", "Y", "LINE UP|GO|IN")]
    [InlineData("p_14", "QB", "LINE UP|SNAP|THROW")]
    [InlineData("p_27", "Y", "LINE UP|MOTION|TAKE IT|THROW")]
    [InlineData("f_01", "H", "LINE UP|TAKE IT|CARRY|CARRY")]
    [InlineData("s_01", "H", "LINE UP|TAKE IT|CARRY|CARRY")]
    public async Task TappingAKidShowsHisStepsAndTheRailSteps(string play, string key, string heads)
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await OpenAsync(page, play);
        await TapAsync(page, key);
        await page.WaitForTimeoutAsync(150);

        var g = JsonSerializer.Deserialize<Panel>(await page.EvaluateAsync<string>(ReadPanel))!;
        Assert.Equal(heads, string.Join("|", g.Steps));
        Assert.Contains("LINE UP", g.Now);
        Assert.True(g.Next, $"{play} {key}: Next is not on the rail");
        Assert.False(g.Stage, $"{play} {key}: Line up / Run it is still on the rail during the steps");
        Assert.False(g.Replay, $"{play} {key}: Replay is still on the rail during the steps");

        // Next walks to the end, and is shut there
        for (var i = 1; i < g.Steps.Length; i++) await page.ClickAsync("#snext");
        g = JsonSerializer.Deserialize<Panel>(await page.EvaluateAsync<string>(ReadPanel))!;
        Assert.Contains(g.Steps[^1], g.Now);
        Assert.True(await page.IsDisabledAsync("#snext"), $"{play} {key}: Next is open on the last step");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// A receiver's catch is the end of his last leg's sentence. It used to be
    /// a step of its own starting at the throw, and on a slant the ball is in
    /// the air for the whole IN, so the IN vanished from his steps.
    /// </summary>
    [Fact]
    public async Task AReceiversLastLegKeepsItsShapeAndEndsInTheCatch()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await OpenAsync(page, "p_05");
        await TapAsync(page, "Y");
        await page.EvaluateAsync("() => goStep(S.steps.length - 1)");
        var g = JsonSerializer.Deserialize<Panel>(await page.EvaluateAsync<string>(ReadPanel))!;
        Assert.Contains("IN", g.Now);
        Assert.Contains("Catch it", g.Text);

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    private sealed record Run(
        [property: JsonPropertyName("startOnSpot")] double StartOnSpot,
        [property: JsonPropertyName("endOnRouteEnd")] double EndOnRouteEnd,
        [property: JsonPropertyName("ballOnHim")] double BallOnHim,
        [property: JsonPropertyName("othersMoved")] double OthersMoved);

    /// <summary>
    /// His marker runs the step; everybody else stays on his spot. Read on the
    /// first step and after the last, and against every other marker, in
    /// viewBox units (a yard is 31.25 of them), mirrored as well.
    /// </summary>
    [Theory]
    [InlineData("p_01", "H", false)]
    [InlineData("p_05", "Y", true)]
    public async Task HisMarkerRunsTheStepsAndNobodyElseMoves(string play, string key, bool mirrored)
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await OpenAsync(page, play);
        if (mirrored) await page.EvaluateAsync("() => { mTarget = 1; mAnim = 1; }");
        await TapAsync(page, key);
        await page.WaitForTimeoutAsync(150);
        const string where = """
            k => { const m = /translate\(\s*(-?[\d.]+)\s+(-?[\d.]+)\s*\)/.exec(SC.players[k].g.getAttribute('transform'));
                   return [+m[1], +m[2]]; }
            """;
        var start = await page.EvaluateAsync<double[]>(where, key);
        await page.EvaluateAsync("() => goStep(S.steps.length - 1)");
        await SettleStepAsync(page);

        var r = JsonSerializer.Deserialize<Run>(await page.EvaluateAsync<string>($$"""
            ([k, s]) => {
              const at = {{where}};
              const gap = (a, c) => +Math.hypot(a[0]-c[0], a[1]-c[1]).toFixed(1);
              const p = S.play, sp = p.spots[k];
              const path = p.paths[p.paths.findIndex(x => x.who === k && x.type !== 'motion' && x.type !== 'handoff')];
              const z = path.pts[path.pts.length - 1];
              const m = /translate\(\s*(-?[\d.]+)\s+(-?[\d.]+)\s*\)/.exec(SC.ball.getAttribute('transform') || '');
              let moved = 0;
              for (const o in p.spots) if (o !== k)
                moved = Math.max(moved, gap(at(o), W2S(p.spots[o].x, p.spots[o].y)));
              return JSON.stringify({startOnSpot: gap(s, W2S(sp.x, sp.y)), endOnRouteEnd: gap(at(k), W2S(z[0], z[1])),
                ballOnHim: m ? gap([+m[1], +m[2]], at(k)) : 9999, othersMoved: moved});
            }
            """, new object[] { key, start }))!;

        Assert.True(r.StartOnSpot < 1.5, $"{play} {key}: on LINE UP he is {r.StartOnSpot} off his spot");
        Assert.True(r.EndOnRouteEnd < 1.5, $"{play} {key}: after the last step he is {r.EndOnRouteEnd} off the end of his route");
        Assert.True(r.BallOnHim < 3, $"{play} {key}: after the last step the ball is {r.BallOnHim} off him");
        Assert.True(r.OthersMoved < 1.5, $"{play} {key}: another kid moved {r.OthersMoved} off his spot");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// All puts the play back the way the tap found it: the calls, the rail's
    /// Line up and Replay, and the finished picture.
    /// </summary>
    [Fact]
    public async Task AllLeavesTheStepsAndPutsThePlayBack()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        await OpenAsync(page, "p_01");
        await TapAsync(page, "H");
        await page.ClickAsync("#snext");
        await page.ClickAsync("#unsolo");
        await page.WaitForTimeoutAsync(150);

        var g = JsonSerializer.Deserialize<Panel>(await page.EvaluateAsync<string>(ReadPanel))!;
        Assert.Empty(g.Steps);
        Assert.False(g.Next, "Next is still on the rail");
        Assert.True(g.Stage, "Line up is not back on the rail");
        Assert.True(g.Replay, "Replay is not back on the rail");
        Assert.True(await page.EvaluateAsync<bool>("() => S.step === null && S.solo === null && S.stage === 'run'"));
        Assert.True(await page.IsVisibleAsync("#calls .call"), "the call strip is not back");

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// Every step of every kid in every play of all three formats is named in
    /// the nine shapes or one of the app's action words, and every kid has
    /// something after lining up. Counted, so the sweep cannot pass by finding
    /// no kids at all.
    /// </summary>
    [Fact]
    public async Task EveryStepIsNamedInTheVocabulary()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0], settle: 350);
        var result = await page.EvaluateAsync<string>("""
            () => {
              const ok = new Set(['GO','OUT','IN','SIT','CORNER','POST','WHEEL','SWING','CARRY',
                                  'LINE UP','MOTION','SNAP','TAKE IT','HAND OFF','THROW','CATCH','FAKE']);
              const bad = [], idle = [];
              let kids = 0;
              for (const n of [5, 6, 7]){
                loadMode(n);
                for (const p of DATA.plays){
                  const tl = buildTimeline(p);
                  for (const k in p.spots){
                    kids++;
                    const s = kidSteps(p, tl, k);
                    s.filter(x => !ok.has(x.head)).forEach(x => bad.push(`${p.id} ${k} ${x.head}`));
                    if (s.length < 2) idle.push(`${p.id} ${k}`);
                  }
                }
              }
              return JSON.stringify({kids, bad, idle});
            }
            """);
        using var doc = JsonDocument.Parse(result);
        var kids = doc.RootElement.GetProperty("kids").GetInt32();
        var bad = doc.RootElement.GetProperty("bad").EnumerateArray().Select(x => x.GetString()).ToList();
        var idle = doc.RootElement.GetProperty("idle").EnumerateArray().Select(x => x.GetString()).ToList();

        Assert.True(kids > 400, $"the sweep only saw {kids} kids");
        Assert.True(bad.Count == 0, "steps outside the vocabulary:\n" + string.Join("\n", bad));
        Assert.True(idle.Count == 0, "kids with nothing after LINE UP:\n" + string.Join("\n", idle));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }

    /// <summary>
    /// The busiest kid in the library — six steps — at every size: the panel
    /// is on screen and nothing in it is clipped, every step's name is whole,
    /// and the rail's four buttons are on screen.
    /// </summary>
    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task TheStepsFitAtEverySize(string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height), settle: 350);
        await OpenAsync(page, "f_07");
        await TapAsync(page, "H");
        await page.EvaluateAsync("() => goStep(S.steps.length - 1)");
        await page.WaitForTimeoutAsync(200);

        var result = await page.EvaluateAsync<string>("""
            () => {
              const vh = innerHeight, vw = innerWidth, out = [];
              const calls = document.getElementById('calls').getBoundingClientRect();
              if (calls.bottom > vh + 0.5) out.push(`the panel runs ${calls.bottom - vh}px off the bottom`);
              const steps = document.querySelector('#calls .steps');
              if (!steps) out.push('no steps panel');
              else if (steps.scrollHeight > steps.clientHeight + 1) out.push('the steps panel is clipped');
              const pills = [...document.querySelectorAll('#calls .spill')];
              pills.filter(b => b.scrollWidth > b.clientWidth + 1).forEach(b => out.push(`step "${b.textContent}" is cut off`));
              for (const id of ['snext', 'sback', 'sagain', 'mirror']){
                const r = document.getElementById(id).getBoundingClientRect();
                if (r.height === 0 || r.bottom > vh + 0.5 || r.right > vw + 0.5) out.push(`${id} is not on screen`);
              }
              const field = document.getElementById('field').getBoundingClientRect();
              if (field.height < vh * 0.4) out.push(`the field is only ${Math.round(field.height)}px of ${vh}`);
              return JSON.stringify({pills: pills.length, out});
            }
            """);
        using var doc = JsonDocument.Parse(result);
        Assert.Equal(6, doc.RootElement.GetProperty("pills").GetInt32());
        var problems = doc.RootElement.GetProperty("out").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.True(problems.Count == 0, $"{label}:\n" + string.Join("\n", problems));

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
