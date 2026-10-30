using Microsoft.Playwright;

namespace HuddleBoard.Tests;

/// <summary>The tilted board is a different picture of the same play and timeline.</summary>
public sealed class ThreeDChecks(AppFixture app)
{
    private const string Drawn =
        "() => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)))";

    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task EveryPlayFitsTheTiltedBoard(string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height));
        // Use the wider fallback font, as on an offline tablet.
        await page.AddStyleTagAsync(new() { Content = "svg text{font-family:Arial,sans-serif!important}" });
        var failures = new List<string>();
        foreach (var format in HuddleBoard.Playbook.Formats.All)
        {
            await page.EvaluateAsync($"loadMode({format.Players})");
            var ids = await page.EvaluateAsync<string[]>("DATA.plays.map(p=>p.id)");
            Assert.Equal(format.Plays.Count, ids.Length);
            foreach (var id in ids)
            {
                await page.EvaluateAsync("id => { cfg.view3d=false; openPlay(id); }", id);
                await page.ClickAsync("#view3d");
                Assert.Equal("true", await page.GetAttributeAsync("#view3d", "aria-pressed"));
                foreach (var mirror in new[] { 0, 1 })
                foreach (var stage in new[] { "lineup", "run" })
                {
                    await page.EvaluateAsync("v => { mTarget=mAnim=v.mirror; S.stage=v.stage; S.t0=performance.now()-S.tl.tEnd; }",
                        new { mirror, stage });
                    await page.EvaluateAsync(Drawn);
                    Assert.Equal(format.Players, await page.EvaluateAsync<int>("Object.keys(SC.players).length"));
                    var bad = await page.EvaluateAsync<string[]>("""
                        () => {
                          const bad=[], f=document.getElementById('field').getBoundingClientRect();
                          const inside=(e, box, name) => {
                            const b=e.getBoundingClientRect();
                            if (!b.width || b.left<box.left-1 || b.right>box.right+1 || b.top<box.top-1 || b.bottom>box.bottom+1)
                              bad.push(name+' clipped');
                          };
                          for (const [k,p] of Object.entries(SC.players)){
                            if (!p.shadow || p.tag) bad.push(k+' identity');
                            for (const e of [p.shape,p.l1,p.l2]) if(e===p.shape || e.textContent) inside(e,f,k);
                          }
                          for (const e of document.querySelectorAll('#field [points], #field [transform]'))
                            if (/NaN|Infinity/.test(e.getAttribute('points')||e.getAttribute('transform')||'')) bad.push('invalid projection');
                          const screen={left:0,top:0,right:innerWidth,bottom:innerHeight};
                          for (const e of document.querySelectorAll('.bar button,.rail button:not(.hidden)'))
                            if(e.offsetParent!==null) inside(e,screen,e.id);
                          for (const o of Object.values(SC.oob)) inside(o.badge,f,'sideline');
                          return bad;
                        }
                        """);
                    failures.AddRange(bad.Select(b => $"{id} {stage} mirror={mirror}: {b}"));
                }
            }
        }
        await page.CloseAsync();
        Assert.Empty(errors);
        Assert.True(failures.Count == 0, string.Join("\n", failures.Take(20)));
    }

    [Fact]
    public async Task SwitchingViewsPreservesPlaybackAndStepsAndCarriesToTheNextPlay()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[4]);
        await page.EvaluateAsync("openPlay('p_01')");
        Assert.Equal("false", await page.GetAttributeAsync("#view3d", "aria-pressed"));
        await page.ClickAsync("#replay");
        await page.ClickAsync("#slow");
        var calls = await page.InnerTextAsync("#calls");
        await page.ClickAsync("#mirror");
        await page.WaitForFunctionAsync("mAnim===1");
        Assert.Equal(await page.EvaluateAsync<string>("text => swapWords(text)", calls),
            await page.InnerTextAsync("#calls"));
        var origin = await page.EvaluateAsync<double>("S.t0");
        await page.ClickAsync("#view3d");
        Assert.Equal(origin, await page.EvaluateAsync<double>("S.t0"));
        Assert.True(await page.EvaluateAsync<bool>("S.slow && mTarget===1 && S.stage==='run' && S.ran"));
        await page.EvaluateAsync(Drawn);
        // A real pointer tap at the projected marker, rather than dispatching
        // an event, verifies that the visible marker still owns its hit area.
        var marker = await page.EvaluateAsync<double[]>("() => { const b=SC.players.H.shape.getBoundingClientRect(); return [b.x+b.width/2,b.y+b.height/2]; }");
        await page.Mouse.ClickAsync((float)marker[0], (float)marker[1]);
        Assert.True(await page.EvaluateAsync<bool>("S.solo==='H' && S.step.i===0"));
        await page.ClickAsync("#snext");
        var stepOrigin = await page.EvaluateAsync<double>("S.step.t0");
        await page.ClickAsync("#view3d");
        Assert.Equal(stepOrigin, await page.EvaluateAsync<double>("S.step.t0"));
        Assert.Equal(1, await page.EvaluateAsync<int>("S.step.i"));
        await page.ClickAsync("#view3d");
        await page.ClickAsync("#sagain");
        Assert.Equal(1, await page.EvaluateAsync<int>("S.step.i"));
        await page.ClickAsync("#sback");
        Assert.Equal(0, await page.EvaluateAsync<int>("S.step.i"));
        await page.ClickAsync("#unsolo");
        Assert.True(await page.EvaluateAsync<bool>("!S.solo && !S.steps"));
        // Sideline controls remain interactive on the receding board.
        await page.EvaluateAsync("setOurSide('blue')");
        Assert.True(await page.EvaluateAsync<bool>("""
            () => Object.entries(SC.oob).every(([side,o]) => {
              const m=o.btxt.transform.baseVal.consolidate().matrix;
              const badge=o.badge.transform.baseVal.consolidate().matrix;
              const x=side==='blue' ? -16.85 : 16.85;
              const near=F2S(x,2), far=F2S(x,6);
              // The text's baseline follows the projected sideline; its other
              // axis lies across the ground, rather than standing vertically.
              return Math.abs(m.a*(far[1]-near[1])-m.b*(far[0]-near[0]))<0.01 &&
                     Math.abs(m.d)<0.001 && Math.abs(m.c)>0.1 && m.a*m.d-m.b*m.c>0 &&
                     ['a','b','c','d','e','f'].every(k=>m[k]===badge[k]);
            })
            """));
        var side = await page.EvaluateAsync<double[]>("() => { const b=SC.oob.orange.badge.getBoundingClientRect(); return [b.x+b.width/2,b.y+b.height/2]; }");
        await page.Mouse.ClickAsync((float)side[0], (float)side[1]);
        Assert.Equal("orange", await page.EvaluateAsync<string>("ourSide"));
        await page.SetViewportSizeAsync(800, 1280);
        await page.EvaluateAsync(Drawn);
        Assert.True(await page.EvaluateAsync<bool>("Number.isFinite(SC.board.scale) && cfg.view3d"));
        Assert.True(await page.EvaluateAsync<bool>("Object.values(SC.players).every(p=>p.hit.getBoundingClientRect().width>=43.9)"));
        await page.ClickAsync("#back");
        await page.EvaluateAsync("openPlay('p_05')");
        Assert.Equal("true", await page.GetAttributeAsync("#view3d", "aria-pressed"));
        await page.ClickAsync("#view3d");
        await page.EvaluateAsync("openPlay('p_01')");
        Assert.Equal("false", await page.GetAttributeAsync("#view3d", "aria-pressed"));
        await page.CloseAsync();
        Assert.Empty(errors);
    }

    [Theory]
    [MemberData(nameof(AppFixture.AllSizes), MemberType = typeof(AppFixture))]
    public async Task CrowdedNamesStayClearOfMarkers(string label, int width, int height)
    {
        var (page, errors) = await app.OpenAppAsync(new Viewport(label, width, height));
        await page.AddStyleTagAsync(new() { Content = "svg text{font-family:Arial,sans-serif!important}" });
        foreach (var id in new[] { "f_19", "p_01", "s_19" })
        {
            var players = id[0] switch { 'f' => 5, 's' => 7, _ => 6 };
            await page.EvaluateAsync($"cfg.view3d=false; loadMode({players}); openPlay('{id}')");
            await page.ClickAsync("#view3d");
            await page.ClickAsync("#stage");
            await page.EvaluateAsync(Drawn);
            Assert.Equal(players, await page.EvaluateAsync<int>("Object.values(SC.players).filter(p=>p.l1.textContent).length"));
            var bad = await page.EvaluateAsync<string[]>("""
                () => {
                  const bad=[], labels=[], players=Object.values(SC.players);
                  const markers=players.map(p=>p.shape.getBoundingClientRect());
                  for(const p of players) for(const t of [p.l1,p.l2]){
                    if(!t.textContent) continue;
                    const b=t.getBoundingClientRect();
                    const overlap=a=>Math.min(a.right,b.right)-Math.max(a.left,b.left)>1 &&
                                     Math.min(a.bottom,b.bottom)-Math.max(a.top,b.top)>1;
                    if(markers.some(overlap) || labels.some(a=>a.player!==p && overlap(a.box))) bad.push(t.textContent);
                    labels.push({player:p,box:b});
                  }
                  return bad;
                }
                """);
            Assert.True(bad.Length == 0, $"{id}: {string.Join(", ", bad)} overlaps another marker or name");
        }
        await page.CloseAsync();
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("p_01")]
    [InlineData("p_22")]
    [InlineData("p_27")]
    [InlineData("f_19")]
    [InlineData("s_20")]
    public async Task BallAndGroundKeepTheirMeaningInPerspective(string id)
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0]);
        var players = id[0] switch { 'f' => 5, 's' => 7, _ => 6 };
        await page.EvaluateAsync($"loadMode({players}); openPlay('{id}')");
        await page.ClickAsync("#view3d");
        var bad = await page.EvaluateAsync<string[]>("""
            async () => {
              const bad=[], draw=()=>new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));
              const b=S.tl.ball, times=[0,S.tl.tSnap-1,S.tl.tSnap+1,S.tl.tEnd];
              for(const l of b.legs||[]) times.push(l.start-1,l.start+1);
              if(b.mode==='pass') times.push(b.start-1,b.start+1,b.start+b.dur/2,b.start+b.dur);
              let ground=null;
              for(const mirror of [0,1]){
                mAnim=mTarget=mirror;
                for(const t of times){
                  // Pin the same instant while the actual frame renderer runs.
                  S.step={ta:t,tb:t,t0:performance.now(),ms:600};
                  await draw();
                  if(SC.throw){
                    const below=e=>!!(e.compareDocumentPosition(SC.throw)&Node.DOCUMENT_POSITION_FOLLOWING);
                    if(!Object.values(SC.players).every(p=>below(p.g)) || !SC.paths.every(p=>below(p.line)))
                      bad.push('pass flight covered by ground picture');
                    if(!(SC.throw.compareDocumentPosition(SC.ball)&Node.DOCUMENT_POSITION_FOLLOWING))
                      bad.push('pass flight covers football');
                  }
                  const at=b.mode==='pass' && t>=b.start
                    ? qbez(b.src,b.ctrl,b.dst,Math.max(0,Math.min(1,(t-b.start)/b.dur))) : heldBall(S.play,S.tl,t);
                  const want=W2S(...at), tr=SC.ball.transform.baseVal.getItem(0).matrix;
                  if(Math.hypot(tr.e-want[0],tr.f-want[1])>0.1) bad.push('ball '+t+' mirror '+mirror);
                  if(SC.throw && t>=b.start+b.dur){
                    const end=SC.throw.points.getItem(SC.throw.points.numberOfItems-1);
                    if(Math.hypot(end.x-want[0],end.y-want[1])>0.1) bad.push('throw endpoint');
                  }
                }
                const paint=[SC.base.getAttribute('points'),SC.side.blue.outerHTML,SC.side.orange.outerHTML].join('|');
                if(ground && paint!==ground) bad.push('field mirrored');
                ground=paint;
              }
              return bad;
            }
            """);
        Assert.Empty(bad);
        await page.CloseAsync();
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ViewChoiceSurvivesReloadFormatsAndSync()
    {
        using var site = new StaticSite(HuddleBoard.Playbook.Workspace.Deploy);
        await using var context = await app.Browser.NewContextAsync();
        await AppFixture.MarkTourSeenAsync(context);
        var page = await context.NewPageAsync();
        var errors = new List<string>();
        page.PageError += (_, e) => errors.Add(e);
        await page.GotoAsync(site.Origin + "/index.html");
        await page.ClickAsync("#start");
        await page.EvaluateAsync("openPlay('p_01')");
        Assert.Equal("false", await page.GetAttributeAsync("#view3d", "aria-pressed"));
        await page.ClickAsync("#view3d");
        await page.ClickAsync("#back");
        await page.EvaluateAsync("loadMode(5); openPlay('f_01')");
        Assert.Equal("true", await page.GetAttributeAsync("#view3d", "aria-pressed"));
        await page.ReloadAsync();
        await page.ClickAsync("#start");
        await page.EvaluateAsync("loadMode(7); openPlay('s_01')");
        Assert.Equal("true", await page.GetAttributeAsync("#view3d", "aria-pressed"));
        var exported = await page.EvaluateAsync<string>("setupBlob()");
        await page.ClickAsync("#view3d");
        await page.ReloadAsync();
        await page.ClickAsync("#start");
        await page.EvaluateAsync("openPlay(DATA.plays[0].id)");
        Assert.Equal("false", await page.GetAttributeAsync("#view3d", "aria-pressed"));
        await page.EvaluateAsync("text => { applySetup(readSetup(text)); openPlay(DATA.plays[0].id); }", exported);
        Assert.Equal("true", await page.GetAttributeAsync("#view3d", "aria-pressed"));
        // Legacy files default to 2D without adding a spurious invalid-value
        // report. Explicit malformed values are rejected and reported.
        Assert.True(await page.EvaluateAsync<bool>("""
            text => {
              const old=JSON.parse(text); delete old.settings.view3d;
              const clean=readSetup(JSON.stringify(old)); applySetup(clean);
              if(cfg.view3d!==false || clean.dropped!==0) return false;
              old.settings.view3d='true';
              const bad=readSetup(JSON.stringify(old)); applySetup(bad);
              return cfg.view3d===false && bad.dropped===1;
            }
            """, exported));
        await page.CloseAsync();
        Assert.Empty(errors);
    }

    [Fact]
    public async Task DrawnPlaysAndReducedMotionUseTheSameBoard()
    {
        var (page, errors) = await app.OpenAppAsync(AppFixture.Sizes[0]);
        await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await page.ReloadAsync();
        await page.ClickAsync("#start");
        await page.EvaluateAsync("""
            () => {
              const d=designFrom(DATA.plays.find(p=>p.id==='p_01'));
              d.id='u_3dcheck'; designs[d.id]=d; refreshPlays(); openPlay(d.id);
            }
            """);
        await page.ClickAsync("#view3d");
        await page.ClickAsync("#mirror");
        await page.EvaluateAsync(Drawn);
        Assert.True(await page.EvaluateAsync<bool>("REDUCED && mAnim===1 && in3D() && Object.keys(SC.players).length===6"));
        await page.ClickAsync("#stage");
        Assert.Equal("lineup", await page.EvaluateAsync<string>("S.stage"));
        await page.ClickAsync("#stage");
        await page.EvaluateAsync("S.t0=performance.now()-S.tl.tEnd-100");
        await page.EvaluateAsync(Drawn);
        var held = await page.GetAttributeAsync("#field > g:last-child", "transform");
        await page.EvaluateAsync(Drawn);
        Assert.Equal(held, await page.GetAttributeAsync("#field > g:last-child", "transform"));
        await page.CloseAsync();
        Assert.Empty(errors);
    }
}
