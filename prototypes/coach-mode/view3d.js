/* ------------------------------------------------------------ 3D view
   MOCKUP. The view from behind your own team: the camera stands about 13
   yards behind the line of scrimmage and looks down the field toward the
   goal, the way a kid sees it when he lines up. Same technology as the rest
   of the app — no library, no WebGL. The two functions everything already
   draws through, W2S() for players/routes/ball and F2S() for field paint,
   get a perspective projection instead of a flat scale; everything else
   follows. Markers and names stay upright and only shrink with distance,
   so a kid can still read his name on a far-away spot. */
const V3 = (() => {
  let TOP = 70, BOT = VB_H - 6;            // where the far edge and the near edge land
  let YC, NEAR, FAR, A, B, K, REF, MK = 1, FARY = TOPY;
  // a lens: where the camera stands (yards behind the snapper) and how wide
  // the near sideline lands, half-width in viewBox units
  const lens = (yc, nearHalf, mk, farY = TOPY, top = 70, bot = VB_H - 6) => {
    YC = yc; FARY = farY; TOP = top; BOT = bot; NEAR = FIELD_BOT - YC; FAR = FARY - YC;
    B = (BOT - TOP) / (1 / NEAR - 1 / FAR); A = TOP - B / FAR;
    K = nearHalf * NEAR / EDGE; REF = -YC; MK = mk;
  };
  lens(-20, 560, 1);
  return {
    on: false,
    // the coach's view: close behind the team, the whole field in the frame
    wide(){ lens(-20, 560, 1); },
    // Player Focus: the camera farther back on a longer lens, so the field is
    // flatter and fills the screen edge to edge, the goal line across the top
    // It reaches past the goal to the back of the end zone, so the whole end
    // zone is on screen with the goalpost, fence and trees behind it.
    close(visHalf, visTop, visH){ lens(-60, visHalf * 1.02, 1.45, TOPY + 10, visTop + visH * 0.30, visTop + visH); },
    far(){ return FARY; },
    topY(){ return TOP; },
    key(){ return YC + ":" + K.toFixed(1) + ":" + TOP.toFixed(1); },
    boost(){ return MK; },
    // how many viewBox units one yard straight up is, at this depth
    vscale(y){ return K / Math.max(2, y - YC); },                 // extra marker size in Player Focus
    proj(x, y){ const d = Math.max(2, y - YC); return [CX + x * K / d, A + B / d]; },
    // how big a marker is at this screen height, from the same projection
    scaleAt(sy){ const d = B / Math.max(1, sy - A); return MK * Math.max(0.62, Math.min(1.35, REF / d)); },
  };
})();

const W2S_flat = W2S, F2S_flat = F2S;
W2S = function(x, y){
  if (!V3.on || dzView()) return W2S_flat(x, y);
  return V3.proj(x * (1 - 2 * mAnim), y);
};
F2S = function(x, y){
  if (!V3.on || dzView()) return F2S_flat(x, y);
  return V3.proj(x, y);
};

function setView3d(on){
  V3.on = on;
  const b = document.getElementById("v3d");
  if (b){ b.classList.toggle("on", on); b.innerHTML = on ? "3D<small>on</small>" : "3D<small>off</small>"; }
  if (!on && SC && SC.f3d){ SC.f3d.setAttribute("opacity", 0); }
  if (!on && SC && SC.grass){ SC.grass.setAttribute("opacity", 0); }
}

