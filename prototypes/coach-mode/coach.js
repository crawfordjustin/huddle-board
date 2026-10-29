/* ------------------------------------------------------------ coach mode
   MOCKUP. Coach Mode breaks a play into steps a 6-year-old can follow. The
   coach picks WHO (one kid, or the whole team), then walks through the play
   one step at a time with NEXT: line up, snap, then every cut, handoff,
   throw and catch. The markers actually run their routes, the ball starts
   with the snapper and goes where the possession chain says, and the words
   are generated from the play's own geometry, so every play gets steps
   without anybody writing them. Mirror still works: the words swap BLUE and
   ORANGE with the picture. */
const inCoach = () => !!S.coach && S.screen === "play";
const COACH_SLOW = 0.4, SNAP_MS = 520, MERGE_MS = 220;

// the kid is called "the BACK", but "WIDE BLUE" needs no article
const who_ = k => { const n = S.play.spots[k].name;
  return /BLUE|ORANGE/.test(n) ? n : "the " + n; };
const Who_ = k => { const s = who_(k); return s[0].toUpperCase() + s.slice(1); };

function coachEvents(){
  const p = S.play, tl = S.tl, b = tl.ball;
  const ev = {handoffs:[], pass:null, end:0};
  ev.end = Math.max(...tl.paths.map(s => s.start + s.dur), b.start + b.dur);
  if (b.legs){
    b.legs.forEach((l, k) => {
      if (l.handoff == null) return;
      const h = p.paths[l.handoff], to = h.pts[h.pts.length - 1];
      let t = l.start;
      if (k === 0){
        const seg = tl.paths[l.pathIdx];
        t = seg.start + seg.dur * fracNear(p.paths[l.pathIdx].pts, to);
        t = Math.max(t, tl.tSnap + 120);
      }
      ev.handoffs.push({t, giver:h.who, taker:l.who, leg:k});
    });
  }
  if (b.mode === "pass")
    ev.pass = {thrower:b.thrower, rec:p.paths[b.pathIdx].who, t:b.start, catchT:b.start + b.dur};
  return ev;
}

// where a kid is at time t: on the path he is running, or on his spot
function kidPos(key, t){
  const p = S.play, tl = S.tl, sp = p.spots[key];
  let pos = [sp.x, sp.y], handing = null;
  p.paths.forEach((path, i) => {
    if (path.who !== key) return;
    const seg = tl.paths[i];
    if (t < seg.start) return;
    const w = walk(path.pts, (t - seg.start) / seg.dur).end;
    if (path.type === "handoff" && t < seg.start + seg.dur) handing = w;
    pos = w;
  });
  return handing || pos;
}

// who has the ball at time t, and where it is (pass flight has no holder)
function ballAt(t){
  const p = S.play, tl = S.tl, b = tl.ball, c = S.coach, ev = c.ev;
  const C = p.spots.C, QB = p.spots.QB;
  if (t < tl.tSnap || (c.snapF != null && c.snapF < 1)){
    const f = c.snapF == null || t < tl.tSnap ? 0 : c.snapF;
    return {holder: f < 1 ? "C" : "QB",
            at: [C.x + (QB.x - C.x) * f, C.y + 0.9 + (QB.y - C.y - 0.9) * f]};
  }
  if (ev.pass && t >= ev.pass.t){
    const f = Math.max(0, Math.min(1, (t - b.start) / b.dur));
    return {holder: f >= 1 ? ev.pass.rec : null, at: qbez(b.src, b.ctrl, b.dst, f), flying: f < 1};
  }
  if (b.legs){
    let leg = null;
    for (const l of b.legs) if (t >= l.start) leg = l;
    const first = ev.handoffs.find(h => h.leg === 0);
    if (!leg || (leg === b.legs[0] && first && t < first.t)){
      const g = first ? first.giver : "QB";
      return {holder:g, at:kidPos(g, t)};
    }
    const seg = tl.paths[leg.pathIdx];
    return {holder:leg.who, at:walk(p.paths[leg.pathIdx].pts, (t - seg.start) / seg.dur).end};
  }
  const th = ev.pass ? ev.pass.thrower : "QB";
  return {holder:th, at:kidPos(th, t)};
}

