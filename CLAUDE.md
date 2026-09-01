# Huddle Board

A sideline play tool for 8U (6-on-6) flag football. One HTML file, no runtime
dependencies, works with the radio off. A coach picks a play on an 11" rugged
Android tablet, turns it around, and shows six seven-year-olds what to do.

The audience is a coach who **knows football well** but needs help explaining it
to children. Do not simplify the football. Simplify the explaining.

## Commands

```
make build     # data -> dist/proto_data.json -> dist/{HuddleBoard.html, deploy/, zip}
make check     # fast: legality + safety + vocabulary pass over the play library
make test      # build, then every check (drives real Chromium; ~2-4 min)
make print     # the paper playbook, field cards and rotation sheet -> PDFs
```

`dist/` is generated in full by `make build`; nothing in it is edited by hand.

## The rules that matter

These are not style preferences. Each one exists because breaking it made the
tool worse for actual eight-year-olds.

**1. Nine shapes, and no tenth.** Every route a kid runs is GO, OUT, IN, SIT,
CORNER, POST, WHEEL, SWING or CARRY (`spots.py: SHAPES`), plus the default rule:
*if the coach did not give you a job, run GO*. The library can grow forever
inside that vocabulary at zero teaching cost. A tenth shape costs every kid on
the team, so adding one is a product decision, not a convenience. `check_plays.py`
warns when a call strip uses a word that is not one of the nine. Play 11
currently violates this ("CROSS deep") and is a known, deliberate exception.

**2. Colour, never left/right.** Sides are BLUE and ORANGE, because 8U players
confuse left and right and because left/right inverts depending on whether the
coach is beside them or facing them. The sidelines are also OUR SIDE / THEIR
SIDE — the one landmark that holds for a whole game. Parents move for the sun;
teammates move because only six are on the field.

**3. No letters on the field.** Not `W`, `SN`, `QB`, `T`. A kid knows he is
WIDE BLUE. Markers carry shape (circle = receiver, diamond = back, hexagon =
thrower, square = snapper) plus colour plus the spoken name. This was tested at
a real practice and the letters did not land. `tests/verify_labels.py` asserts
no letter tag can come back.

**4. The field never mirrors, the players do.** `W2S()` transforms players,
routes and the ball — it mirrors. `F2S()` transforms field paint — it never
moves. Mirroring the paint with the players is physically wrong and was a real
bug. The call strip rewrites BLUE↔ORANGE on mirror instead.

**5. An update never interrupts a live play.** The service worker installs a new
build as a *waiting* worker and shows "Update ready". It swaps only on tap.

## Source layout

```
huddle_src.html     the whole app — markup, CSS, JS. __DATA__ and __VERSION__
                    are substituted at build time. This is the only UI file.
plays.py            formations + the first 14 plays: route geometry in yards
plays_more.py       plays 15-24 (imported and appended by plays.py)
spots.py            spot names, the 9 SHAPES, DEFAULT_RULE, PLAY_TEXT
spots_more.py       PLAY_TEXT for 15-24 (imported and merged by spots.py)
check_plays.py      the legality/safety/vocabulary checker — see below
export_proto.py     plays + spots -> dist/proto_data.json (gated on the checker)
build_app.py        proto_data + huddle_src -> the three shipping forms
make_icons.py       app icons, drawn to still read at 48px
print/              the paper pipeline: playbook, field cards, rotation sheet
tests/              the verification suite; harness.py holds the shared plumbing
```

### Coordinates