// Routes on the grass. The app draws each route as a line in screen space, so
// in 3D it would float flat over the field. Here every route is a ribbon of
// paint lying on the ground: built in yards, then put through the same
// projection as the field, so it narrows with distance, its dashes shorten,
// and its arrowhead lies flat. Same colour, same reveal, same opacity as the
// app's own line, which is hidden while 3D is on.
const RIB = {run:.30, route:.24, motion:.22, handoff:.20, fake:.24};
const DASH = {motion:[1.1, .75], handoff:[.9, .6], fake:[1.2, .75]};
function ribbon(pts, hw){
  const L = [], R = [];
  for (let i = 0; i < pts.length; i++){
    const a = pts[Math.max(0, i - 1)], b = pts[Math.min(pts.length - 1, i + 1)];
    let dx = b[0] - a[0], dy = b[1] - a[1]; const d = Math.hypot(dx, dy) || 1;
    dx /= d; dy /= d;
    L.push([pts[i][0] - dy * hw, pts[i][1] + dx * hw]);
    R.push([pts[i][0] + dy * hw, pts[i][1] - dx * hw]);
  }
  return L.concat(R.reverse()).map(fpt).join(" ");
}
// cut a path into dashes, in yards along the ground
function dashes(pts, on, off){
  const out = []; let cur = [pts[0]], left = on, drawing = true;
  for (let i = 1; i < pts.length; i++){
    let a = pts[i - 1]; const b = pts[i];
    let seg = Math.hypot(b[0] - a[0], b[1] - a[1]);
    while (seg > 1e-6){
      const step = Math.min(seg, left), f = step / seg;
      const m = [a[0] + (b[0] - a[0]) * f, a[1] + (b[1] - a[1]) * f];
      if (drawing) cur.push(m);
      seg -= step; left -= step; a = m;
      if (left <= 1e-6){
        if (drawing && cur.length > 1) out.push(cur);
        drawing = !drawing; left = drawing ? on : off; cur = [m];
      }
    }
  }
  if (drawing && cur.length > 1) out.push(cur);
  return out;
}
function routes3d(t){
  const svg = document.getElementById("field");
  if (!SC.rib){
    SC.rib = mk("g", {"pointer-events":"none"});
    svg.insertBefore(SC.rib, SC.paths.length ? SC.paths[0].line : SC.los.nextSibling);
  }
  const g = SC.rib;
  g.innerHTML = "";
  const p = S.play, flip = 1 - 2 * mAnim;
  p.paths.forEach((path, i) => {
    const P = SC.paths[i];
    P.line.style.display = V3.on ? "none" : "";
    if (P.bar) P.bar.style.display = V3.on ? "none" : "";
    if (!V3.on) return;
    const op = +(P.line.getAttribute("opacity") || 0);
    if (!(op > 0)) return;
    const seg = S.tl.paths[i];
    const prog = Math.max(0, Math.min(1, (t - seg.start) / seg.dur));
    if (prog <= 0) return;
    const pts = walk(path.pts, prog).pts.map(q => [q[0] * flip, q[1]]);
    if (pts.length < 2) return;
    const col = P.line.getAttribute("stroke"), hw = RIB[path.type] || .24;
    const gg = mk("g", {opacity:op, fill:col});
    const pieces = DASH[path.type] ? dashes(pts, ...DASH[path.type]) : [pts];
    for (const pc of pieces) gg.appendChild(mk("polygon", {points:ribbon(pc, hw)}));
    // the end: an arrowhead on the grass, or a bar for a stop
    const n = pts.length, a = pts[n - 2], b = pts[n - 1];
    let dx = b[0] - a[0], dy = b[1] - a[1]; const d = Math.hypot(dx, dy) || 1; dx /= d; dy /= d;
    if ((path.end || "arrow") !== "bar"){
      const w = hw * 2.6, len = hw * 3.4;
      gg.appendChild(mk("polygon", {points:[[b[0] + dx * len * .55, b[1] + dy * len * .55],
        [b[0] - dx * len * .45 - dy * w, b[1] - dy * len * .45 + dx * w],
        [b[0] - dx * len * .45 + dy * w, b[1] - dy * len * .45 - dx * w]].map(fpt).join(" ")}));
    } else if (prog > .985){
      const w = .55, th = hw;
      gg.appendChild(mk("polygon", {points:[[b[0] - dy * w - dx * th, b[1] + dx * w - dy * th],
        [b[0] + dy * w - dx * th, b[1] - dx * w - dy * th], [b[0] + dy * w + dx * th, b[1] - dx * w + dy * th],
        [b[0] - dy * w + dx * th, b[1] + dx * w + dy * th]].map(fpt).join(" ")}));
    }
    g.appendChild(gg);
  });
  // Coach Mode's faint guide of a kid's whole route lies on the grass too
  if (SC.coach && SC.coach.guides) SC.coach.guides.forEach((l, i) => {
    l.style.display = V3.on ? "none" : "";
    if (!V3.on) return;
    const op = +(l.getAttribute("opacity") || 0);
    if (!(op > 0)) return;
    const pts = p.paths[i].pts.map(q => [q[0] * flip, q[1]]);
    const gg = mk("g", {opacity:op, fill:"#FFFFFF"});
    for (const pc of dashes(pts, .35, .7)) gg.appendChild(mk("polygon", {points:ribbon(pc, .14)}));
    g.appendChild(gg);
  });
}

