"""Builds the Coach Mode mockup: original.html (the live app) + coach.js -> index.html."""
import pathlib

here = pathlib.Path(__file__).parent
s = (here / "original.html").read_text()
coach = (here / "coach.js").read_text() + "\n" + (here / "view3d.js").read_text() + "\n" + (here / "field.js").read_text()


def sub(old, new):
    global s
    n = s.count(old)
    assert n == 1, f"expected 1 match, found {n}: {old[:70]!r}"
    s = s.replace(old, new)


CSS = """
/* ---------------------------------------------------------------- coach mode (mockup) */
.rbtn.coachbtn{background:var(--ball);border-color:#C99A00;color:var(--ink)}
.rbtn.coachbtn small{color:#5A4A12}
.rbtn[disabled]{opacity:.38}
.coachp{flex:1;min-width:0;display:flex;flex-direction:column;gap:clamp(6px,1vh,10px)}
.cwho{display:flex;gap:6px;flex-wrap:wrap;align-items:center}
.cwho .lab{font-family:var(--disp);letter-spacing:.12em;color:var(--ink-2);
  font-size:clamp(12px,1.7vh,15px);text-transform:uppercase;margin-right:4px}
.cwho button{font-family:var(--disp);text-transform:uppercase;font-size:clamp(13px,1.9vh,17px);
  letter-spacing:.04em;padding:6px 12px;border-radius:8px;border:1.5px solid var(--hair);
  background:var(--ground);min-height:40px}
.cwho button.on{background:var(--ink);border-color:var(--ink);color:#fff}
.cstep{display:flex;align-items:center;gap:clamp(10px,1.4vw,18px);border-left:5px solid var(--ball);
  padding-left:clamp(8px,1vw,14px)}
.cnum{flex:none;display:flex;flex-direction:column;align-items:center;font-family:var(--disp);
  font-size:clamp(30px,5.4vh,52px);line-height:.95;min-width:62px}
.cnum small{font-family:var(--body);font-size:clamp(9px,1.3vh,12px);letter-spacing:.12em;
  text-transform:uppercase;color:var(--ink-2);font-weight:700}
.ctext{flex:1;min-width:0}
.ctext i{display:block;font-style:normal;font-family:var(--disp);letter-spacing:.1em;
  font-size:clamp(12px,1.8vh,16px);text-transform:uppercase}
.ctext b{display:block;font-family:var(--disp);font-size:clamp(24px,4.4vh,42px);
  text-transform:uppercase;line-height:1}
.ctext span{display:block;font-size:clamp(14px,2.4vh,22px);color:var(--ink);line-height:1.2;margin-top:3px}
.ctext em{display:block;font-style:normal;font-size:clamp(11px,1.7vh,15px);color:var(--ink-2);
  margin-top:5px}
.cstep{flex-wrap:wrap;border-left:none;padding-left:0;gap:clamp(6px,1vh,10px)}
.ctrack{flex:0 0 100%;display:flex;gap:6px}
.cpill{flex:1;min-width:0;display:flex;align-items:center;gap:8px;padding:6px 10px;border-radius:10px;
  border:2px solid var(--hair);background:var(--surface);color:#8B978D;font-family:var(--disp);
  text-transform:uppercase;font-size:clamp(14px,2.1vh,20px);letter-spacing:.03em;white-space:nowrap;
  overflow:hidden;text-overflow:ellipsis;min-height:44px}
.cpill i{flex:none;font-style:normal;width:1.5em;height:1.5em;border-radius:50%;display:grid;place-items:center;
  background:var(--ground);color:#8B978D;font-size:.9em}
.cpill.now{background:var(--ball);border-color:#C99A00;color:var(--ink)}
.cpill.now i{background:var(--ink);color:#fff}
.cpill.done{color:var(--green);border-color:#BFD9C9}
.cpill.done i{background:var(--green);color:#fff}
.ctext{flex:0 0 100%}
.ctext b{font-size:clamp(22px,3.8vh,36px)}
.ctext span{font-size:clamp(18px,3vh,28px);font-weight:700;line-height:1.15;margin-top:1px}
.ctext span span,.ctext em span{font-size:inherit;display:inline;margin:0}
.ctext em strong{color:var(--ink);font-weight:700}
/* play controls: big, square-ish tiles a coach can hit without looking —
   icon on top, a short word under it, Next the one green tile */
.cnav{flex:0 0 100%;display:flex;gap:clamp(8px,1vw,14px);justify-content:center;padding-top:2px}
.cnav .rbtn.ico{flex:0 0 auto;width:clamp(88px,9.5vw,132px);min-height:clamp(62px,9vh,84px);
  padding:6px 6px;gap:4px;border-radius:14px;border:2px solid var(--hair);background:var(--surface);
  color:var(--ink);box-shadow:0 2px 0 var(--hair)}
.cnav .rbtn.ico svg{width:clamp(26px,3.8vh,36px);height:clamp(26px,3.8vh,36px)}
.cnav .rbtn.ico small{font-family:var(--disp);font-size:clamp(11px,1.6vh,15px);font-weight:600;
  letter-spacing:.12em;color:var(--ink);line-height:1}
.cnav .rbtn.ico.go{width:clamp(124px,13vw,184px);background:var(--green);border-color:var(--green-2);
  color:#fff;box-shadow:0 2px 0 var(--green-2)}
.cnav .rbtn.ico.go small{color:#fff}
.cnav .rbtn.ico.on{background:var(--ink);border-color:var(--ink);color:#fff;box-shadow:none}
.cnav .rbtn.ico.on small{color:#fff}
.cnav .rbtn.ico:active{transform:translateY(2px);box-shadow:none}
.cnav .rbtn.ico[disabled]{opacity:.35;box-shadow:none}

/* the instruction panel goes dark, the way the picture has it: the field
   stays the bright thing on screen, and the words sit on black */
.calls.cdark{background:#141A16;border-top-color:#2A322C;color:#F2F4F1}
.cdark .cb{color:#4C9BFF} .cdark .co{color:#FF8A3D}
.cdark .cwho .lab{color:#C9D1CB}
.cdark .cwho button{background:#1C231F;border-color:#39433C;color:#F2F4F1}
.cdark .cwho button.on{background:#141A16;border-color:#F2F4F1;color:#fff}
.cdark .cwho button.cb{color:#4C9BFF} .cdark .cwho button.co{color:#FF8A3D}
.cdark .cpill{background:#1C231F;border-color:#39433C;color:#C9D1CB}
.cdark .cpill i{background:#4A554E;color:#F2F4F1}
.cdark .cpill.done{background:#1B5E3A;border-color:#2A7B50;color:#fff}
.cdark .cpill.done i{background:#2FB36A;color:#fff}
.cdark .cpill.now{background:var(--ball);border-color:var(--ball);color:#141A16}
.cdark .cpill.now i{background:#141A16;color:#fff}
/* both modes: side colours for the Who chips, the picture slot, the job box */
.cb{color:var(--blue)} .co{color:var(--orange)}
/* the no-run-zone warning: a red bar across the top of the field, above the
   play, whenever a run is open with the ball in a no-run zone */
.nrzwarn{position:absolute;left:50%;top:10px;transform:translate(-50%,-8px);z-index:3;display:flex;
  align-items:center;gap:12px;max-width:calc(100% - 32px);padding:8px 16px 8px 12px;border-radius:10px;
  background:#C0392B;color:#fff;box-shadow:0 4px 14px rgba(0,0,0,.25);opacity:0;pointer-events:none;
  transition:opacity .2s ease,transform .2s ease}
.nrzwarn.show{opacity:1;transform:translate(-50%,0)}
.nrzwarn b{flex:none;font-family:var(--disp);text-transform:uppercase;letter-spacing:.08em;
  font-size:clamp(16px,2.4vh,22px);background:#fff;color:#C0392B;border-radius:6px;padding:3px 9px}
.nrzwarn span{font-size:clamp(13px,2vh,18px);font-weight:600;line-height:1.2}
/* real-field toggle and the ball spot, in the top bar */
.rfspot{display:flex;align-items:center;gap:4px;border:1.5px solid #3B443E;border-radius:9px;padding:2px}
.rfspot.hidden{display:none}
.rfspot button{width:40px;min-height:40px;border-radius:7px;background:#242B26;color:#fff;border:none;
  font-size:24px;font-weight:700;line-height:1}
.rfspot span{min-width:86px;text-align:center;font-family:var(--disp);text-transform:uppercase;color:#fff;
  font-size:clamp(15px,2.2vh,20px);line-height:1;display:flex;flex-direction:column;align-items:center}
.rfspot span small{font-family:var(--body);font-size:9px;letter-spacing:.14em;color:#98A49B;font-weight:700}
/* 3D view toggle in the top bar */
.v3d{font-family:var(--disp);text-transform:uppercase;font-size:clamp(16px,2.4vh,22px);letter-spacing:.06em;
  color:#E4E9E5;background:transparent;border:1.5px solid #3B443E;border-radius:9px;padding:4px 14px;
  min-height:44px;display:flex;flex-direction:column;align-items:center;justify-content:center;line-height:1}
.v3d small{font-family:var(--body);font-size:9px;letter-spacing:.14em;color:#98A49B;font-weight:700}
.v3d.on{background:var(--ball);border-color:var(--ball);color:#141A16}
.v3d.on small{color:#5A4A12}
.cwho button.on.cb,.cwho button.on.co{color:#fff}
.cart{flex:0 0 clamp(150px,16vw,240px);aspect-ratio:16/10;border-radius:12px;border:2px dashed var(--hair);
  background:var(--ground);color:#8B978D;display:flex;flex-direction:column;align-items:center;
  justify-content:center;gap:2px;align-self:center}
.cart b{font-family:var(--disp);font-size:clamp(13px,1.9vh,17px);letter-spacing:.12em;text-transform:uppercase}
.cart small{font-size:clamp(10px,1.4vh,12px);letter-spacing:.1em;text-transform:uppercase}
.cjob{border:1.5px solid var(--hair);background:var(--ground)}
.cjob h4{color:var(--ink-2)}
.cjob li{color:var(--ink)}
.cdark .cart{background:#1C231F;border-color:#4A554E;color:#8B978D}
.crow{flex:0 0 100%;display:flex;gap:clamp(12px,2vw,28px);align-items:stretch}
/* Coach Mode panel: the same height whatever it shows, so the field above it
   keeps its size when the coach switches players or steps. The middle row
   takes what is left; a long job list scrolls inside its box. */
.calls.coachon{height:clamp(300px,44vh,420px);min-height:0;overflow:hidden}
.calls.coachon .coachp{height:100%;min-height:0}
.calls.coachon .cstep{flex:1;min-height:0;flex-direction:column;flex-wrap:nowrap;align-items:stretch}
.calls.coachon .ctrack,.calls.coachon .cnav{flex:none}
.calls.coachon .crow{flex:1 1 auto;min-height:0}
.calls.coachon .ctext{overflow:hidden}
.calls.coachon .cjob{overflow-y:auto;min-height:0}
.calls.coachon .cart{max-height:100%}
.crow .ctext{flex:1;min-width:0}
.cdark .ctext b{font-size:clamp(30px,5.4vh,56px);color:#fff;letter-spacing:.01em}
.cdark .ctext span{color:#F2F4F1}
.cjob{flex:0 0 clamp(260px,32%,420px);border-radius:12px;
  padding:clamp(8px,1.2vh,14px) clamp(12px,1.4vw,18px)}
.cdark .cjob{border-color:#39433C;background:#171D19}
.cdark .cjob h4{color:#A9B5AD}
.cdark .cjob li{color:#E4E9E5}
.cjob h4{margin:0 0 6px;font-family:var(--disp);font-weight:600;font-size:clamp(11px,1.6vh,14px);
  letter-spacing:.14em;text-transform:uppercase}
.cjob ol{list-style:none;margin:0;padding:0;display:flex;flex-direction:column;gap:clamp(3px,.6vh,6px)}
.cjob li{display:flex;align-items:flex-start;gap:9px;font-size:clamp(12px,1.75vh,16px);line-height:1.25}
.cjob li i{flex:none;font-style:normal;width:1.45em;height:1.45em;border-radius:50%;background:var(--ball);
  color:#141A16;font-weight:800;font-size:.85em;display:grid;place-items:center;margin-top:.05em}
.cjob li span{display:inline}
.cdark .cnav .rbtn.ico{background:#1C231F;border-color:#39433C;color:#F2F4F1;box-shadow:0 2px 0 #0C100E}
.cdark .cnav .rbtn.ico small{color:#F2F4F1}
.cdark .cnav .rbtn.ico.go{background:#1F8A50;border-color:#2FB36A;color:#fff;box-shadow:0 2px 0 #0F4A2B}
.cdark .cnav .rbtn.ico.on{background:#F2F4F1;border-color:#F2F4F1;color:#141A16}
.cdark .cnav .rbtn.ico.on small{color:#141A16}
/* end zone colour swatches on Settings */
.ezsw{display:inline-block;width:14px;height:14px;border-radius:3px;margin-right:6px;vertical-align:-2px;
  border:1px solid rgba(0,0,0,.25)}
/* ---------------------------------------------------------------- player focus (mockup) */
.play.pfocus{background:#0E1210}
.play.pfocus>.bar,.play.pfocus .rail,.play.pfocus .hint{display:none!important}
.pfhead{flex:none;display:flex;align-items:center;justify-content:space-between;gap:16px;
  padding:clamp(8px,1.4vh,14px) clamp(16px,2.4vw,32px);background:#0E1210;color:#fff}
.pfwho{display:flex;align-items:center;gap:clamp(10px,1.2vw,16px);min-width:0}
.pfmark{height:clamp(36px,5.4vh,52px);width:auto;flex:none}
.sicon{display:block;width:100%;height:100%;background:currentColor;
  -webkit-mask:var(--i) center/contain no-repeat;mask:var(--i) center/contain no-repeat}
.pftab .pfti{width:clamp(30px,5vh,46px);height:clamp(30px,5vh,46px);flex:none}
.pfwho b{font-family:var(--disp);font-size:clamp(26px,4.6vh,44px);letter-spacing:.02em;text-transform:uppercase}
.pfwho i{width:2px;align-self:stretch;margin:6px 2px;background:#3A433D}
.pfwho span{font-family:var(--disp);font-size:clamp(24px,4.2vh,40px);color:#AEB8B0;text-transform:uppercase;white-space:nowrap}
.pfexit{display:flex;align-items:center;gap:10px;padding:10px 18px;border-radius:10px;border:1.5px solid #3A433D;
  background:#141A16;color:#fff;font-family:var(--disp);font-size:clamp(15px,2.3vh,22px);letter-spacing:.04em;text-transform:uppercase}
.pfexit svg{width:22px;height:22px;fill:none;stroke:currentColor;stroke-width:2.6;stroke-linecap:round}
.calls.pfpanel{background:#0E1210;color:#fff;border-top:0;padding:clamp(8px,1.4vh,14px) clamp(16px,2.4vw,32px) clamp(10px,1.8vh,18px);
  height:clamp(270px,37vh,380px);min-height:0;overflow:hidden}
.pf{flex:1;display:flex;flex-direction:column;gap:clamp(8px,1.6vh,16px);min-width:0;min-height:0}
.pftabs{flex:none;display:flex;gap:clamp(6px,.9vw,12px)}
.pftab{flex:1;min-width:0;display:flex;align-items:center;justify-content:center;gap:clamp(8px,1vw,14px);
  padding:clamp(6px,1vh,10px) 8px;border-radius:8px;border:1.5px solid #39433C;background:#171D19;color:#fff;opacity:.62}

.pftl{display:flex;flex-direction:column;align-items:center;gap:3px;min-width:0}
.pftl i{font-style:normal;width:clamp(24px,3.6vh,32px);height:clamp(24px,3.6vh,32px);border-radius:50%;display:grid;place-items:center;
  background:#59625C;color:#fff;font-family:var(--disp);font-size:clamp(14px,2.2vh,20px)}
.pftl b{font-family:var(--disp);font-weight:600;font-size:clamp(13px,2.1vh,19px);text-transform:uppercase;letter-spacing:.03em;
  white-space:nowrap;overflow:hidden;text-overflow:ellipsis;max-width:100%}
.pftab.now{background:#FFC800;border-color:#FFC800;color:#111;opacity:1}
.pftab.now .pftl i{background:#111;color:#fff}
.pftab.done{background:#0F3D25;border-color:#1C6A40;opacity:1}
.pftab.done .pftl i{background:#2FB36A}
.pftab .cut{stroke:#171D19} .pftab.now .cut{stroke:#FFC800} .pftab.done .cut{stroke:#0F3D25}
.pftab .cutf{fill:#171D19} .pftab.now .cutf{fill:#FFC800} .pftab.done .cutf{fill:#0F3D25}
.pfrow{flex:1 1 auto;min-height:0;display:flex;align-items:center;gap:clamp(18px,3vw,48px);padding-left:clamp(8px,2vw,40px)}
.pfbig{flex:none;width:clamp(96px,17vh,170px);height:clamp(96px,17vh,170px);border-radius:50%;background:#FFC800;color:#111;
  display:grid;place-items:center}
.pfbig .sicon{width:56%;height:56%}
.pfbig .cut{stroke:#FFC800} .pfbig .cutf{fill:#FFC800}
.pftext{min-width:0}
.pftext b{display:block;font-family:var(--disp);font-size:clamp(40px,8vh,76px);line-height:.95;text-transform:uppercase;letter-spacing:.01em}
.pftext span{display:block;font-size:clamp(20px,3.6vh,34px);line-height:1.2;margin-top:6px;color:#F2F4F1;max-width:34ch}
.pftext span span{display:inline;margin:0;font-size:inherit;font-weight:700}
.pfnav{flex:none;display:flex;gap:clamp(12px,2vw,30px);justify-content:center}
.pfnav button{flex:0 1 clamp(200px,28vw,440px);display:flex;align-items:center;justify-content:center;gap:14px;
  min-height:clamp(54px,8.6vh,88px);border-radius:12px;border:1.5px solid #39433C;background:#171D19;color:#fff;
  font-family:var(--disp);font-size:clamp(20px,3.4vh,32px);letter-spacing:.03em;text-transform:uppercase}
.pfnav button svg{width:clamp(26px,4vh,38px);height:clamp(26px,4vh,38px)}
.pfnav button.go{background:#FFC800;border-color:#FFC800;color:#111}
.pfnav button[disabled]{opacity:.35}
.pfnav button:active{transform:translateY(2px)}
"""
sub("/* ---------------------------------------------------------------- intro", CSS +
    "/* ---------------------------------------------------------------- intro")