/* -- words -------------------------------------------------------------- */
function whereWords(key){
  const sp = S.play.spots[key], side = sp.x < 0 ? "BLUE" : "ORANGE";
  if (sp.name === "SNAPPER") return "Stand on the ball. You snap it.";
  if (sp.name === "THROWER") return "Stand right behind the snapper.";
  const ax = Math.abs(sp.x), back = sp.y < -1.2;
  const where = ax >= 8 ? `out wide, by the ${side} sideline`
              : ax >= 4 ? `halfway out, on the ${side} side`
              : `close to the snapper, on the ${side} side`;
  return `Stand ${where}, ${back ? "a few steps behind the line" : "on the line"}.`;
}
function moveWords(a, b, carrying, turned){
  // No yards: a 6-year-old does not know how far a yard is, and the route on
  // the field already shows how far to go. The sentence says which way.
  const dir = dirWords(a, b);
  if (!dir) return "Stay right here.";
  const verb = carrying ? "run with the ball" : "run";
  return `${turned ? "Turn and " + verb : verb[0].toUpperCase() + verb.slice(1)} ${dir}.`;
}
function dirWords(a, b){
  const dx = b[0] - a[0], dy = b[1] - a[1];
  if (Math.hypot(dx, dy) < 0.6) return null;
  const side = dx < 0 ? "BLUE" : "ORANGE", ax = Math.abs(dx), ay = Math.abs(dy);
  return ay >= 2 * ax ? (dy > 0 ? "straight up the field" : "straight back")
       : ax >= 2 * ay ? `across, toward the ${side} side`
       : dy > 0 ? `up the field, toward the ${side} side` : `back, toward the ${side} side`;
}
// the short version of a kid's spot, for the whole-job line
function whereShort(key){
  const sp = S.play.spots[key], side = sp.x < 0 ? "BLUE" : "ORANGE", ax = Math.abs(sp.x);
  if (sp.name === "SNAPPER") return "line up on the ball";
  if (sp.name === "THROWER") return "line up behind the snapper";
  return ax >= 8 ? `line up wide on the ${side} side`
       : ax >= 4 ? `line up halfway out on the ${side} side`
       : sp.y < -1.2 ? `line up behind the line on the ${side} side`
       : `line up next to the snapper on the ${side} side`;
}
// "a, b, c and d."
function joinJob(parts){
  const p = parts.filter(Boolean);
  if (!p.length) return null;
  const s = p.length === 1 ? p[0] : p.slice(0, -1).join(", ") + " and " + p[p.length - 1];
  return s[0].toUpperCase() + s.slice(1) + ".";
}
const heading = (a, b) => Math.atan2(b[1] - a[1], b[0] - a[0]);