// The grass. Mowed stripes run toward the goal, so each one is its own
// trapezoid through the projection and they close in with distance like a
// real field. A small speckle tile, drawn once on a canvas, gives the blades.
const GRASS_STRIPE = 3;                    // yards wide per mowed stripe
function grassDefs(svg){
  const defs = svg.querySelector("defs");
  if (defs.querySelector("#grain3d")) return;
  const c = document.createElement("canvas");
  c.width = c.height = 96;
  const x = c.getContext("2d");
  for (let i = 0; i < 1400; i++){
    x.fillStyle = Math.random() < .5 ? `rgba(190,240,150,${.10 + Math.random() * .14})`
                                     : `rgba(0,40,10,${.10 + Math.random() * .16})`;
    x.fillRect(Math.random() * 96, Math.random() * 96, 1 + Math.random() * 1.5, 2 + Math.random() * 3);
  }
  const pat = mk("pattern", {id:"grain3d", width:96, height:96, patternUnits:"userSpaceOnUse"});
  const img = mk("image", {width:96, height:96});
  img.setAttribute("href", c.toDataURL());
  pat.appendChild(img);
  defs.appendChild(pat);
  // a touch darker toward the goal, for depth
  const fade = mk("linearGradient", {id:"fade3d", x1:0, y1:1, x2:0, y2:0});
  fade.appendChild(mk("stop", {offset:0, "stop-color":"#FFFFFF", "stop-opacity":.05}));
  fade.appendChild(mk("stop", {offset:1, "stop-color":"#00200A", "stop-opacity":.25}));
  defs.appendChild(fade);
}
function grass3d(svg){
  if (!SC.grass){
    grassDefs(svg);
    SC.grass = mk("g", {"pointer-events":"none"});
    svg.insertBefore(SC.grass, SC.f3d.nextSibling);
  }
  const bot = fieldBot(), key = bot + ":" + SC.overX + ":" + V3.key();
  if (SC.grass.dataset.key === key) return;   // only redraw when the view moves
  SC.grass.dataset.key = key;
  SC.grass.innerHTML = "";
  const far = V3.far(), focus = !!(S.coach && S.coach.focus);
  const quad = (x1, x2, y1 = bot, y2 = far) => [[x1, y1], [x2, y1], [x2, y2], [x1, y2]].map(fpt).join(" ");
  if (focus) backdrop3d();
  // in Player Focus the grass runs on past the sidelines, like a real field
  if (S.coach && S.coach.focus) SC.grass.appendChild(mk("polygon", {points:quad(-EDGE * 3, EDGE * 3), fill:"#2A7A34"}));
  let i = 0;
  for (let xs = -EDGE; xs < EDGE; xs += GRASS_STRIPE, i++){
    SC.grass.appendChild(mk("polygon", {points:quad(xs, Math.min(EDGE, xs + GRASS_STRIPE)),
      fill: i % 2 ? "#2F8A3A" : "#3E9B45"}));
  }
  const all = S.coach && S.coach.focus ? quad(-EDGE * 3, EDGE * 3) : quad(-EDGE, EDGE);
  SC.grass.appendChild(mk("polygon", {points:all, fill:"url(#grain3d)"}));
  SC.grass.appendChild(mk("polygon", {points:all, fill:"url(#fade3d)"}));
  if (focus) field3dPaint(quad, bot, far);
}