Yards. `x` = left/right of the snapper (negative = offense's left = BLUE),
`y` = downfield from the line of scrimmage (negative = backfield). The sideline
is at `|x| = 15.7`, the goal line at `y = 15`.

### Adding a play

1. Append to `NEW_PLAYS` in `plays_more.py` — geometry, tagline, notes, mistake.
2. Add matching `PLAY_TEXT` in `spots_more.py` — the call strip and the
   per-spot instructions, in spot language. Call-strip labels use LEFT/RIGHT
   spot names (`SLOT LEFT`); `export_proto.recolor()` turns them into
   BLUE/ORANGE for display. Getting this backwards means the label will not
   resolve and the export will raise.
3. Add `BALL[num]` and `KID_NAMES[num]` in `export_proto.py`. `BALL` is the
   thrower's *first read*, or the ball carrier — one rule, no judgement calls.
4. `make check` until clean, then `make test`.

Prefer **concept × formation** over inventing concepts. Most concepts port to
two or three of the four formations, so the honest way to grow the library is
to run the concepts you have from more places, not to draw 100 one-offs. At 8U
there are perhaps 40-50 genuinely distinct ideas before you are drawing
distinctions a second-grader cannot perceive.

## check_plays.py

Hand-drawing routes does not scale, and every bug found by eye on the first
fourteen plays is encoded here. It is a hard gate on `export_proto.py`: bad
geometry cannot reach a tablet.

The collision rule is **time-aware**. Two routes crossing on paper is not a
collision if the players are there at different moments, which is usually the
case. It models every kid at 6 yd/s from the snap (pre-snap motion is a head
start) and flags pairs that are genuinely within 1.6 yards at the same instant.
A purely geometric rule produced constant false positives on legitimate
concepts.

Calibration note: **the original fourteen plays pass with zero errors**, and
nothing was tuned to make that true. If a change makes them fail, the change is
wrong, not the plays.

Warnings (not errors) are drawing-legibility notes — usually an arrowhead
finishing near another route's line. Stacked-OUT concepts like Triple Out do
this by design and are fine.

## Testing

Everything drives real Chromium against a real build. There are no unit tests
on purpose: nearly every bug this project has had was a layout or timing bug
that only appears on screen at a particular size. Checks run across five tablet
shapes (`harness.SIZES`) — two landscape, two portrait, and a short landscape
that catches anything relying on vertical room.

`tests/verify_labels.py` sweeps 24 plays × 2 mirror states × 2 stages × 5
viewports = 480 states.

Several checks pad the library out to 100 plays (`harness.INJECT_PLAYS`) to
judge the UI at a size it has not reached yet.

## Gotchas that have already cost a day

- **`min-height: 0` on flex containers.** A flex item defaults to
  `min-height: auto`, so a scroll container inside it grows instead of
  scrolling and rows fall off the bottom, unreachable and with no scrollbar.
  This silently capped the library at ~22 plays.
- **Do not measure text overflow with `scrollHeight` vs `clientHeight`.**
  With `line-height` under 1 the glyph box is taller than the line box, so that
  comparison is true even for one word on one line. `fitText()` counts laid-out
  lines with a Range instead.
- **The webfont is absent offline** and the fallback is much wider. CSS sizing
  alone will clip. `fitText()` measures and shrinks; deck names then all drop to
  the smallest fitted size so the grid scans as one.
- **Reserve the scrollbar gutter.** A scrollbar appearing after render narrows
  rows and ellipsises names that had already been fitted.
- **`.chip` was taken.** The play screen's formation badge already used it; the
  library filter chips are `.fchip`. Watch for class collisions in a single-file
  app.
- **Browser storage on `file://`** may be blocked by Android, so deck and
  settings will not persist on the standalone copy. That is the main reason to
  prefer the hosted build.

## Deploy

`dist/deploy/` is a static site — no server code, no build step. Drop it at the
site root of an Azure App Service (or any static host; the config file for each
is included). Only two things actually matter: serve `.webmanifest` as
`application/manifest+json`, and send `Cache-Control: no-cache` for
`index.html` and `sw.js`. Miss the second and tablets never see a new build.

`dist/HuddleBoard.html` is the whole app in one file for a tablet with no
network at all.

Full deploy notes, including how to get it onto a coach's tablet and lose the
address bar, are generated into `dist/deploy/README.md`.

## Out of scope (decided, not forgotten)

- **Coach-authored plays.** The library grows by editing `plays_more.py`, not
  through UI. Revisit only with the checker validating user input.
- **The flip/orientation toggle.** Removed deliberately — all coaches teach the
  same way instead.
- **Parent/assistant logging apps.** The event log already records everything
  with millisecond timestamps and monotonic-vs-wall drift detection, so a v2
  reconciliation is possible without refactoring. Not built.
