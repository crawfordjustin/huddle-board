using System.Text.Json.Serialization;

namespace HuddleBoard.Tests;

/// <summary>
/// OUR SIDE / THEIR SIDE stays centred on its band, the band bleeds to the edge
/// of the field, and tapping the far band reassigns which side is ours.
/// </summary>
/// <remarks>
/// The sidelines are the one landmark that holds for a whole game — parents move
/// for the sun, teammates move because only six are on the field.
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class SidelineChecks(AppFixture app)
{
    private sealed record BandGeometry(
        [property: JsonPropertyName("off")] double BadgeOffset,
        [property: JsonPropertyName("loff")] double LabelOffset,
        [property: JsonPropertyName("edge")] double EdgeGap,
        [property: JsonPropertyName("vtop")] double TopGap,
        [property: JsonPropertyName("vbot")] double BottomGap);

    private const string Measure = """
        () => {
          const o={}; const wrap=document.querySelector('.fieldwrap').getBoundingClientRect();
          for (const nm of ["blue","orange"]){
            const s=SC.oob[nm].r.getBoundingClientRect();
            const t=SC.oob[nm].btxt.getBoundingClientRect();
            const l=SC.oob[nm].lbl.getBoundingClientRect();
            o[nm]={off:+(((t.left+t.right)/2)-((s.left+s.right)/2)).toFixed(2),
                   loff:+(((l.left+l.right)/2)-((s.left+s.right)/2)).toFixed(2),
                   edge:+(nm==="blue"? s.left-wrap.left : wrap.right-s.right).toFixed(2),
                   vtop:+(s.top-wrap.top).toFixed(2), vbot:+(wrap.bottom-s.bottom).toFixed(2)};
          }
          return o;}
        """;

    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task BandsAreCentredBleedToTheEdgeAndCanBeSwapped(string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height), settle: 350);
        await page.EvaluateAsync("openPlay('p_01')");
        await page.WaitForTimeoutAsync(650);

        var bands = await page.EvaluateAsync<Dictionary<string, BandGeometry>>(Measure);
        foreach (var (side, g) in bands)
        {
            Assert.True(Math.Abs(g.BadgeOffset) < 0.6, $"{side} badge is off-centre by {g.BadgeOffset}");
            Assert.True(Math.Abs(g.LabelOffset) < 0.6, $"{side} label is off-centre by {g.LabelOffset}");
            Assert.True(Math.Abs(g.EdgeGap) < 1.5, $"{side} band stops {g.EdgeGap} short of the edge");
            Assert.True(Math.Abs(g.TopGap) < 1.5, $"{side} band stops {g.TopGap} short of the top");
            Assert.True(Math.Abs(g.BottomGap) < 1.5, $"{side} band stops {g.BottomGap} short of the bottom");
        }

        // tapping the far band must reassign our side
        var before = await page.EvaluateAsync<string>("ourSide");
        await page.EvaluateAsync("""
            () => { const nm = ourSide==='blue'?'orange':'blue';
                    SC.oob[nm].g.dispatchEvent(new MouseEvent('click',{bubbles:true})); }
            """);
        await page.WaitForTimeoutAsync(250);
        var after = await page.EvaluateAsync<string>("ourSide");

        await page.CloseAsync();
        Assert.NotEqual(before, after);
        Assert.True(errors.Count == 0, string.Join("\n", errors));
    }
}
