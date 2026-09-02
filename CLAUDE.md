# Huddle Board

A sideline play tool for 8U (6-on-6) flag football. One HTML file, no runtime
dependencies, works with the radio off. A coach picks a play on an 11" rugged
Android tablet, turns it around, and shows six seven-year-olds what to do.

The audience is a coach who **knows football well** but needs help explaining it
to children. Do not simplify the football. Simplify the explaining.

## The solution

`HuddleBoard.slnx` opens in Visual Studio. Four projects:

```
src/HuddleBoard.Playbook    the play library, the checker, the build, the print
                            documents. All the substance is here.
src/HuddleBoard.Build       the command line — this is what `make` used to be
src/HuddleBoard.Web         an ASP.NET Core host, so F5 serves the real thing
tests/HuddleBoard.Tests     the verification suite, driving real Chromium
```

Nothing depends on Python. The app itself is still one hand-written HTML file
with no framework and no runtime dependencies — that has not changed and should
not.

## Commands

```
dotnet run --project src/HuddleBoard.Build -- build   # data -> dist/
dotnet run --project src/HuddleBoard.Build -- check   # fast play-library check
dotnet run --project src/HuddleBoard.Build -- print   # the paper PDFs
dotnet run --project src/HuddleBoard.Build -- icons   # redraw the app icons
dotnet run --project src/HuddleBoard.Build -- shots   # README screenshots
dotnet test                                           # build, then every check
```

In Visual Studio: set **HuddleBoard.Build** as the startup project and pick the
launch profile for the verb you want, or set **HuddleBoard.Web** and press F5 to
serve the app on localhost. Use the web host rather than opening the file
directly whenever you are touching storage, the install prompt or the service
worker — none of them work on `file://`.

`dist/` is generated in full by `build`; nothing in it is edited by hand.

## The rules that matter

These are not style preferences. Each one exists because breaking it made the
tool worse for actual eight-year-olds.

**1. Nine shapes, and no tenth.** Every route a kid runs is GO, OUT, IN, SIT,
CORNER, POST, WHEEL, SWING or CARRY (`Spots.Shapes`), plus the default rule:
*if the coach did not give you a job, run GO*. The library can grow forever
inside that vocabulary at zero teaching cost. A tenth shape costs every kid on
the team, so adding one is a product decision, not a convenience. `PlayChecker`
warns when a call strip uses a word that is not one of the nine. Play 11
currently violates this ("CROSS deep") and is a known, deliberate exception —
`PlayLibraryChecks.TheVocabularyHasNotGrown` pins that to exactly play 11.

**2. Colour, never left/right.** Sides are BLUE and ORANGE, because 8U players
confuse left and right and because left/right inverts depending on whether the
coach is beside them or facing them. The sidelines are also OUR SIDE / THEIR
SIDE — the one landmark that holds for a whole game. Parents move for the sun;
teammates move because only six are on the field.

**3. No letters on the field.** Not `W`, `SN`, `QB`, `T`. A kid knows he is
WIDE BLUE. Markers carry shape (circle = receiver, diamond = back, hexagon =
thrower, square = snapper) plus colour plus the spoken name. This was tested at
a real practice and the letters did not land. `LabelChecks` asserts no letter
tag can come back.

**4. The field never mirrors, the players do.** `W2S()` transforms players,
routes and the ball — it mirrors. `F2S()` transforms field paint — it never
moves. Mirroring the paint with the players is physically wrong and was a real
bug. The call strip rewrites BLUE↔ORANGE on mirror instead.

**5. The intro art ships inside the file.** It is one illustration, authored at
`art/intro-art.png` and inlined by the build as a data URI, because the app is
one file that opens with the radio off and a second request is not available to
it. Replacing that file is the entire process for changing the art. `IntroArt.cs`
downsamples to 1600px and re-encodes, picking whichever of WebP, JPEG and PNG
comes out smallest — a full-bleed illustration is the case PNG is worst at, and
the art's weight is the app's weight on every cold open and every service-worker
update. `IntroChecks` holds the size ceiling, and holds that the picture actually
decoded: a data URI the build got wrong fails silently, with a correct layout, a
working button and an empty panel.

This replaced six kids drawn from a pose table in SVG. The figures were flat and
front-on but standing on a receding field, so nothing sat in the same space, and
no amount of tuning pose data fixes a perspective that was never coherent. The
cost of the swap is honest and worth writing down: **rule 3 no longer reaches the
intro art.** A raster cannot be swept for letters, and the illustration in the
repo today has jersey numbers and a lettered scoreboard in it. It is also blue
against **red**, not blue against ORANGE. Neither is on the field diagram, where
the rules bite, but the first screen a kid sees is now teaching a different
colour pair than every screen after it. That is a live design debt, not a
decision.

