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
[Collection(AppCollection.Name)]
public sealed class BallChecks(AppFixture app)
{
    private sealed record Ball(
        [property: JsonPropertyName("circles")] int Circles,
        [property: JsonPropertyName("shapes")] int Shapes,
        [property: JsonPropertyName("opacity")] double Opacity,
        [property: JsonPropertyName("rotated")] bool Rotated,
        [property: JsonPropertyName("dx")] double Dx,
        [property: JsonPropertyName("dy")] double Dy);

    /// <summary>Jump the animation to just before it loops, so the ball has arrived.</summary>
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
          return JSON.stringify({
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
    [InlineData("p_01", "a handoff")]
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
        }

        await page.CloseAsync();
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