/* -- steps -------------------------------------------------------------- */
function coachSteps(who){
  const p = S.play, tl = S.tl, ev = S.coach.ev;
  const steps = [];
  const motion = p.paths.find(x => x.type === "motion");
  const team = who === "team";

  // every step carries `short`, its clause in the whole-job line
  steps.push({kind:"lineup", head:"Line up",
    text: team ? "Everybody find your spot. BLUE kids on the BLUE side, ORANGE kids on the ORANGE side."
               : whereWords(who),
    short: team ? null : whereShort(who)});
  if (motion){
    const mover = motion.who, mine = !team && who === mover;
    const d = dirWords(motion.pts[0], motion.pts[motion.pts.length - 1]);
    steps.push({kind:"motion", head:"Motion", ta:HOLD, tb:tl.tSnap,
      text: mine ? `Before the snap, jog ${d}.`
        : `Before the snap, ${who_(mover)} moves. ${team ? "Everybody else stays still." : "You stay still."}`,
      short: mine ? `jog ${d} before the snap` : null});
  }
  const role = team ? "team" : p.spots[who].name;
  steps.push({kind:"snap", head:"Snap",
    text: role === "team" ? "The snapper snaps the ball back to the thrower."
        : role === "SNAPPER" ? "Snap the ball back to the thrower."
        : role === "THROWER" ? "Catch the snap with both hands."
        : "Get ready. When the snapper snaps the ball, GO!",
    short: role === "SNAPPER" ? "snap it to the thrower" : role === "THROWER" ? "catch the snap" : null});

  // breaks after the snap: every handoff, the throw and the catch (team);
  // plus every corner of this kid's own route (one kid)
  const breaks = [];
  ev.handoffs.forEach(h => {
    if (team || h.giver === who || h.taker === who)
      breaks.push({t:h.t, tag: team ? "handoff" : h.giver === who ? "give" : "take", h});
  });
  if (ev.pass){
    if (team || ev.pass.thrower === who) breaks.push({t:ev.pass.t, tag:"throw"});
    if (team || ev.pass.thrower === who || ev.pass.rec === who) breaks.push({t:ev.pass.catchT, tag:"catch"});
  }
  let kEnd = 0;
  if (!team){
    p.paths.forEach((path, i) => {
      if (path.who !== who || path.type === "motion") return;
      const seg = tl.paths[i], L = lens(path.pts), tot = L[L.length - 1] || 1;
      for (let j = 1; j < path.pts.length; j++)
        breaks.push({t: seg.start + seg.dur * L[j] / tot, tag:"corner"});
      kEnd = Math.max(kEnd, seg.start + seg.dur);
    });
  }
  const end = team ? ev.end : Math.max(kEnd, ...breaks.map(x => x.t), tl.tSnap + 400);
  breaks.push({t:end, tag:"end"});
  breaks.sort((a, b) => a.t - b.t);
  // two breaks close together are one step; the ball event wins the label
  const merged = [];
  for (const br of breaks){
    if (br.t <= tl.tSnap + 60) continue;
    const last = merged[merged.length - 1];
    const plain = x => x.tag === "corner" || x.tag === "end";
    if (last && br.t - last.t < MERGE_MS && (plain(last) || plain(br))){
      if (last.tag === "corner" || last.tag === "end") { last.tag = br.tag === "end" ? last.tag : br.tag; last.h = br.h || last.h; }
      last.t = Math.max(last.t, br.t);
    } else merged.push({...br});
  }

  // one kid: a corner where he keeps going the same way, or a stretch where
  // he stands still, is not a step of its own
  const kept = [];
  if (team) kept.push(...merged);
  else {
    const plain = x => x.tag === "corner" || x.tag === "end";
    let from = tl.tSnap;
    merged.forEach((br, i) => {
      if (plain(br)){
        const a = kidPos(who, from), b = kidPos(who, br.t);
        const still = Math.hypot(b[0]-a[0], b[1]-a[1]) < 0.6;
        const next = merged[i + 1];
        if (next){
          const c = kidPos(who, next.t);
          const goOn = !still && Math.hypot(c[0]-b[0], c[1]-b[1]) > 0.6 &&
            Math.abs(((heading(b, c) - heading(a, b) + 3 * Math.PI) % (2 * Math.PI)) - Math.PI) < 0.35;
          if (still || goOn) return;
        } else if (still && kept.length) return;
      }
      kept.push(br); from = br.t;
    });
  }

  let ta = tl.tSnap, prevHead = null, moved = false;
  kept.forEach((br, n) => {
    const tb = br.t, mid = (ta + tb) / 2;
    const holder = ballAt(mid).holder;
    let head, text, short = null;
    if (team){
      const firstMove = n === 0 ? " Everybody else: run your route!" : "";
      if (br.tag === "handoff"){
        head = "Handoff"; text = `${Who_(br.h.giver)} hands it to ${who_(br.h.taker)}.${firstMove}`;
      } else if (br.tag === "throw"){
        head = "Routes"; text = `Everybody runs their route. The thrower looks at ${who_(ev.pass.rec)}.`;
      } else if (br.tag === "catch"){
        head = "Throw"; text = `The thrower throws it to ${who_(ev.pass.rec)}. Catch it!`;
      } else {
        const h = ballAt(tb - 1).holder;
        head = ev.pass ? "Finish" : "Run";
        text = ev.pass ? "Everybody finishes the route." :
               `${Who_(h || "QB")} runs with the ball. Go score!${firstMove}`;
      }
    } else {
      const a = kidPos(who, ta), b = kidPos(who, tb);
      const carrying = holder === who;
      const turned = moved && prevHead != null && Math.hypot(b[0]-a[0], b[1]-a[1]) > 0.6 &&
        Math.abs(((heading(a, b) - prevHead + 3 * Math.PI) % (2 * Math.PI)) - Math.PI) > 0.6;
      const mv = moveWords(a, b, carrying, turned), dir = dirWords(a, b);
      // the whole-job clause is shorter than the step: just which way
      const sd = !dir ? null : /^straight up/.test(dir) || /^up the field/.test(dir) && !/toward/.test(dir) ? "up the field"
               : /^straight back/.test(dir) ? "back" : `toward the ${dir.includes("BLUE") ? "BLUE" : "ORANGE"} side`;
      const runShort = !sd ? null : turned ? `turn ${sd}` : `run ${sd}`;
      if (br.tag === "take"){
        head = "Take it"; text = `Go to ${who_(br.h.giver)} and take the ball with both hands!`;
        short = `take the ball from ${who_(br.h.giver)}`;
      } else if (br.tag === "give"){
        head = "Hand off"; text = `Turn and hand the ball to ${who_(br.h.taker)}.`;
        short = `hand it to ${who_(br.h.taker)}`;
      } else if (br.tag === "throw"){
        head = "Look"; text = `Look at ${who_(ev.pass.rec)}. Get ready to throw.`;
        short = runShort;
      } else if (br.tag === "catch"){
        const thr = ev.pass.thrower === who;
        head = thr ? "Throw" : "Catch";
        text = thr ? `Throw the ball to ${who_(ev.pass.rec)}!`
             : `${dir ? mv + " " : ""}Then catch the ball!`;
        short = thr ? `throw it to ${who_(ev.pass.rec)}` : [runShort, "catch the ball"].filter(Boolean).join(" and ");
      } else {
        head = turned ? "Turn" : carrying ? "Run" : !moved ? "Go" : "Keep going";
        text = mv;
        if (carrying && b[1] > 4 && br.tag === "end") text += " Go score!";
        short = runShort;
      }
      if (Math.hypot(b[0]-a[0], b[1]-a[1]) > 0.6){ prevHead = heading(a, b); moved = true; }
    }
    // the same words twice in a row is one step, not two
    const prev = steps[steps.length - 1];
    if (prev.kind === "go" && prev.text === text) prev.tb = tb;
    else steps.push({kind:"go", head, text, short, ta, tb});
    ta = tb;
  });
  // the whole job is this kid's steps in one line, so it can never say
  // something the steps do not
  steps.whole = team ? null : joinJob(steps.map(s => s.short));
  steps.jobs = team ? null : steps.map(s => s.short).filter(Boolean)
    .map(s => s[0].toUpperCase() + s.slice(1) + ".");
  return steps;
}

