/* ------------------------------------------------------------ real field
   MOCKUP. The NFL FLAG youth field, drawn around wherever the ball is:
   50 yards goal line to goal line, 10-yard end zones, the line to gain at
   midfield, and no-run zones 5 yards before the end zone and on each side
   of midfield. The play's own view (6.5 yards behind the ball to 15 past
   it) does not change; the real lines are drawn where they fall inside it.
   Coordinates stay the app's: y is yards downfield from the ball.        */
const RF = {on:false, spot:5};             // spot: yards from our own goal line
const RF_LEN = 50, RF_MID = 25, RF_EZ = 10, RF_NRZ = 5;

function spotName(s){
  return s === RF_MID ? "Midfield" : s < RF_MID ? `Our ${s}` : `Their ${RF_LEN - s}`;
}
function setRealField(on){
  RF.on = on;
  const b = document.getElementById("rfield"), w = document.getElementById("rfspot");
  if (b){ b.classList.toggle("on", on); b.innerHTML = `Field<small>${on ? "real" : "play"}</small>`; }
  if (w) w.classList.toggle("hidden", !on);
  paintSpot();
}
function moveSpot(d){
  RF.spot = Math.max(1, Math.min(RF_LEN - 1, RF.spot + d));
  paintSpot();
}
function paintSpot(){
  const l = document.getElementById("rfval");
  if (l) l.innerHTML = `<small>Ball on</small>${esc(spotName(RF.spot))}`;
}
// End zone colour: a setting, black unless the coach picks another. Kept in
// its own key so it never touches the app's own saved settings.
const EZ_COLORS = [["black", "Black", "#141A16"], ["navy", "Navy", "#1E2F5C"], ["red", "Red", "#B3171B"],
                   ["purple", "Purple", "#4B2A80"], ["green", "Green", "#1B5E3A"]];
let ezColor = "black";
try { ezColor = localStorage.getItem("hb.ezcolor") || "black"; } catch(e){}
const ezHex = () => (EZ_COLORS.find(c => c[0] === ezColor) || EZ_COLORS[0])[2];
function setEzColor(v){ ezColor = v; try { localStorage.setItem("hb.ezcolor", v); } catch(e){} }
// the row on Settings > Game, built the same way as the app's own rows
function ezSettingRow(){
  return ["game", "End zone color", "The color painted in both end zones on the field.",
    `<div class="seg" id="ezseg">${EZ_COLORS.map(([v, name, hex]) =>
      `<button data-ez="${v}" class="${v === ezColor ? "on" : ""}"><i class="ezsw" style="background:${hex}"></i>${name}</button>`
    ).join("")}</div>`];
}
function bindEzSetting(){
  const g = document.getElementById("ezseg");
  if (!g) return;
  g.onclick = e => {
    const b = e.target.closest("button"); if (!b) return;
    setEzColor(b.dataset.ez);
    g.querySelectorAll("button").forEach(x => x.classList.toggle("on", x === b));
  };
}

// A pylon: the orange-red marker on each corner of an end zone. In 3D it is a
// little box that stands up off the grass and shrinks with distance; flat, it
// is the square you would see from above.
function pylon(g, x, y){
  const [px, py] = F2S(x, y);
  if (!V3.on){
    g.appendChild(mk("rect", {x:px - 10, y:py - 10, width:20, height:20, rx:3,
      fill:"#E8231B", stroke:"#8E0E0B", "stroke-width":2}));
    return;
  }
  // A real box standing on the corner, built in yards and put through the
  // same projection as the field, so it leans and shrinks with the grass.
  // Bigger than a real 4-inch pylon so a kid can see it from the line.
  const s = .15, H = 1.15;
  const up = (q) => { const [sx, sy] = F2S(q[0], q[1]); return [sx, sy - H * V3.vscale(q[1])]; };
  const base = [[x - s, y - s], [x + s, y - s], [x + s, y + s], [x - s, y + s]];
  const gb = base.map(q => F2S(q[0], q[1])), gt = base.map(up);
  const poly = (pts, fill) => g.appendChild(mk("polygon", {points:pts.map(q => q.join(",")).join(" "), fill}));
  const plate = [[x - s * 1.5, y - s * 1.5], [x + s * 1.5, y - s * 1.5], [x + s * 1.5, y + s * 1.5], [x - s * 1.5, y + s * 1.5]];
  poly(plate.map(q => F2S(q[0], q[1])), "#1A1A1A");
  // the face toward the middle of the field, the face toward us, then the top
  const inner = x < 0 ? [1, 2] : [3, 0];
  poly([gb[inner[0]], gb[inner[1]], gt[inner[1]], gt[inner[0]]], "#A3100D");
  poly([gb[0], gb[1], gt[1], gt[0]], "#E5231C");
  poly(gt, "#FF5B4B");
}

