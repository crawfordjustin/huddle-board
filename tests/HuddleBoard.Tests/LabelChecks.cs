namespace HuddleBoard.Tests;

/// <summary>
/// Spot names stay on the field, and no letter tag can come back.
/// </summary>
/// <remarks>
/// A kid knows he is WIDE BLUE, not W. Markers carry shape, colour and the
/// spoken name — this was tested at a real practice and the letters did not
/// land. Every play, both mirror states, both stages, five viewports.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class LabelChecks(AppFixture app)
{
    private const string Check = """
        () => {
          // every name must stay inside the playing surface, in both stages and
          // both mirror states, and no letters may survive anywhere on a marker
          const svg = document.getElementById('field');
          const f = svg.getBoundingClientRect();
          const bad = [];
          for (const k in SC.players){
            const P = SC.players[k];
            if (P.tag) bad.push(k + ': still has a letter tag');
            for (const t of [P.l1, P.l2]){
              if (!t.textContent) continue;
              const b = t.getBoundingClientRect();
              if (b.left < f.left - 1 || b.right > f.right + 1 ||
                  b.top < f.top - 1 || b.bottom > f.bottom + 1)
                bad.push(k + ': "' + t.textContent + '" escapes the field');
            }
          }
          return bad;
        }
        """;

    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task NamesStayOnTheFieldAndNoLetterTagsSurvive(string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height), settle: 350);
        var bad = new List<string>();

        var ids = await page.EvaluateAsync<string[]>("DATA.plays.map(p=>p.id)");
        Assert.Equal(HuddleBoard.Playbook.PlayLibrary.All.Count, ids.Length);

        foreach (var id in ids)
        {
            await page.EvaluateAsync($"openPlay('{id}')");
            await page.WaitForTimeoutAsync(90);

            // guards against this whole sweep passing because it looked at nothing
            Assert.Equal(6, await page.EvaluateAsync<int>("Object.keys(SC.players).length"));

            foreach (var mirrored in new[] { false, true })
            {
                if (mirrored)
                    await page.EvaluateAsync("mTarget=1;mAnim=1");

                foreach (var stage in new[] { "lineup", "run" })
                {
                    await page.EvaluateAsync($"S.stage='{stage}'; S.t0=performance.now()-S.tl.tEnd*0.5");
                    await page.WaitForTimeoutAsync(60);
                    var found = await page.EvaluateAsync<string[]>(Check);
                    bad.AddRange(found.Select(x =>
                        $"{id} {stage} {(mirrored ? "mirrored" : "")} {x}"));
                }

                await page.EvaluateAsync("mTarget=0;mAnim=0");
            }
        }

        await page.CloseAsync();
        Assert.True(bad.Count == 0, string.Join("\n", bad.Take(10)));
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