/* -- control ------------------------------------------------------------ */
function enterCoach(){
  S.coach = {who:null, steps:[], i:0, anim:null, slow:true, auto:false, snapF:null,
             ev:coachEvents()};
  S.solo = null; S.stage = "lineup"; S.hintSeen = true;
  ensureCoachLayer();
  logEvent("coach_mode", {play:S.play.id});
  renderCalls(); syncRail();
}
function exitCoach(){
  setFocus(false);
  S.coach = null; S.solo = null;
  if (SC && SC.coach) SC.coach.g.setAttribute("opacity", 0);
  S.stage = "run"; S.t0 = performance.now() - S.tl.tEnd;
  renderCalls(); syncRail();
}
function coachPick(who){
  const c = S.coach;
  c.who = who; c.auto = false;
  S.solo = who === "team" ? null : who;
  c.steps = coachSteps(who);
  setFocus(who !== "team");
  goStep(0);
}
function goStep(i){
  const c = S.coach, tl = S.tl, st = c.steps[i];
  c.i = i; c.snapF = null;
  const speed = c.slow ? COACH_SLOW : 1, now = performance.now();
  if (st.kind === "lineup"){ S.stage = "lineup"; c.anim = {from:0, to:0, start:now, dur:1}; }
  else if (st.kind === "snap"){ S.stage = "run"; c.anim = {snap:true, from:tl.tSnap, to:tl.tSnap, start:now, dur:SNAP_MS / speed}; }
  else { S.stage = "run"; c.anim = {from:st.ta, to:st.tb, start:now, dur:Math.max(250, (st.tb - st.ta) / speed)}; }
  c.anim.done = false;
  c.finished = false;
  clearTimeout(c.doneTimer);
  if (i === c.steps.length - 1)
    c.doneTimer = setTimeout(() => { if (S.coach === c && c.i === i){ c.finished = true; renderCalls(); } }, c.anim.dur + 150);
  // Replay whole play walks through every step on its own, with a beat
  // between them so the kids can see where each one ends
  clearTimeout(c.timer);
  if (c.auto){
    const wait = Math.max(c.anim.dur, st.kind === "lineup" ? 1100 : 0) + 650;
    c.timer = setTimeout(() => {
      if (S.coach !== c || !c.auto) return;
      if (c.i < c.steps.length - 1) goStep(c.i + 1);
      else { c.auto = false; renderCalls(); }
    }, wait);
  }
  renderCalls(); syncRail();
}
function coachT(now){
  const c = S.coach, a = c.anim;
  if (!c.who || !a) return 0;
  const f = Math.max(0, Math.min(1, (now - a.start) / a.dur));
  // steps after the snap keep the ball in the thrower's hands
  c.snapF = a.snap ? f : (c.steps[c.i].kind === "go" ? 1 : null);
  return a.from + (a.to - a.from) * f;
}