The football (`BALL_BODY`/`BALL_LACES`) is one shape shared between the play
screen's ball marker and nothing else now — a kid should not have to learn two
pictures of the same object. `BallChecks` holds that marker's shape, heading and
landing spot.

**6. A coach's own play names outlive the build.** Every team ends up calling
22 DIVE something else. Both halves of a name are editable — the real name you
say to another coach and the fun name the kids shout — from the pencil on a
library row, which is the coach's screen and never the one turned around at the
kids. The overrides live in `localStorage` under `hb.names`, keyed by play id
and never in the build, so they survive an update; only what actually differs
from the shipped name is stored, so a play the coach retyped identically is not
"renamed". Search matches the custom name *and* the shipped one, so renaming can
never hide a play. Setup shows how many are renamed and resets them all. This is
also the one string in the app a coach types, so it is the one string that has to
be escaped before it reaches markup — see `esc()`. `RenameChecks` holds all of
it, including the survives-an-update part, against a real new build taken through
the service worker. Custom names are per tablet and do not reach the printed
playbook, which is built from source.

Setup's last row, **Start over**, puts the whole tablet back: the starting deck,
every shipped name, every setting, and which sideline is ours. It deliberately
leaves the game log alone. A log is a recording rather than a preference — the
one thing on the tablet that cannot be derived again — and it has its own Clear
one row up. Everything destructive in Setup arms on the first tap and acts on the
second (`confirmTap`), and the arming lapses on its own, so a stray thumb on the
way past costs nothing. `ResetChecks` holds all of that, including the log.

**7. The deck answers the same two questions the library does.** On the sideline
the thought is "I need a goal line play", not "I need play 13", so the deck
carries the same run/pass and situation chips the library has, with the same
`.fchip` styling — two screens should not teach two controls for one idea. Every
tile shows its situation at every column count; it used to be hidden the moment
the grid went past two columns, which is every deck size big enough to have to
hunt through.

The filter is in memory only, never saved. A deck filtered to GOAL LINE and then
persisted would have a coach pick the tablet up next week, see two plays and
think his deck had been eaten — so the header count reads "2 of 14" whenever a
filter is on, and going back through the intro clears it. The bar stays away
below six plays, where the whole deck is on screen at a glance and a filter is
just one more thing to read. `DeckFilterChecks` holds all of that.

**8. An update never interrupts a live play.** The service worker installs a new
build as a *waiting* worker and shows "Update ready". It swaps only on tap.

## Source layout

```
huddle_src.html                 the whole app — markup, CSS, JS. __DATA__,
                                __INTRO_ART__ and __VERSION__ are substituted at
                                build time. This is the only UI file, and it is
                                not C#.
art/intro-art.png               the intro illustration, as authored. Swap this
                                file and rebuild; nothing else changes.

src/HuddleBoard.Playbook/
  Model.cs, Geometry.cs         the record types; Pt and Num
  Formations.cs                 where everybody lines up
  Plays.cs                      the first 14 plays: route geometry in yards
  PlaysMore.cs                  plays 15-24
  Spots.cs                      spot names, the 9 shapes, the default rule
  PlayTexts.cs                  what the tablet says, plays 1-14
  PlayTextsMore.cs              the same for 15-24
  Library.cs                    joins the two halves of each pair
  PlayChecker.cs                the legality/safety/vocabulary checker
  ProtoExporter.cs              plays + spots -> dist/proto_data.json
  AppBuilder.cs                 proto_data + huddle_src -> the shipping forms
  DeployReadme.cs               the notes that ship inside dist/deploy/
  IconRenderer.cs               app icons, drawn to still read at 48px
  IntroArt.cs                   art/intro-art.png -> a data URI small enough to
                                inline in the one file the app ships as
  JsonWriter.cs                 exact control over how the data is spelled
  Pipeline.cs, Workspace.cs     what "build" means, and where the repo is
  Print/                        the paper playbook, cards and rotation sheet

src/HuddleBoard.Build/          the CLI, plus the Playwright PDF and screenshot
                                steps that need a browser
src/HuddleBoard.Web/            static host with the two headers that matter
tests/HuddleBoard.Tests/        the verification suite
```

### Coordinates

