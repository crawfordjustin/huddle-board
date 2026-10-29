# Coach Mode + Player Focus (prototype)

This is a feedback prototype. Nothing here changes the app. It's a working mockup of a few ideas for helping 6U–8U kids learn their job in a play, built on top of a copy of the live app so you can click through them.

## Run it

```bash
cd prototypes/coach-mode
python3 -m http.server 8802
```

Open http://localhost:8802, pick a play (51 DIVE in 5 on 5 is a good start) and tap **Coach**.

## What's in it

**Coach Mode** breaks a play into numbered steps, for one kid or the whole team.
- Tap a player or pick a name. The steps come from the play's own geometry: corners, handoffs, the throw and the catch. Nothing is authored by hand.
- Each step is a heading plus one kid-sized sentence ("Go to the THROWER and take the ball with both hands!"). There are no yard counts, and BLUE/ORANGE are colored.
- Markers really run their routes, and a dashed ring stays where each kid lined up. The ball goes snapper → thrower → whoever has it next.
- "Your whole job" lists one player's steps in a single numbered list.
- Controls: Start, Replay, Whole play, Next. Mirror and the slow/full speed toggle work throughout.

**Player Focus** opens full screen when you tap one player in Coach Mode.
- Only that kid is on the field, with the same play, marker and timing.
- Icon step tabs: the current step is yellow, done steps get a check, and steps still to come are dimmed. One instruction per step, plus Back / Replay / Next and Exit.
- Running steps show the route-tree arrow that matches the kid's path for that step.

**3D view** (the 3D button) puts the camera behind the offense.
- It uses the same SVG and no libraries: `W2S` and `F2S` get a perspective projection.
- Grass with mowed stripes, routes painted flat on the grass, pylons on the end zone corners.
- In Player Focus the camera moves in, and the end zone, goalpost, fence and trees fill the top.

**Real field** (the FIELD button) draws the NFL FLAG youth field around the ball.
- 50 yards goal line to goal line, 10-yard end zones, line to gain at midfield.
- No-run zones 5 yards before the end zone and on each side of midfield. Some leagues use only the zone before midfield; that's one line in `noRunZone()`.
- "− Ball on +" moves the spot 5 yards at a time.
- If a run play is open in a no-run zone, a warning says so and gives the real distance ("3 yards from the end zone").

**Settings → Game → End zone color**: black by default.

## How it's built

`original.html` is the hosted app, downloaded as-is. `patch.py` injects the CSS and a few small hooks into it, appends `coach.js`, `view3d.js` and `field.js`, and writes `index.html`. To change anything, edit those files and run `python3 patch.py`.

- `coach.js`: step generation, wording, the Coach panel, Player Focus.
- `view3d.js`: the 3D projection, grass, routes on the grass, markers, backdrop.
- `field.js`: the real youth field, ball spot, no-run warning, pylons, end zone color.
- `art/`: player markers, step and route icons, ball, backdrop.

This is a prototype, not a proposed implementation. It hasn't been run through the browser test suite in `tests/`, and it hasn't been tried on tablet hardware. If any of it is worth keeping, it should be rebuilt properly in `huddle_src.html`.

## One bug we noticed in the app

On run plays the app shows the ball already in the Back's hands at the snap. The prototype has the ball start with the snapper, go to the thrower, then follow the handoffs.