// In 3D the paint on the field is white, the way real lines look on grass.
// Each element keeps its flat-view colour to go back to.
const PAINT3D = [
  [() => SC.yard, {"stroke-opacity":0}],     // the app's dashed 5 and 10: not on a real field
  [() => [SC.los], {stroke:"#FFFFFF", "stroke-opacity":.95}],
];
// Player Focus paints a real field: solid white sidelines with the BLUE and
// ORANGE bands outside them, a solid white goal line, and the whole end zone
// in the end zone colour out to its back line. With the real field on, the
// field's own lines do this instead.
function field3dPaint(quad, bot, far){
  const g = SC.grass, flip = mTarget === 1;
  const band = 1.4, goal = TOPY;
  const sideCol = left => (left !== flip) ? "#1D6FD0" : "#E4610F";
  g.appendChild(mk("polygon", {points:quad(-EDGE - band, -EDGE, bot, far), fill:sideCol(true)}));
  g.appendChild(mk("polygon", {points:quad(EDGE, EDGE + band, bot, far), fill:sideCol(false)}));
  if (!RF.on){
    g.appendChild(mk("polygon", {points:quad(-EDGE, EDGE, goal, far), fill:ezHex()}));
    const l = (y, w) => g.appendChild(mk("polygon", {points:quad(-EDGE, EDGE, y - w / 2, y + w / 2), fill:"#FFFFFF"}));
    l(goal, 0.35); l(far - 0.15, 0.3);
    const t = F2S(0, goal + 5);
    const tx = mk("text", {x:t[0], y:t[1], "text-anchor":"middle", "dominant-baseline":"central",
      "font-family":"Barlow Condensed, Arial Narrow, sans-serif", "font-weight":700, "letter-spacing":8,
      "font-size":34 * V3.scaleAt(t[1]) / V3.boost(), fill:"#FFFFFF", opacity:.9});
    tx.textContent = "END ZONE";
    g.appendChild(tx);
  }
  const side = x => g.appendChild(mk("polygon", {points:quad(x - .18, x + .18, bot, far), fill:"#FFFFFF"}));
  side(-EDGE); side(EDGE);
}
// the goalpost, fence and trees past the back of the end zone, under a sky
function backdrop3d(){
  const svg = document.getElementById("field");
  if (!SC.sky){
    const defs = svg.querySelector("defs");
    const sg = mk("linearGradient", {id:"sky3d", x1:0, y1:0, x2:0, y2:1});
    sg.appendChild(mk("stop", {offset:0, "stop-color":"#3F86D6"}));
    sg.appendChild(mk("stop", {offset:1, "stop-color":"#BFDDF5"}));
    defs.appendChild(sg);
    SC.sky = mk("rect", {fill:"url(#sky3d)", "pointer-events":"none"});
    SC.bd = mk("image", {preserveAspectRatio:"none", "pointer-events":"none"});
    SC.bd.setAttribute("href", "art/backdrop.png");
    svg.insertBefore(SC.sky, SC.base.nextSibling);
    svg.insertBefore(SC.bd, SC.sky.nextSibling);
  }
  const top = V3.topY(), visW = VB_W + 2 * SC.overX;
  const h = (vbH() + 2 * SC.overY) * 0.30, w = h * 5386 / 531;
  SC.sky.setAttribute("x", -SC.overX); SC.sky.setAttribute("y", -SC.overY);
  SC.sky.setAttribute("width", visW); SC.sky.setAttribute("height", top + SC.overY + 2);
  SC.bd.setAttribute("x", CX - w / 2); SC.bd.setAttribute("y", top - h + h * 0.03);
  SC.bd.setAttribute("width", w); SC.bd.setAttribute("height", h);
}

function paint3d(on){
  for (const [els, attrs] of PAINT3D) for (const e of els()){
    if (!e) continue;
    for (const k in attrs){
      const save = "flat" + k.replace(/-/g, "");
      if (!(save in e.dataset)) e.dataset[save] = e.getAttribute(k) ?? "";
      if (on) e.setAttribute(k, attrs[k]);
      else if (e.dataset[save] === "") e.removeAttribute(k);
      else e.setAttribute(k, e.dataset[save]);
    }
  }
}