Yards. `x` = left/right of the snapper (negative = offense's left = BLUE),
`y` = downfield from the line of scrimmage (negative = backfield). The sideline
is at `|x| = 15.7`, the goal line at `y = 15`.

### Adding a play

1. Append to `Recent` in `PlaysMore.cs` — geometry, tagline, notes, mistake.
2. Add matching text in `PlayTextsMore.cs` — the call strip and the per-spot
   instructions, in spot language. Call-strip labels use LEFT/RIGHT spot names
   (`SLOT LEFT`); `ProtoExporter.Recolor` turns them into BLUE/ORANGE for
   display. Getting this backwards means the label will not resolve and the
   export will throw.
3. Add `KidNames[num]` and `Ball[num]` in `ProtoExporter.cs`. `Ball` is the
   thrower's *first read*, or the ball carrier — one rule, no judgement calls.
4. `check` until clean, then `dotnet test`.

Prefer **concept × formation** over inventing concepts. Most concepts port to
two or three of the four formations, so the honest way to grow the library is
to run the concepts you have from more places, not to draw 100 one-offs. At 8U
there are perhaps 40-50 genuinely distinct ideas before you are drawing
distinctions a second-grader cannot perceive.

## PlayChecker

Hand-drawing routes does not scale, and every bug found by eye on the first
fourteen plays is encoded here. It is a hard gate on `ProtoExporter`: bad
geometry cannot reach a tablet.

The collision rule is **time-aware**. Two routes crossing on paper is not a
collision if the players are there at different moments, which is usually the
case. It models every kid at 6 yd/s from the snap (pre-snap motion is a head
start) and flags pairs that are genuinely within 1.6 yards at the same instant.
A purely geometric rule produced constant false positives on legitimate
concepts.

Calibration note: **the original fourteen plays pass with zero errors**, and
nothing was tuned to make that true. If a change makes them fail, the change is
wrong, not the plays. `PlayLibraryChecks.TheOriginalFourteenStillPassClean`
holds that line.

Warnings (not errors) are drawing-legibility notes — usually an arrowhead
finishing near another route's line. Stacked-OUT concepts like Triple Out do
this by design and are fine.

## Testing

`dotnet test` drives real Chromium against a real build. There are no unit tests
on the UI on purpose: nearly every bug this project has had was a layout or
timing bug that only appears on screen at a particular size. Checks run across
five tablet shapes (`AppFixture.Sizes`) — two landscape, two portrait, and a
short landscape that catches anything relying on vertical room.

The app opens on an intro screen — art and one START button — so
`AppFixture.OpenAppAsync` taps through it and hands every other check the deck.
Pass `intro: true` to stay on it; `IntroChecks` is the only thing that does.

`RenameChecks`, `ResetChecks` and `PwaChecks` run against the hosted build over a local
`StaticSite` rather than the standalone file, because both are about storage and
service workers and a `file://` origin has neither.

`LabelChecks` sweeps 24 plays × 2 mirror states × 2 stages × 5 viewports = 480
states. Several checks pad the library out to 100 plays
(`AppFixture.InjectPlaysAsync`) to judge the UI at a size it has not reached yet.

Everything shares one browser and one build and runs in sequence — two checks
racing over the same `dist/` is not a real signal. The first run downloads
Chromium (about 190 MB); after that it is cached in your profile.

When a sweep test can only pass by finding nothing, give it something to assert
it actually looked — `LabelChecks` checks it saw six players per state, so the
whole sweep cannot pass vacuously.

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
- **`justify-content: center` on a scroller hides its first row.** Setup grew
  past what 600px of landscape can hold, so `.setrows` scrolls — and centring it
  with `justify-content` puts the first row above the top of the scroll range,
  permanently out of reach. Auto margins on the first and last child centre the
  group while it fits and collapse to zero once it does not.
- **`.tilemain` is taller than the tile it sits in.** The card is
  `display:flex; align-items:center`, so its child is sized to its own content
  and cheerfully overflows the card — which means `.tilemain`'s own box never
  reports the overflow, and anything measuring against it sees nothing wrong.
  The card clips, so the card is what you measure against. This cost a real bug
  twice: once in `fitTiles`, and once in `NameChecks`, which had been asserting
  against `.tilemain` and therefore could not see the clipping it existed to
  catch.
- **`fitText()` only ever asked a horizontal question** — does this run past the
  side, does it take more than two lines. Nothing asked whether the block fits
  the card's height, so a two-line name on a tight grid pushed the content
  taller than the tile; and because the tile centres its children, the clipping
  landed evenly on the top and bottom — on the RUN/PASS row and the formation,
  the two things a coach reads first, while the tagline sat there in full.
  `fitDeck()` adds the vertical pass: the tagline goes first because it is the
  only optional line, then the name shrinks for the whole deck at once, because
  one size across the grid is what makes it scannable.
- **Reserve the scrollbar gutter.** A scrollbar appearing after render narrows
  rows and ellipsises names that had already been fitted.
- **`.chip` was taken.** The play screen's formation badge already used it; the
  library filter chips are `.fchip`. Watch for class collisions in a single-file
  app.
- **Browser storage on `file://`** may be blocked by Android, so deck and
  settings will not persist on the standalone copy. That is the main reason to
  prefer the hosted build.
- **`Num` remembers how you wrote it.** `new Pt(0, 0)` exports as `[0,0]` and
  `new Pt(0.0, -2.0)` as `[0.0,-2.0]`. Both parse identically in JavaScript, so
  this only affects the diff — but it is why the coordinates are `Num` and not
  `double`.
- **Generated files are UTF-8 with no BOM and LF endings**, on every platform.
  `Workspace.WriteText` is the only thing that should write into `dist/`;
  `File.WriteAllText` would use the platform default and produce CRLF on
  Windows.

## Deploy

`dist/deploy/` is a static site — no server code, no build step. Drop it at the
site root of an Azure App Service (or any static host; the config file for each
is included). Only two things actually matter: serve `.webmanifest` as
`application/manifest+json`, and send `Cache-Control: no-cache` for
`index.html` and `sw.js`. Miss the second and tablets never see a new build.

From Visual Studio, right-click **HuddleBoard.Web** → **Publish** → Azure App
Service. Publishing runs the build first and stages `dist/deploy/` into
`wwwroot`, and the host applies the same two rules the generated `web.config`
applies. Either route works; the static one has fewer moving parts.

### The pipeline

`.github/workflows/deploy.yml` deploys every push to `master` to the
`huddleboard` App Service (`rg-default`, westus2, Windows). It runs `check`,
runs `build`, and zip-deploys `dist/HuddleBoard-deploy.zip` — the artifact the
build already produces, which is exactly `dist/deploy/` flattened, `web.config`
included. Nothing in `dist/` is committed, so CI builds it from source.

The deploy is gated on `check`, not on `dotnet test`. That is a deliberate
trade for a fast sideline fix; the browser suite still has to be run by hand
before anything that touches layout, storage or the service worker.

CI stamps `HB_VERSION` as `yyyy.MM.dd-HHmm+<short sha>`. That string is the
service worker's cache name and the version on the deck note, so it has to
change on every deploy — a repeated version means tablets keep the old build.

Auth is OIDC, so there is no Azure credential in this repository. The Entra app
registration `huddle-board-github-deploy` holds a federated credential and holds
Website Contributor scoped to the `huddleboard` site alone — not the resource
group, not the subscription. GitHub stores only three identifiers, as repository
*variables* — `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` —
because they are not secrets and a failed login is easier to read unmasked.

**The trust is bound to the environment, not the branch**, and the subject is
spelled in numbers:

```
repo:crawfordjustin@66266306/huddle-board@1353173891:environment:production
```

Two things in there are easy to get wrong, and both cost a failed run:

*The environment, not the ref.* Because the job declares
`environment: production`, GitHub sends an `:environment:` subject and never the
`ref:refs/heads/master` form a branch-scoped credential would match. Removing
`environment:` from the workflow breaks the login outright. The branch
restriction is real, but it lives on the GitHub side — the `production`
environment has a deployment branch policy allowing only `master` — so that
policy and this credential have to stay in step.

*The IDs, not the names.* GitHub presents immutable owner and repo IDs
(`crawfordjustin@66266306`, `huddle-board@1353173891`), not the plain path. This
is the better form: it survives a rename, and nobody who later claims a freed-up
`crawfordjustin/huddle-board` inherits the trust. But it means the credential
cannot be written from the repo name alone — read the IDs from
`gh api repos/crawfordjustin/huddle-board -q '.id, .owner.id'`, or just read the
subject back out of the `AADSTS700213` error, which prints exactly what was
presented.

Publish profiles do not work here at all — SCM basic publishing credentials are
disabled on the app, which is the Azure default and worth leaving alone.

`dist/HuddleBoard.html` is the whole app in one file for a tablet with no
network at all.

Full deploy notes, including how to get it onto a coach's tablet and lose the
address bar, are generated into `dist/deploy/README.md`.

## Out of scope (decided, not forgotten)

- **Coach-authored plays.** The library grows by editing `PlaysMore.cs`, not
  through UI. Revisit only with the checker validating user input.
- **Rewriting the app in Blazor.** The single HTML file is the product: it opens
  from a file, on a tablet, with no network and no runtime. A WASM runtime works
  against that, and every gotcha above would have to be re-solved.
- **The flip/orientation toggle.** Removed deliberately — all coaches teach the
  same way instead.
- **Parent/assistant logging apps.** The event log already records everything
  with millisecond timestamps and monotonic-vs-wall drift detection, so a v2
  reconciliation is possible without refactoring. Not built.