/* -- drawing ------------------------------------------------------------ */
function ensureCoachLayer(){
  if (SC.coach){ SC.coach.g.setAttribute("opacity", 1); return; }
  const svg = document.getElementById("field");
  const g = mk("g", {"pointer-events":"none"});
  const first = Object.values(SC.players)[0].g;
  svg.insertBefore(g, first);
  const guides = S.play.paths.map(path => {
    const l = mk("polyline", {fill:"none", stroke:"#232B26", "stroke-width":5,
      "stroke-dasharray":"3 10", "stroke-linecap":"round", opacity:0});
    g.appendChild(l); return l;
  });
  const ghosts = {};
  for (const key in S.play.spots){
    const c = mk("circle", {r:15, fill:"none", stroke:"#8A948C", "stroke-width":3,
      "stroke-dasharray":"5 5", opacity:0});
    g.appendChild(c); ghosts[key] = c;
  }
  SC.coach = {g, guides, ghosts};
}
function coachFrame(t){
  const p = S.play, c = S.coach, tl = S.tl, on = !!c.who;
  const moving = on && c.steps[c.i] && c.steps[c.i].kind !== "lineup";
  // the markers run their routes; a dashed ring stays where each kid lined up
  for (const key in SC.players){
    const P = SC.players[key], sp = p.spots[key];
    const at = moving ? kidPos(key, t) : [sp.x, sp.y];
    const s = W2S(at[0], at[1]);
    P.g.setAttribute("transform", `translate(${s[0].toFixed(1)} ${s[1].toFixed(1)})`);
    const gs = W2S(sp.x, sp.y), gh = SC.coach.ghosts[key];
    gh.setAttribute("cx", gs[0]); gh.setAttribute("cy", gs[1]);
    const away = Math.hypot(at[0] - sp.x, at[1] - sp.y) > 0.4;
    gh.setAttribute("opacity", !away ? 0 : S.solo ? (S.solo === key ? 0.9 : 0) : 0.45);
    if (!on) P.g.setAttribute("opacity", 1);
  }
  // one kid: his whole route waits for him as a faint dotted guide
  p.paths.forEach((path, i) => {
    const l = SC.coach.guides[i];
    const show = on && S.solo === path.who && path.type !== "handoff";
    l.setAttribute("opacity", show ? 0.35 : 0);
    if (show) l.setAttribute("points", path.pts.map(pt).join(" "));
  });
  if (c.focus) focusFrame();
  // the ball: snapper first, then whoever the chain says, never ahead of it
  if (!on){ SC.ball.setAttribute("opacity", 0); return; }
  const now = ballAt(t), ahead = ballAt(t + 40);
  const s = W2S(now.at[0], now.at[1]), s2 = W2S(ahead.at[0], ahead.at[1]);
  if (Math.hypot(s2[0] - s[0], s2[1] - s[1]) > 0.5)
    SC.ballAng = Math.atan2(s2[1] - s[1], s2[0] - s[0]) * 180 / Math.PI;
  else if (now.holder === "C" && !moving) SC.ballAng = 0;
  // held, it sits a touch off the kid's marker so both stay visible
  // in Player Focus the marker is big, so the ball rides at its shoulder
  const k3 = c.focus ? V3.scaleAt(s[1]) : 1;
  const off = now.flying ? [0, 0] : c.focus ? [44 * k3, -78 * k3] : [0, -26];
  SC.ball.setAttribute("transform",
    `translate(${(s[0] + off[0]).toFixed(1)} ${(s[1] + off[1]).toFixed(1)}) rotate(${SC.ballAng.toFixed(1)}) scale(.9)`);
  SC.ball.setAttribute("opacity", 1);
}

/* -- panel -------------------------------------------------------------- */
// stroke icons in the button's own colour, so they follow the .on/.go states
const svgI = d => `<svg viewBox="0 0 24 24" width="30" height="30" fill="none" stroke="currentColor"
  stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${d}</svg>`;
const ICON = {
  start:  svgI('<path d="M6 5v14"/><path d="M18 5l-9 7 9 7z" fill="currentColor"/>'),
  back:   svgI('<path d="M15 5l-7 7 7 7"/>'),
  replay: svgI('<path d="M4 12a8 8 0 1 0 2.4-5.7"/><path d="M4 4v5h5"/>'),
  whole:  svgI('<path d="M4 12a8 8 0 1 0 2.4-5.7"/><path d="M4 4v5h5"/><path d="M10.5 9l4.5 3-4.5 3z" fill="currentColor"/>'),
  next:   svgI('<path d="M9 5l7 7-7 7"/>'),
};
// light or dark instruction panel, remembered on this tablet
let coachTheme = "dark";
try { coachTheme = localStorage.getItem("hb.coachtheme") || "dark"; } catch(e){}
function setCoachTheme(v){ coachTheme = v; try { localStorage.setItem("hb.coachtheme", v); } catch(e){} }