# the rail gets a Coach button, bound with the others
sub("""        <div class="rail">
          <button class="rbtn" id="stage"></button>""",
    """        <div class="rail">
          <button class="rbtn coachbtn" id="coach">Coach<small>step by step</small></button>
          <button class="rbtn" id="stage"></button>""")
sub("""  bindClock();
  document.getElementById("stage").onclick = toggleStage;
  document.getElementById("mirror").onclick = () => {
    mTarget = mTarget ? 0 : 1; if (REDUCED) mAnim = mTarget; syncRail();
    logEvent("mirror", {play: S.play.id, on: mTarget === 1}); };
  document.getElementById("replay").onclick = () => { S.stage = "run"; S.t0 = performance.now(); syncRail(); };
  buildScene();""",
    """  bindClock();
  S.coach = null;
  bindPlayRail();
  buildScene();""")
sub("""function syncRail(){
  const st = document.getElementById("stage");""",
    """function syncRail(){
  if (inCoach()){ coachRail(); return; }
  normalRail();
  const st = document.getElementById("stage");""")
sub("""  const el = document.getElementById("calls");
  const p = S.play;
  if (S.solo){""",
    """  if (inCoach()){ renderCoach(); return; }
  const el = document.getElementById("calls");
  el.classList.remove("cdark", "coachon", "pfpanel");
  const p = S.play;
  if (S.solo){""")