function bindRealField(){
  const b = document.getElementById("rfield");
  if (!b || b.onclick) return;
  b.onclick = () => setRealField(!RF.on);
  document.getElementById("rfminus").onclick = () => moveSpot(-5);
  document.getElementById("rfplus").onclick = () => moveSpot(5);
  setRealField(RF.on);
}

// Which no-run zone the ball is in, if any. A ball spotted inside one
// (5 yards either side of midfield, or the 5 yards before their goal line)
// cannot be run; the offense has to pass.
function noRunZone(spot){
  if (spot >= RF_MID - RF_NRZ && spot <= RF_MID + RF_NRZ) return "midfield";
  if (spot >= RF_LEN - RF_NRZ) return "goal";
  return null;
}
// a run is any play where the ball is carried, not thrown: dives, sweeps,
// reverses, keepers
const isRunPlay = p => p.ball.mode === "carry";

function nrzWarning(){
  const wrap = document.querySelector(".play .fieldwrap");
  if (!wrap) return;
  let w = document.getElementById("nrzwarn");
  if (!w){
    w = document.createElement("div");
    w.id = "nrzwarn"; w.className = "nrzwarn"; w.setAttribute("role", "alert");
    wrap.appendChild(w);
  }
  const zone = RF.on ? noRunZone(RF.spot) : null;
  const show = !!zone && isRunPlay(S.play);
  w.classList.toggle("show", show);
  if (show){
    // the real distance, so "their 3" does not claim to be 5 yards out
    const yd = zone === "goal" ? RF_LEN - RF.spot : Math.abs(RF_MID - RF.spot);
    const mark = zone === "goal" ? "the end zone" : "midfield";
    const where = yd === 0 ? `at ${mark}` : `${yd} yard${yd > 1 ? "s" : ""} from ${mark}`;
    const html = `<b>No-run zone</b><span>The ball is ${where}. You can't run this play here. Pick a pass play.</span>`;
    if (w.dataset.html !== html){ w.innerHTML = html; w.dataset.html = html; }
  }
}