function renderCoach(){
  if (S.coach.focus){ renderFocus(); return; }
  const el = document.getElementById("calls"), p = S.play, c = S.coach;
  const W = s => mirrored() ? swapWords(s) : s;
  el.classList.remove("pfpanel");
  el.classList.toggle("cdark", coachTheme === "dark");
  // one fixed height for every player and step, so the field never jumps
  el.classList.add("coachon");
  const chips = [`<button data-who="team" class="${c.who === "team" ? "on" : ""}">Whole team</button>`]
    .concat(Object.keys(p.spots).map(k => {
      const sd = sideOf(k), col = sd === "blue" ? "cb" : sd === "orange" ? "co" : "";
      return `<button data-who="${k}" class="${c.who === k ? "on" : ""} ${col}">${esc(sideName(k))}</button>`;
    })).join("");
  let body;
  if (!c.who){
    body = `<div class="cstep"><div class="ctext"><b>Who are we showing?</b>
      <span>Tap a player on the field, or pick one above. Whole team shows everybody.</span></div></div>`;
  } else {
    const st = c.steps[c.i], n = c.steps.length;
    // BLUE and ORANGE in the sentence wear their own colour, like the field
    const paint = s => esc(W(s)).replace(/\b(BLUE|ORANGE)\b/g,
      (m) => `<span style="color:${m === "BLUE" ? SIDE.blue : SIDE.orange}">${m}</span>`);
    // every step is a numbered pill: done, this one, still to come
    // a step is done once the coach has moved past it, or once the last one
    // has finished playing; going back un-does the ones after it
    const isDone = k => k < c.i || (c.finished && k === c.i);
    const track = c.steps.map((s, k) => `<button class="cpill ${isDone(k) ? "done" : k === c.i ? "now" : ""}"
        data-step="${k}"><i>${isDone(k) ? "&#10003;" : k + 1}</i>${esc(W(s.head))}</button>`).join("");
    body = `<div class="cstep">
      <div class="ctrack">${track}</div>
      <div class="crow">
        <div class="cart" aria-label="Picture for this step (placeholder)">
          <svg viewBox="0 0 24 24" width="34" height="34" fill="none" stroke="currentColor" stroke-width="1.8"
            stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="3" y="4" width="18" height="16" rx="2"/>
            <circle cx="9" cy="10" r="2"/><path d="M21 17l-5-5-9 8"/></svg>
          <b>Picture</b><small>${esc(W(st.head))}</small>
        </div>
        <div class="ctext"><b>${esc(W(st.head))}</b><span>${paint(st.text)}</span></div>
        ${c.steps.jobs ? `<div class="cjob"><h4>Your whole job</h4><ol>${
          c.steps.jobs.map((j, k) => `<li><i>${k + 1}</i><div>${paint(j)}</div></li>`).join("")}</ol></div>` : ""}
      </div>
      <div class="cnav">
        <button class="rbtn ico" id="cstart" aria-label="Start from the beginning" ${c.i === 0 ? "disabled" : ""}>${ICON.start}<small>Start</small></button>
        <button class="rbtn ico" id="cagain" aria-label="Replay this step">${ICON.replay}<small>Replay</small></button>
        <button class="rbtn ico ${c.auto ? "on" : ""}" id="cplay" aria-label="Replay whole play">${ICON.whole}<small>Whole play</small></button>
        <button class="rbtn ico go" id="cnext" aria-label="Next" ${c.i === n - 1 ? "disabled" : ""}>${ICON.next}<small>Next</small></button>
      </div></div>`;
  }
  el.innerHTML = `<div class="coachp"><div class="cwho"><span class="lab">Who?</span>${chips}</div>${body}</div>`;
  el.querySelectorAll("[data-who]").forEach(b => b.onclick = () => coachPick(b.dataset.who));
  const on = (id, f) => { const b = document.getElementById(id); if (b) b.onclick = f; };
  on("cback", () => { c.auto = false; goStep(Math.max(0, c.i - 1)); });
  on("cagain", () => { c.auto = false; goStep(c.i); });
  el.querySelectorAll("[data-step]").forEach(b => b.onclick = () => { c.auto = false; goStep(+b.dataset.step); });
  on("cplay", () => { c.auto = true; goStep(0); });
  on("cstart", () => { c.auto = false; goStep(0); });
  on("cnext", () => { c.auto = false; goStep(Math.min(c.steps.length - 1, c.i + 1)); });
}
function coachRail(){
  const c = S.coach, rail = document.querySelector(".play .rail");
  rail.innerHTML = `
    <button class="rbtn on" id="coach">Coach<small>tap to leave</small></button>
    <button class="rbtn ${mTarget === 1 ? "on" : ""}" id="mirror">Mirror<small>swap sides</small></button>
    <button class="rbtn" id="cspeed">${c.slow ? "Slow" : "Full"}<small>speed</small></button>
    <button class="rbtn ${coachTheme === "dark" ? "on" : ""}" id="ctheme">${coachTheme === "dark" ? "Dark" : "Light"}<small>tap to switch</small></button>`;
  document.getElementById("ctheme").onclick = () => {
    setCoachTheme(coachTheme === "dark" ? "light" : "dark"); coachRail(); renderCoach(); };
  document.getElementById("coach").onclick = exitCoach;
  document.getElementById("mirror").onclick = () => {
    mTarget = mTarget ? 0 : 1; if (REDUCED) mAnim = mTarget; coachRail(); };
  document.getElementById("cspeed").onclick = () => { c.slow = !c.slow; coachRail(); };
  const h = document.getElementById("hint");
  if (h){ h.textContent = "Tap a player to show their steps"; h.style.opacity = c.who ? 0 : 1; }
}
function normalRail(){
  const rail = document.querySelector(".play .rail");
  if (rail.querySelector("#stage")) return;
  rail.innerHTML = `
    <button class="rbtn coachbtn" id="coach">Coach<small>step by step</small></button>
    <button class="rbtn" id="stage"></button>
    <button class="rbtn" id="mirror">Mirror<small>swap sides</small></button>
    <button class="rbtn" id="replay">Replay</button>`;
  bindPlayRail();
}
function bindPlayRail(){
  const v = document.getElementById("v3d");
  if (v && !v.onclick){ v.onclick = () => setView3d(!V3.on); setView3d(V3.on); }
  bindRealField();
  document.getElementById("coach").onclick = enterCoach;
  document.getElementById("stage").onclick = toggleStage;
  document.getElementById("mirror").onclick = () => {
    mTarget = mTarget ? 0 : 1; if (REDUCED) mAnim = mTarget; syncRail();
    logEvent("mirror", {play: S.play.id, on: mTarget === 1}); };
  document.getElementById("replay").onclick = () => { S.stage = "run"; S.t0 = performance.now(); syncRail(); };
  const h = document.getElementById("hint");
  if (h) h.textContent = "Tap a player to show their job";
}