# in Coach Mode a tap on a player picks him
sub("""      if (S.screen === "designer") return;   // the designer reads its own taps
      S.hintSeen = true;""",
    """      if (S.screen === "designer") return;   // the designer reads its own taps
      if (inCoach()){ coachPick(key); return; }
      S.hintSeen = true;""")
# the coach owns the clock while it is on
sub("""  const t = !running ? 0 : S.screen === "play" ? Math.min(now - S.t0, tl.tEnd)""",
    """  const t = inCoach() ? coachT(now) : !running ? 0 : S.screen === "play" ? Math.min(now - S.t0, tl.tEnd)""")
sub("""  } else {
    SC.ball.setAttribute("opacity", 0);
  }
}""",
    """  } else {
    SC.ball.setAttribute("opacity", 0);
  }
  if (inCoach()) coachFrame(t);
  fieldFrame();
  frame3d(t);
}""")
sub('<div class="hint" id="hint">Tap a player to show him his job</div>',
    '<div class="hint" id="hint">Tap a player to show their job</div>')
# a 3D button in the top bar, next to the formation chip
sub("""        <div class="barright">${clockHTML()}<span class="chip">""",
    """        <div class="barright"><span class="rfspot hidden" id="rfspot"><button id="rfminus" aria-label="Ball back 5 yards">&minus;</button><span id="rfval"></span><button id="rfplus" aria-label="Ball up 5 yards">+</button></span><button class="v3d" id="rfield">Field<small>play</small></button><button class="v3d" id="v3d">3D<small>off</small></button>${clockHTML()}<span class="chip">""")
# no service worker for a local mockup
sub('if ("serviceWorker" in navigator && location.protocol.startsWith("http")){',
    'if (false){')
sub("\nrequestAnimationFrame(frame);", "\n" + coach + "\nrequestAnimationFrame(frame);")
# end zone colour: a row on Settings > Game, wired after the page draws
sub("""    ["game", "Play names",""", """    ezSettingRow(),
    ["game", "Play names",""")
sub("""  document.getElementById("sdone").onclick = renderDeck;""",
    """  document.getElementById("sdone").onclick = renderDeck;
  bindEzSetting();""")
sub("<title>Huddle Board</title>", "<title>Huddle Board: Coach Mode mockup</title>")

(here / "index.html").write_text(s)
print("ok", len(s))