function fieldFrame(){
  if (!SC || S.screen !== "play") return;
  nrzWarning();
  const svg = document.getElementById("field");
  // the play's own field paint: a goal 15 yards out and dashes at 5 and 10
  const fake = [SC.goal, SC.goalT, ...SC.yard];
  fake.forEach(e => e.setAttribute("opacity", RF.on ? 0 : 1));
  if (!SC.rf){
    SC.rf = mk("g", {"pointer-events":"none"});
    svg.insertBefore(SC.rf, SC.los);
    const defs = svg.querySelector("defs");
    const pat = mk("pattern", {id:"nrz", width:16, height:16, patternUnits:"userSpaceOnUse",
      patternTransform:"rotate(45)"});
    pat.appendChild(mk("rect", {width:7, height:16, fill:"#5C665F", "fill-opacity":.16}));
    defs.appendChild(pat);
    // the same hatching in white, for the grass in 3D
    const pat3 = pat.cloneNode(true);
    pat3.id = "nrz3d";
    pat3.firstChild.setAttribute("fill", "#FFFFFF");
    defs.appendChild(pat3);
  }
  const g = SC.rf;
  g.innerHTML = "";
  // pylons sit on top of the sideline paint, under the routes and players
  if (!SC.pyl){
    SC.pyl = mk("g", {"pointer-events":"none"});
    svg.insertBefore(SC.pyl, SC.side.orange.nextSibling);
  }
  const pg = SC.pyl;
  pg.innerHTML = "";
  // the play's own end zone, the sliver past its goal line, in the end zone colour
  SC.goal.setAttribute("fill", ezHex()); SC.goal.setAttribute("fill-opacity", 1);
  SC.goalT.setAttribute("fill", "#FFFFFF");
  if (!RF.on){
    if (S.coach && S.coach.focus && V3.on){
      // Player Focus shows the whole end zone: a pylon on each of its corners
      for (const y of [TOPY, TOPY + 10]){ pylon(pg, -EDGE, y); pylon(pg, EDGE, y); }
    } else { pylon(pg, -EDGE, 13.3); pylon(pg, EDGE, 13.3); }
    return;
  }

  const lo = fieldBot(), hi = V3.on ? V3.far() : TOPY;          // what the view shows, in play yards
  const Y = fy => fy - RF.spot;              // field yards (from our goal) -> play yards
  // flat view: dark paint on the white board; 3D: white paint on grass
  const P = V3.on
    ? {ez:ezHex(), ezOp:1, ezText:"#FFFFFF", goal:"#FFFFFF", nrz:"#FFFFFF", mid:"#FFFFFF",
       text:"#FFFFFF", tag:"#FFFFFF", hatch:"url(#nrz3d)"}
    : {ez:ezHex(), ezOp:1, ezText:"#FFFFFF", goal:"#1B5E3A", nrz:"#8A948C", mid:"#141A16",
       text:"#4A554E", tag:"#141A16", hatch:"url(#nrz)"};
  const band = (a, b, attrs, label, labelAttrs) => {
    const y1 = Math.max(lo, Y(a)), y2 = Math.min(hi, Y(b));
    if (y2 <= y1) return;
    g.appendChild(mk("polygon", {...attrs,
      points:[[-EDGE, y1], [EDGE, y1], [EDGE, y2], [-EDGE, y2]].map(fpt).join(" ")}));
    if (label) text(label, (y1 + y2) / 2, labelAttrs);
  };
  const line = (fy, attrs) => {
    const y = Y(fy);
    if (y < lo || y > hi) return false;
    const a = F2S(-EDGE, y), b = F2S(EDGE, y);
    const at = {...attrs};
    if (V3.on) delete at["stroke-dasharray"];        // a real field's lines are solid
    g.appendChild(mk("line", {x1:a[0], y1:a[1], x2:b[0], y2:b[1], ...at}));
    return true;
  };
  const text = (s, y, attrs = {}) => {
    const p = F2S(0, y);
    const t = mk("text", {x:p[0], y:p[1], "text-anchor":"middle", "dominant-baseline":"central",
      "font-family":"Barlow Condensed, Arial Narrow, sans-serif", "font-weight":700,
      "letter-spacing":4, "font-size":22, fill:P.text, opacity:V3.on ? .85 : .75, ...attrs});
    t.textContent = s;
    g.appendChild(t);
  };
  const tag = (s, fy, attrs = {}) => {
    const y = Y(fy);
    if (y < lo || y > hi) return;
    const p = F2S(-EDGE + (V3.on ? 2.2 : 0.5), y);   // clear of the sideline, which leans in 3D
    const t = mk("text", {x:p[0], y:p[1] - 7, "font-family":"Barlow Condensed, Arial Narrow, sans-serif",
      "font-weight":700, "letter-spacing":1.5, "font-size":15, fill:P.tag, ...attrs});
    t.textContent = s;
    g.appendChild(t);
  };

  // end zones
  band(-RF_EZ, 0, {fill:P.ez, "fill-opacity":P.ezOp}, "OUR END ZONE", {fill:P.ezText, "font-size":26, "letter-spacing":6});
  band(RF_LEN, RF_LEN + RF_EZ, {fill:P.ez, "fill-opacity":P.ezOp}, "END ZONE", {fill:P.ezText, "font-size":30, "letter-spacing":8});
  // no-run zones: 5 yards before their end zone, and either side of midfield
  band(RF_LEN - RF_NRZ, RF_LEN, {fill:P.hatch}, "NO-RUN ZONE");
  band(RF_MID - RF_NRZ, RF_MID + RF_NRZ, {fill:P.hatch}, "NO-RUN ZONE");
  // lines
  line(0, {stroke:P.goal, "stroke-width":6});
  line(RF_LEN, {stroke:P.goal, "stroke-width":6});
  line(RF_LEN + RF_EZ, {stroke:P.goal, "stroke-width":3, "stroke-dasharray":"10 8"});
  line(RF_MID - RF_NRZ, {stroke:P.nrz, "stroke-width":2.5, "stroke-dasharray":"8 8"});
  line(RF_MID + RF_NRZ, {stroke:P.nrz, "stroke-width":2.5, "stroke-dasharray":"8 8"});
  line(RF_LEN - RF_NRZ, {stroke:P.nrz, "stroke-width":2.5, "stroke-dasharray":"8 8"});
  line(RF_MID, {stroke:P.mid, "stroke-width":6});
  // what each line is, at the BLUE sideline
  tag("GOAL LINE", RF_LEN, {fill:P.goal});
  tag("BACK OF END ZONE", RF_LEN + RF_EZ, {fill:P.goal});
  tag(RF.spot < RF_MID ? "MIDFIELD · LINE TO GAIN" : "MIDFIELD", RF_MID);
  tag("OUR GOAL LINE", 0, {fill:P.goal});
  // pylons on all four corners of both end zones, wherever they are in view
  for (const fy of [-RF_EZ, 0, RF_LEN, RF_LEN + RF_EZ]){
    const y = Y(fy);
    if (y >= lo - .5 && y <= hi + .5){ pylon(pg, -EDGE, y); pylon(pg, EDGE, y); }
  }
}