/* -- Player Focus -------------------------------------------------------
   Tap one player in Coach Mode and the play goes full screen for just that
   kid: the same play, the same marker, the same timing, with every other
   player and route taken off the field. Nothing about the play changes;
   only who is on the screen. Exit goes back to Coach Mode. */
function setFocus(on){
  const c = S.coach, play = document.querySelector(".play");
  if (!c || !play || !!c.focus === on) return;
  c.focus = on;
  play.classList.toggle("pfocus", on);
  if (on){
    c.was3d = V3.on; setView3d(true);
    if (!document.getElementById("pfhead")){
      const h = document.createElement("div");
      h.id = "pfhead"; h.className = "pfhead";
      play.insertBefore(h, play.firstChild);
    }
  } else {
    setView3d(!!c.was3d);
    const h = document.getElementById("pfhead"); if (h) h.remove();
    SC.paths.forEach(g => { g.line.removeAttribute("opacity"); if (g.bar) g.bar.removeAttribute("opacity"); });
  }
}
function exitFocus(){
  const c = S.coach;
  clearTimeout(c.timer); clearTimeout(c.doneTimer);
  setFocus(false);
  c.who = null; c.auto = false; c.anim = null; S.solo = null; S.stage = "lineup";
  renderCalls(); syncRail();
}
// runs after the app's own drawing: the app fades the other kids, focus hides them
function focusFrame(){
  const p = S.play, who = S.coach.who;
  for (const key in SC.players) if (key !== who) SC.players[key].g.setAttribute("opacity", 0);
  p.paths.forEach((path, i) => {
    const g = SC.paths[i], op = path.who === who ? 1 : 0;
    g.line.setAttribute("opacity", op); if (g.bar) g.bar.setAttribute("opacity", op);
  });
  if (SC.throw && SC.throw.getAttribute("opacity") !== "1") SC.throw.setAttribute("opacity", 0);
}

// Craig's step icons, white art used as a mask so each one takes the colour
// of where it sits: white on a dark tab, black on the yellow one
const sIcon = n => `<span class="sicon" style="--i:url(art/step-${n}.png)" aria-hidden="true"></span>`;
// Craig's route-tree arrows, each described by the way it starts, bends and
// ends (degrees: 0 right, 90 up the field). A running step is matched to the
// arrow whose shape is closest to the kid's own path in that step.
const ROUTE_ICONS = (() => {
  const N = 90, S_ = 270, E = 0, W_ = 180, NE = 45, NW = 135, SE = 315, SW = 225;
  return [
  ["1-1", N, N, N], ["1-2", S_, S_, S_], ["1-3", E, E, E], ["1-4", W_, W_, W_],
  ["1-5", NE, NE, NE], ["1-6", NW, NW, NW], ["1-7", SE, SE, SE], ["1-8", SW, SW, SW],
  ["3-1", E, NE, N], ["3-2", S_, SE, E], ["3-3", W_, NW, N], ["3-4", S_, SW, W_],
  ["3-6", E, N, N], ["3-5", W_, N, N],
  ["4-2", NW, NW, N], ["6-3", SE, SE, S_], ["6-4", SW, SW, S_],
  ["6-1", N, NE, NE], ["6-2", N, NW, NW],
  ["7-1", N, E, N], ["7-2", N, W_, N], ["7-3", E, E, S_], ["7-4", W_, W_, S_],
  ["7-7", N, NE, E], ["7-8", N, NW, W_],
  ];
})();
const angDiff = (a, b) => { const d = Math.abs(a - b) % 360; return d > 180 ? 360 - d : d; };
function routeIcon(st){
  if (st.routeIcon !== undefined) return st.routeIcon;
  const who = S.coach.who, flip = mTarget === 1 ? -1 : 1;
  const pts = [];
  for (let k = 0; k <= 24; k++) pts.push(kidPos(who, st.ta + (st.tb - st.ta) * k / 24));
  const dir = (a, b) => {
    const dx = (b[0] - a[0]) * flip, dy = b[1] - a[1];
    return Math.hypot(dx, dy) < .05 ? null : (Math.atan2(dy, dx) * 180 / Math.PI + 360) % 360;
  };
  const s = dir(pts[0], pts[6]), m = dir(pts[6], pts[18]), e = dir(pts[18], pts[24]);
  const all = dir(pts[0], pts[24]);
  if (all == null){ st.routeIcon = null; return null; }
  const ss = s ?? all, mm = m ?? all, ee = e ?? all;
  let best = null, score = 1e9;
  for (const [id, a, b, c] of ROUTE_ICONS){
    const sc = angDiff(ss, a) + angDiff(mm, b) * 0.8 + angDiff(ee, c);
    if (sc < score){ score = sc; best = id; }
  }
  st.routeIcon = best;
  return best;
}