// In 3D the players are Craig's markers: a shape standing up on a coloured
// ring on the grass. Shape is the job, colour is the side, as in the flat view.
// The ring's centre is the kid's spot on the field.
const MARK_W = 46, MARK_H = MARK_W * 216 / 245, MARK_RING = 30 * MARK_W / 245;
function markerArt(key){
  const P = SC.players[key], sp = S.play.spots[key], sd = sideOf(key) === "orange" ? "orange" : "blue";
  return "art/marker-" + (P.role === "snap" ? "snap" : P.role === "throw" ? "throw" : P.role === "back" ? "back"
    : sp.tag === "W" ? "wr-" + sd : "extra-" + sd) + ".png";
}
function markers3d(on){
  const focus = !!(S.coach && S.coach.focus);
  const defs = document.querySelector("#field defs");
  if (on && !defs.querySelector("#mshadow")){
    const f = mk("filter", {id:"mshadow", x:"-30%", y:"-30%", width:"160%", height:"170%"});
    f.appendChild(mk("feDropShadow", {dx:0, dy:3, stdDeviation:3, "flood-color":"#000", "flood-opacity":.55}));
    defs.appendChild(f);
  }
  // the ball is Craig's football in 3D
  if (!SC.ballImg && on){
    SC.ballImg = mk("image", {x:-17, y:-13, width:34, height:26, filter:"url(#mshadow)"});
    SC.ballImg.setAttribute("href", "art/ball.png");
    SC.ball.appendChild(SC.ballImg);
  }
  if (SC.ballImg){
    for (const ch of SC.ball.children) if (ch !== SC.ballImg) ch.style.display = on ? "none" : "";
    SC.ballImg.style.display = on ? "" : "none";
  }
  for (const key in SC.players){
    const P = SC.players[key];
    if (!P.img){
      if (!on) continue;
      P.img = mk("image", {x:-MARK_W / 2, y:-(MARK_H - MARK_RING), width:MARK_W, height:MARK_H,
        filter:"url(#mshadow)"});
      P.g.insertBefore(P.img, P.l1);
    }
    // Player Focus is about one kid, so his marker is drawn twice as big
    const z = focus ? 2 : 1;
    if (P.img.dataset.z !== String(z)){
      P.img.dataset.z = z;
      P.img.setAttribute("x", -MARK_W * z / 2); P.img.setAttribute("y", -(MARK_H - MARK_RING) * z);
      P.img.setAttribute("width", MARK_W * z); P.img.setAttribute("height", MARK_H * z);
    }
    const href = markerArt(key);
    if (P.img.getAttribute("href") !== href) P.img.setAttribute("href", href);
    P.img.style.display = on ? "" : "none";
    P.shape.style.display = on ? "none" : "";
    P.halo.style.display = on ? "none" : "";
    // Player Focus shows the kid, not his name: the header already says it
    P.l1.style.display = P.l2.style.display = on && focus ? "none" : "";
  }
}

// runs after the frame has drawn everything, and fixes the few things that
// are rectangles in the flat view and have to be a trapezoid in 3D
function frame3d(t){
  if (!SC || S.screen !== "play") return;
  const svg = document.getElementById("field");
  for (const nm of ["blue", "orange"]) SC.oob[nm].g.setAttribute("opacity", V3.on ? 0 : 1);
  paint3d(V3.on);
  markers3d(V3.on);
  const focus = !!(S.coach && S.coach.focus);
  for (const e of [SC.goal, SC.goalT, SC.side.blue, SC.side.orange]) e.style.display = V3.on && focus ? "none" : "";
  if (SC.sky){ SC.sky.style.display = SC.bd.style.display = V3.on && focus ? "" : "none"; }
  if (S.coach && S.coach.focus) V3.close(VB_W / 2 + SC.overX, -SC.overY, vbH() + 2 * SC.overY); else V3.wide();
  if (SC.grass) SC.grass.setAttribute("opacity", V3.on ? 1 : 0);
  if (!V3.on){ SC.base.setAttribute("fill", "#F7F9F6"); routes3d(t); return; }
  if (!SC.f3d){
    SC.f3d = mk("polygon", {fill:"#2F8A3A"});
    svg.insertBefore(SC.f3d, SC.base.nextSibling);
  }
  grass3d(svg);
  // the ground around the field is the dark out-of-bounds colour
  SC.base.setAttribute("fill", "#1E2521");
  SC.base.setAttribute("x", -SC.overX); SC.base.setAttribute("y", -SC.overY);
  SC.base.setAttribute("width", VB_W + 2 * SC.overX); SC.base.setAttribute("height", vbH() + 2 * SC.overY);
  SC.f3d.setAttribute("opacity", 1);
  SC.f3d.setAttribute("points",
    [[-EDGE, fieldBot()], [EDGE, fieldBot()], [EDGE, V3.far()], [-EDGE, V3.far()]].map(fpt).join(" "));
  routes3d(t);
  // players and the ball get smaller the farther down the field they are
  for (const key in SC.players){
    const g = SC.players[key].g, tr = g.getAttribute("transform") || "";
    const m = /translate\(([-\d.]+) ([-\d.]+)\)/.exec(tr);
    if (m) g.setAttribute("transform", `translate(${m[1]} ${m[2]}) scale(${V3.scaleAt(+m[2]).toFixed(3)})`);
  }
  // the frame rewrites the ball's transform every time it shows it, so this
  // adds the depth scale once per frame and never stacks
  if (SC.ball.getAttribute("opacity") === "1"){
    const bt = SC.ball.getAttribute("transform") || "";
    const bm = /translate\(([-\d.]+) ([-\d.]+)\)/.exec(bt);
    if (bm) SC.ball.setAttribute("transform", `${bt} scale(${V3.scaleAt(+bm[2]).toFixed(3)})`);
  }
}