function stepIcon(st){
  const h = st.head;
  // a running step shows the shape of the kid's own route
  if (st.kind === "go" && /^(Run|Go|Keep going|Turn)$/.test(h) && S.coach && S.coach.who && S.coach.who !== "team"){
    const r = routeIcon(st);
    if (r) return `<span class="sicon" style="--i:url(art/route-${r}.png)" aria-hidden="true"></span>`;
  }
  // motion runs the way the kid jogs: BLUE is the left side unless mirrored
  const toBlue = /BLUE/.test(st.text) && !/ORANGE/.test(st.text);
  return sIcon(st.kind === "lineup" ? "feet" : st.kind === "snap" ? "ball"
    : st.kind === "motion" ? (toBlue !== (mTarget === 1) ? "motion-left" : "motion-right")
    : h === "Look" ? "look" : h === "Throw" ? "throw" : h === "Catch" ? "catch" : h === "Hand off" ? "handoff"
    : h === "Take it" ? "hands" : h === "Turn" ? "turn" : "run");
}

function renderFocus(){
  const el = document.getElementById("calls"), p = S.play, c = S.coach;
  const W = s => mirrored() ? swapWords(s) : s;
  const st = c.steps[c.i], n = c.steps.length, who = c.who;
  el.classList.remove("cdark", "coachon");
  el.classList.add("pfpanel");
  // the header: this kid's own marker, his name, the play
  const h = document.getElementById("pfhead");
  if (h){
    h.innerHTML = `<div class="pfwho"><img class="pfmark" src="${markerArt(who)}" alt="">
        <b style="color:${{blue:"#4C9BFF", orange:"#FF8A3D"}[sideOf(who)] || "#fff"}">${esc(sideName(who))}</b><i></i><span>${esc(nameOf(p).big)}</span></div>
      <button class="pfexit" id="pfexit"><svg viewBox="0 0 24 24"><path d="M5 5l14 14M19 5L5 19"/></svg>Exit player view</button>`;
    document.getElementById("pfexit").onclick = exitFocus;
  }
  const paint = s => esc(W(s)).replace(/\b(BLUE|ORANGE)\b/g,
    (m) => `<span style="color:${m === "BLUE" ? "#4C9BFF" : "#FF8A3D"}">${m}</span>`);
  const isDone = k => k < c.i || (c.finished && k === c.i);
  const tabs = c.steps.map((s, k) => `<button class="pftab ${isDone(k) ? "done" : k === c.i ? "now" : ""}" data-step="${k}">
      <span class="pfti">${stepIcon(s)}</span><span class="pftl"><i>${isDone(k) ? "&#10003;" : k + 1}</i><b>${esc(W(s.head))}</b></span></button>`).join("");
  el.innerHTML = `<div class="pf">
    <div class="pftabs">${tabs}</div>
    <div class="pfrow"><div class="pfbig">${stepIcon(st)}</div>
      <div class="pftext"><b>${esc(W(st.head))}</b><span>${paint(st.text)}</span></div></div>
    <div class="pfnav">
      <button id="pfback" ${c.i === 0 ? "disabled" : ""}>${ICON.back}Back</button>
      <button id="pfagain">${ICON.replay}Replay</button>
      <button id="pfnext" class="go" ${c.i === n - 1 ? "disabled" : ""}>Next${ICON.next}</button>
    </div></div>`;
  const on = (id, f) => { const b = document.getElementById(id); if (b) b.onclick = f; };
  on("pfback", () => { c.auto = false; goStep(Math.max(0, c.i - 1)); });
  on("pfagain", () => { c.auto = false; goStep(c.i); });
  on("pfnext", () => { c.auto = false; goStep(Math.min(n - 1, c.i + 1)); });
  el.querySelectorAll("[data-step]").forEach(b => b.onclick = () => { c.auto = false; goStep(+b.dataset.step); });
}
