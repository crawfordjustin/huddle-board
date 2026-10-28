# Huddle Board

A sideline play tool for 6U to 8U flag football, or any division where the
coach is allowed on the field with the kids — 5-on-5, 6-on-6 or 7-on-7. One HTML
file, no runtime dependencies, works with the radio off. A coach picks a play on
an 11" rugged Android tablet, turns it around, and shows a handful of
seven-year-olds what to do.

The audience is a coach who **knows football well** but needs help explaining it
to children. Do not simplify the football. Simplify the explaining.

## The solution

`HuddleBoard.slnx` opens in Visual Studio. Four projects:

```
src/HuddleBoard.Playbook    the play library, the checker, the build, the print
                            documents. All the substance is here.
src/HuddleBoard.Build       the command line — this is what `make` used to be
src/HuddleBoard.Web         the website: Razor Pages at /, the app at /app/
tests/HuddleBoard.Tests     the verification suite, driving real Chromium
```

Nothing depends on Python. The app itself is still one hand-written HTML file
with no framework and no runtime dependencies — that has not changed and should
not. The website around it is ordinary ASP.NET Core, so APIs and a database can
follow; the commented `MapGroup("/api")` in `Program.cs` is where. Nothing is
added there that has no caller.

### The app lives at `/app/`, on the same host

The site's pages own the root; the built app is mounted at `/app/`
(`Program.cs`). It is a path and never a subdomain because everything a coach
keeps is `localStorage`, which is keyed by origin: same host, same storage, and
a subdomain would start every tablet empty. The app did not change for this —
every path it uses is relative, so its worker scopes itself to `/app/` and the
installed icon launches `/app/`.

`/app` **redirects to `/app/`** (301), and that is load-bearing: without the
slash `./sw.js` resolves to `/sw.js` and nothing installs. The root `/sw.js` is
a **kill-switch** (`wwwroot/sw.js`), for any browser that ran the app when it
was served at `/`: it unregisters itself and deletes only the cache holding the
root's `/index.html`, because cache names are per origin and the `/app/`
worker's `huddle-*` caches must survive it. Nothing registers it; it only ever
arrives as an update.

`dist/deploy/` and `HuddleBoard-deploy.zip` are unchanged: still a complete
static site for anyone hosting the app alone. Publish stages the app into
`wwwroot/app/` and leaves the static-host config (`web.config`, `_headers`, …)
behind, because this host applies those rules itself. `PwaChecks` runs once at
the root and once under `/app` (`StaticSite`'s `mount`), so both stay true.

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
`PlayLibraryChecks.TheVocabularyHasNotGrown` pins that to exactly 6-on-6 play
11, and holds the 5-on-5 and 7-on-7 libraries to no exceptions at all.

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

A library row's **thumbnail** opens the play large (`openPreview`): the same
picture, with the play's chips, names and tagline, sized by `fitPreview` to
whatever height the words leave so the whole card is on screen at every size.
Only the thumbnail does this — the rest of the row still adds and removes the
play — and the preview has nothing that touches the deck. `LibraryChecks`
holds it at the five sizes.

Setup is filed under four tabs (`SET_TABS`): **Game** (players on the field,
play names), **Clock**, **Your own** (names, weeks, drawn plays) and **Tablet**
(sync, the log, Start over). Only the open tab's rows are on the page, so its
handlers wire whatever is there; which tab is open lives in memory (`setTab`)
like the deck filter, and an import's answer opens on Tablet, the row that
asked. `MenuChecks` walks every tab at every size; a check reaching for a
Setup button opens its tab first (`AppFixture.SetupTabAsync`).

The Tablet tab's last row, **Start over**, puts the whole tablet back: the starting deck,
every shipped name, every setting, and which sideline is ours. It deliberately
leaves the game log alone. A log is a recording rather than a preference — the
one thing on the tablet that cannot be derived again — and it has its own Clear
one row up. Everything destructive in Setup arms on the first tap and acts on the
second (`confirmTap`), and the arming lapses on its own, so a stray thumb on the
way past costs nothing. The second tap lands on the **intro**, because a tablet
put back the way it came starts the way it came: its START asks how many are on
the field (the kept answer lit) and runs the tutorial. `ResetChecks` holds all
of that, including the log.

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
filter is on, and going back through the intro clears it. `DeckFilterChecks`
holds all of that.

The deck has no title row. It is the screen a coach picks plays from, so every
line of chrome above the tiles is a row of plays he cannot see. The chips sit on
the left of one top bar and the clock, the count and the menu on its right.
Because they cost no row of their own, they show for any deck with something to
narrow by, and stay away only when every play in the deck would answer both
questions the same way. (They used to hide below six plays, when the bar was a
row of its own and the whole deck was already on screen; with the bar there
anyway, an empty left half is just wasted space.)
**Change plays** is in the hamburger with Setup and Exit, because changing the
deck is something a coach does between series, not with six kids waiting.
`MenuChecks` holds the three items and that the menu is the last thing in the
bar.

**8. An update never interrupts a live play.** The service worker installs a new
build as a *waiting* worker and shows "Update ready". It swaps only on tap. The
offer appears on the deck and on the intro — the intro because it is where a
coach lands opening the app before a game, the one moment an update costs
nothing — and on no other screen, because the others are the ones turned
around at the kids. `PwaChecks` walks the offer through both screens.

**9. The ball is wherever the kid holding it is.** A play has one ball target,
the kid who ends up with it, and for 23 plays that was the whole story: the
football rode that kid's path from the snap. On REVERSE it put the ball in the
second runner's hands at the line before the first handoff had happened, while
the first runner ran his two steps empty-handed — the opposite of what the
coach was teaching. Now every handoff names who takes it, the exporter turns
the chain into legs, and the marker follows the first runner until he reaches
the end of the second arrow, then the second. `BallChecks` reads a frame either
side of that exchange on all three reverses.

A pass shows its flight the same way a handoff does. The handoff arrow is a
dashed grey line the ball rides along, and it stays on screen after the
exchange; a throw used to be the ball alone on an invisible arc, which six
routes could swallow. Now the arc is drawn as the same dashed grey line
(`SC.throw`), growing under the ball as it flies and left in place after the
catch, so there is one picture for "where the ball goes" whether it is handed
or thrown. The deck and library thumbnails draw the same arc from the same
three points (`throwArc`), so a tile is a small picture of what the big screen
will show. `BallChecks` holds that a pass has the line ending at the catch and
a run has none.

A pass does not have to leave the THROWER's hands. JET PASS is Jet Sweep for
two steps, then the sweep man stops short of the line and throws it deep —
the halfback pass, the one piece of misdirection the library had no picture
for, because a play's only thrower was the kid under centre and on a pass
every handoff was a fake. Now `ProtoExporter.Throwers` names the kid who
throws it when it is not the THROWER, the exporter follows the handoff chain
to him exactly as it does to a carrier, and refuses the play if his run ends
past the line, because a forward pass from there is a flag. The ball rides
his legs until his run ends, then flies; `throwArc` starts the arc from the
end of his run, so the tiles show the same thing. Play 11's handoff is still
a fake — nothing changes for a pass that names no thrower. `BallChecks` reads
a frame mid-sweep and one after the catch on JET PASS.

A rollout is the same picture with no handoff: a pass where the THROWER has
a `run` of his own exports one leg on him (`ProtoExporter.Rollout`), so the
ball rides his roll and is thrown from where it ends, under the same
behind-the-line rule. TRIPLE OUT used to throw from the spot he had already
left, his roll line drawing out empty-handed beside the ball. For the same
reason NO-RUN ZONE forbids a handoff or a `run` that crosses the line, not
any `run` at all: a thrower rolling out behind it is passing, which is
exactly what the zone is for. `BallChecks` reads TRIPLE OUT mid-roll. The reverse family is one concept
run from three formations — REVERSE (SPREAD), JET REVERSE (TRIPS LEFT) and
PITCH REVERSE (ACE) — and each starts as the exact sweep, jet or pitch the
defense has already chased, because a reverse is the punishment for chasing.

A play opens on its **finished picture** — the last frame of the run, held:
every route drawn and the ball where it ends up, because that is what a coach
turns around to show. **Line up** goes back to the spots; **Run it** and
**Replay** play it once from the snap and stop on that frame. The play screen
never loops (`frame()` clamps at `tEnd`); the tutorial's running slide and the
designer's preview still do, because nobody is standing there to press Replay.
`BallChecks` holds the opening frame and that a run does not come round again.

**10. Tablets sync by file, and the file is not trusted.** Assistant coaches
carry their own tablets and the sideline has no network, so there is no server
to sync through and never will be. Setup's **Sync tablets** row exports one
small JSON file — the deck, every custom name, every setting, which sideline is
ours — and imports it on the other tablet. Export goes to the share sheet
first, because Quick Share reaches the tablet next to you over Bluetooth with
the radio otherwise off, and falls back to a download; a coach closing the
share sheet is a cancel, not a reason to download instead. Import **replaces**
rather than merges: the goal is two tablets that agree, and a merge would be a
third deck neither coach chose. The game log is left alone for the same reason
Start over leaves it — it is this tablet's recording, not a preference.

The file is the second string in the app somebody else wrote, so `readSetup`
believes none of it: play ids are checked against the library, settings against
`CFG_CHOICES` (which also draws Setup's buttons, so the two cannot drift), the
sideline must be a colour, and names go through `setNames` exactly as typed
ones do, then `esc()` on the way out. Whatever fails is counted and reported on
the row rather than silently dropped. The settings that do land are applied
over the shipped defaults, never over what the tablet had before, so the result
depends on the file alone. `SyncChecks` holds all of it across two browser
contexts, which is what gives two tablets two separate stores.

**11. A coach starts from Week 1, and builds the rest.** The shipped play
packs (`PlayPacks.cs`, and each new format's own `Authoring.Weeks`) are six
week slots on a strip on the Change plays screen, per format. **Week 1** is a complete game deck — a run, a quick pass, a third
down and a goal line call, four plays — because a deck is what a coach
carries into one game, and four he can find without looking beats fourteen
he has to hunt through. **Weeks 2 to 6 ship empty.** They used to be five
more curated game decks covering the whole library. That was more of somebody
else's season plan than a coach wanted to start from, so they are slots for
his own decks. `PlayLibraryChecks` pins both halves: Week 1 is a whole game,
and every other week ships empty. The first pack *is* the starting deck:
`ProtoExporter` reads `defaultDeck` from it, so Start over and Week 1 cannot
disagree, and `PlayPacks.Check` fails the deploy gate if it is ever empty.

**The deck can be empty.** A coach who clears it to build his own keeps an
empty deck through a reload and a sync. It is not refilled with Week 1; only
Start over does that. An empty deck screen says what the play library is and
offers one green button to it (`#dlib`, the intro's own `.start`). An empty
week is a slot, not a deck. It is never lit as "the deck you have", even when
the deck is empty too. Tapping it says how to fill it rather than emptying
the deck, and Save deck is disabled while there is nothing to save. A sync
file spells empty weeks out as `[]`, and `readSetup` does not count those as
unrecognised.

Taking a pack replaces the deck, the same way a sync import does and for the
same reason — the point is a deck somebody chose, not a merge nobody did — so
it arms on the first tap (`confirmTap`, the same two-tap rule as everything
destructive in Setup) and lapses on its own. The strip is not a filter: it
changes what is *in* the deck, not what is on screen, so it wears its own
`.pack` chip with a dashed edge rather than `.fchip`, and lights solid when the
deck is exactly that pack, with the pack's blurb in the note below the rows.
A pack names plays by number and nothing else; the tablet resolves them
against the library it has and offers no pack it cannot fill, because a deck
with holes in it is worse than no pack. `PlayPacks.Check` runs inside `check`
(not inside `PlayChecker.Check`, which is also run on the original fourteen
alone) so a pack naming a play that does not exist fails the deploy gate.
`PackChecks` holds the two taps, the exact replacement, the lapse, the hidden
strip, and the strip's fit across the five sizes.

A week is also a slot a coach can save his own deck into. **Save deck** sits at
the far end of the strip; tapping it turns the strip into the question "save
into which week?", the weeks go amber, and the next tap on a week puts the
current deck in that slot. It is the same two-tap shape as taking a pack, and
the strip says which question it is asking so the second tap cannot be
mistaken for the other one; the mode lapses on its own. At rest the strip has
**no label**: it used to open with "Start from", and that was a heading over a
row of chips that already read as weeks, costing width on the one row that has
to fit six of them plus two verbs. A label appears only while a verb is armed,
because then it is the question being asked, not a caption. Saved weeks live in
`localStorage` under `hb.packs`, keyed by pack id, with the same discipline as
names: only a slot that differs from the shipped pack is stored, so saving the
shipped deck back into its own week is not a save, and the pencil on the chip
cannot claim otherwise. Setup shows how many weeks are saved and resets them;
Start over resets them too. The sync file spells out **every** week as the
exporting tablet has it, shipped or saved, and import starts from the shipped
weeks and applies the file's — so a week the importing tablet had saved on its
own goes back to shipped when the file does not carry it, and the result
depends on the file alone. `readSetup` trusts none of it: a slot must be one
the build knows, its plays must exist in the library, once each. `SyncChecks`
carries a saved week across two tablets, and `PackChecks` holds the save, the
take-back, the not-a-save, the lapse and the reset.

A week's name is the coach's too. "Week 3" is the build's word for a deck the
team calls PLAYOFFS or HAWKS, so **Rename** sits beside Save deck and works
the same way: tap it, the strip asks "rename which?", tap the week, and a
one-box sheet opens — the same sheet shape as a play's, and the second place
in the app a coach types, so it goes through `tidyName` and `esc()` like the
first. Names live under `hb.packnames`, keyed by pack id, with the same rule
as play names: only a name that differs from the shipped one is stored, so
retyping "Week 3" is not a rename and emptying the box is the reset. A name
and a saved deck are independent — renaming does not touch the slot's plays
and saving does not touch its name — but they are one thing to Setup, which
counts both on the **Your own weeks** row and resets both with one button;
Start over does the same. The sync file carries `packNames` the way it carries
`names`, and `readSetup` accepts only a slot the build knows holding a string.
`PackChecks` holds the rename, the not-a-rename, the sheet's Reset, the lapse,
one verb cancelling the other, and Start over; `SyncChecks` carries a name
across two tablets and feeds it markup.

**12. The first START goes through the tutorial, once.** The app grew past
what one sentence on the intro could carry — chips, mirror, tap-a-kid, packs,
Setup, the menu — so a fresh tablet's first START lands on six slides
(`renderTour`) on its way to the deck, after one question:
**how many players does your team put on the field?** (`renderAskPlayers`).
It comes first because every screen after it, the tutorial's first tile
included, belongs to the format the answer picks; three big answers, each with
its format's base formation drawn, so the count is something a coach sees. It
is asked whenever the tutorial is about to run for the first time — a fresh
tablet, or one after Start over, with the kept answer lit — and never from the
menu's Tutorial, because that coach has answered. The slides themselves are: the coach's own first tile, that play
running, the marker shapes, a week of the pack strip, a slice of Setup, and
the menu, each beside a few sentences. Every picture is the real thing drawn
by the real code (`tourTile`, `tourField`, `tourMarkers`, `tourPacks`,
`tourSettings`, `tourMenu`), so a slide cannot drift from the screen it
describes — down to the second slide, which does not draw a picture of the
play running so much as run it: `startTourField` hands a real play to the
same `buildTimeline`/`buildScene` the play screen uses and lets the same
`frame()` loop animate it, gated on a `tourFieldOn` flag so the loop knows to
stop the moment the slide changes. The tile on the first slide is also
larger than the deck grid ever shows one, because this is the one screen
where a single play gets the whole box to itself. It is the coach's screen
and never the one turned around at the kids, so text is fine there. The title
row says TUTORIAL in the deck's own heading size, with "1 of 6" and the way
out — **Skip tutorial**, or **Close** from the menu — in the top right where
Settings keeps its Done; a small "How it works" up there was not enough for a
coach to tell he was in a walkthrough rather than the app, or that he was
allowed to leave it. Skip and the last slide's **Pick plays** both mark it seen, and
both land on the **play library**, not the deck: a new coach's deck is the
shipped Week 1 until he has chosen, and the library is where he chooses (its
Done goes on to the deck). From the menu, Close and Done go back to the deck.
The flag is kept under
`localStorage` `hb.tour` — a coach who skipped has said he does not want it,
and **Tutorial** in the hamburger brings it back whenever he does. The flag is
per tablet and not in the sync file: the assistant's tablet is still his first
time. Start over clears it, because that tablet is somebody's first time
again. The suite's fixture writes the flag before the app boots, so every
other check still lands on the deck; `TutorialChecks` opens the one tablet
that has not seen it, and holds that NEXT is on screen on every slide at every
size — the slides are the one screen with no other way off — and that once
means once.

**13. A coach can draw his own play, and the tablet checks it.** **New play**
on the Change plays screen opens the designer: pick one of the four
formations, then MOVE anyone anywhere legal (trick plays live outside the
formations), draw each kid's ROUTE by tapping points, and give someone the
BALL as a pass or a run. It is a coach's screen at a table before practice,
never the sideline, so it can ask for precision the deck never could. The
field is the play screen's own `buildScene()`/`frame()` with the drawing laid
over it, and **Preview** runs the real animation. The designer's field runs
to `DZ_BOT`, two yards deeper than the play screen's `FIELD_BOT`, and draws
its markers and names smaller, because two backs and a pitch behind the
THROWER did not fit; a kid or a point still stops at `FIELD_BOT`, the deepest
the play screen shows whole. **Start over**, beside Save,
puts the drawing back on the formation it began from: every kid on his spot,
no routes, no ball. It keeps the names and, on a saved play, the id, so Save
still overwrites that play. It takes two taps and is disabled while there is
nothing to undo. A route is drawn one leg
at a time, on purpose: tapping a kid only picks him. Tapping inside the
dotted ring (round the kid, then on the end of his route) starts a leg, the
next tap is where it ends, and he is back to just picked with the ring on the
new end. It used to keep drawing after every leg, so picking another player
dragged a leg across the field after him. Dragging from the ring pulls a first
leg out of the kid, or moves the route's end. While a leg is being drawn, a tap
lands on a marker only if it is right on it (`HIT_ON`); picking uses the wider
`HIT_PICK`. The ring's drawn radius is its hit area (`RING_KID`, `RING_END`),
because an invisible hit circle wider than anything on screen once made the
first point a hunt for its edge.

The checker is ported to JavaScript (`checkPlay`) because a drawn play has to
pass the same gate as a shipped one and the tablet has no C#. It reads the
tablet's play format, so it runs unchanged on shipped and drawn plays, and
`DesignerChecks` holds that it finds exactly what `PlayChecker` finds on the
whole shipped library — two copies of one rulebook are only safe while
something proves they agree. **Change a rule in `PlayChecker.cs` and you must
make the same change in `checkPlay`**, or that check fails. Save stays shut
while there is an error; warnings are shown and allowed. A coach never draws
a handoff: `meetPoint` puts the exchange where the thrower and the carrier
actually meet at 6 yd/s, so it passes the timing rule by construction. On a
pass the thrower can be drawn a **rollout**: a `run` line, the same picture
TRIPLE OUT uses, which the ball rides as one leg before it flies from the
line's end. While the play is a pass, his points are held behind the line
(`clampFor`), because a forward pass from past it is a flag. On a keeper he
is a carrier like anyone else; on a handoff to somebody else his line is the
exchange and is not his to draw.

A **reverse** is a chain of carriers: Ball, Run, the first carrier, then
**Hand off** and the next one, up to three (`VIA_CAP`). The design stores them
as `ball.via` (everyone before the last) plus `ball.who`, and `saneDesign`
holds that nobody appears twice and that the THROWER can only start a chain,
as a keeper that pitches it. He can never take it back. The second exchange is
placed by `meetRunning`. It picks the first moment after the previous exchange
when the two runners are within a yard of each other at 6 yd/s, or failing that
the closest they come. Runners drawn so they never meet get an honest checker
error saying how far apart they are. The call strip names the carriers in the
order they have the ball, as play 26's does. `DesignerChecks` rebuilds PITCH
REVERSE from scratch and reads the ball either side of the exchange.

A pass can be thrown by a **back**: JET PASS, drawn. With a target picked,
**Hand off** or **Pitch** (both "he throws it") and a tap on a kid makes him
the passer — stored as `ball.via` on a pass, one kid, never the THROWER and
never the target, plus `ball.pitch` for a toss. He runs his line, stops, and
throws from where it ends; `clampFor` holds his line behind the line exactly
as it holds a rollout's, and the compiled play names him in `ball.thrower`
with one leg, which is all `buildTimeline` and `throwArc` already needed. A
handoff is placed by `meetPoint`. A pitch is placed by `pitchPoint`: a toss
of at least `PITCH_MIN` yards, out in front of the runner and **behind or
level with the THROWER**, because a pitch that goes forward is a forward pass
and the kid who catches it could not throw again. The compiled handoff
carries `pitch: true`, and `checkPlay` errors on one that goes forward
(`LEGAL/pitch`) — the one rule with no copy in `PlayChecker.cs`, because the
shipped format has no word for a pitch, so it cannot fire on the library and
parity holds. Tapping a new target keeps the passer; **Take back** gives the
throw back to the THROWER with the same target; switching to Run drops the
passer. Any exchange keeps the play out of NO-RUN ZONE. The flea flicker —
the ball coming back to the THROWER — is deliberately not drawable:
`saneDesign` still holds that he only ever starts a chain. `DesignerChecks`
draws both, reads the ball mid-run and after the catch, and holds that a
pitch to a kid who never comes back behind the THROWER will not save.

What is stored is the **drawing**, not the play — spots, route points, who
gets the ball, names — under `localStorage` `hb.plays`, keyed by `u_` ids that
cannot collide with `p_NN`. `compileDesign` turns it into the tablet's play on
every load, so the call strip and jobs are always the running build's words,
and a play from a sync file is numbers and names, never markup. Routes are
named in the nine shapes by `shapeOf` (OUT is toward the kid's own sideline);
a route that is none of them gets the checker's vocabulary warning, which is
rule 1 reaching the designer. A dragged kid **keeps his formation name** —
the huddle told him he is SLOT ORANGE — and his job starts with where to line
up ("LINE UP behind SLOT ORANGE, then OUT at 4"); the formation reads
"· SHIFTED". A play files under one of the five situations or **CUSTOM**, and
wears a "Yours" chip and a ★ in the library. A new play goes straight into
the deck. The pencil on a drawn play's row opens the designer, not the rename
sheet. A stored design the checker now rejects is kept and marked "Needs
fixing", never dropped; one this build cannot read at all is carried through
saves untouched.

Drawn plays survive an update and survive **Start over**, which puts back what
shipped and a drawn play is not that — Setup's **Your own plays** row clears
them, two taps, and clearing leaves no hole in the deck or a saved week. The
sync file carries them as drawings and import **replaces** them like
everything else (rule 10), so with any on the tablet Import arms on the first
tap and the row says how many it will replace. `readSetup` runs every play in
the file through `saneDesign` and then the checker, and counts what fails.
`DesignerChecks` holds the parity, drawing, moving, the handoff, the save
gate, the fit across the five sizes, the update, the sync and the storage.

**14. Three formats, one tool.** 5-on-5, 6-on-6 and 7-on-7 are each a
`Format` (`Format.cs`): the SNAPPER, the THROWER and three, four or five more,
with their own four formations, their own library and their own Week 1. They
are distinct on purpose — not 6-on-6 with a kid taken away or added — so a coach
picking a play is never looking at somebody else's division. **Setup's first
row, Players on the field**, picks one (`cfg.players`, default 6), and the whole
tablet reads that format: `DATA` is `ALL.formats[n]`, and `loadMode(n)` is both
the boot path and the switch, so a switch is exactly a fresh start in the other
format. The deck's top bar and the library header wear the format as a badge,
and the intro says how many kids.

A coach's own choices are kept **per format**: the deck, play names, saved
weeks, week names and drawn plays live under `keyIn(k, n)` — 6-on-6 keeps the
keys it always had (`hb.deck`), the others add `.5` or `.7` — so a tablet from
before there was a choice is untouched, and switching back finds everything as
it was left. Shared across formats: settings, our sideline, the tutorial flag
and the log.

**Formation names are unique across every format**, because the tablet and a
stored design find a formation by name alone. `FORMATIONS` is the union of all
formats', `FORMATION_PLAYERS` says whose each is, the designer offers only the
current format's (`formationsHere`), and `saneDesign` refuses a design whose
formation belongs to another format. `PlayChecker.Run` fails `check` if two
formats share a formation name. Play ids are prefixed per format — `f_` (5),
`p_` (6), `s_` (7) — so no two formats ever name the same play. `QB` and `C` are
the THROWER's and SNAPPER's keys in all three; 7-on-7's fifth non-ball spot is
`F`, which is never shown — the kid still hears his spot's name.

Start over puts **every** format back but keeps `players`, because the division
a tablet plays in is the league, not a customisation. The sync file is **v2**:
settings, our side, and a `modes` section per format, each read against that
format's library alone (a 5-on-5 id in the 7-on-7 section is not recognised).
Import replaces every format; one the file does not carry goes back to shipped.
A v1 file is from before formats and is read as the 6-on-6 section. Print emits
a playbook, cards and rotation sheet per format, named `-5v5`, `-6v6`, `-7v7`.
`FormatChecks` holds the switch and the isolation.

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
  PlaysMore.cs                  plays 15-27
  Spots.cs                      spot names, the 9 shapes, the default rule
  PlayPacks.cs                  the saved decks a coach starts from, by week
  Format.cs                     one division's library; Formats.Five/Six/Seven
  Authoring.cs                  one row per play, for the formats after the first
  FiveOnFive.cs, SevenOnSeven.cs  those two formats, whole: formations, spots,
                                plays, texts, kid names, ball, weeks, script
  PlayTexts.cs                  what the tablet says, plays 1-14
  PlayTextsMore.cs              the same for 15-27
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
src/HuddleBoard.Web/            the website: Razor Pages (Pages/, wwwroot/) and the
                                app mounted at /app/, with the rules that matter
tests/HuddleBoard.Tests/        the verification suite
```

### Coordinates

Yards. `x` = left/right of the snapper (negative = offense's left = BLUE),
`y` = downfield from the line of scrimmage (negative = backfield). The sideline
is at `|x| = 15.7`, the goal line at `y = 15`, in every format.

### Adding a play

To 5-on-5 or 7-on-7, append one `Authored` row to `FiveOnFive.cs` or
`SevenOnSeven.cs` — geometry, text, kid name, ball and thrower all live in the
row — and, if it belongs in the game script, a line in that format's script.
Then `check` until clean. The steps below are for 6-on-6, whose library is
still four parallel tables:

1. Append to `Recent` in `PlaysMore.cs` — geometry, tagline, notes, mistake.
2. Add matching text in `PlayTextsMore.cs` — the call strip and the per-spot
   instructions, in spot language. Call-strip labels use LEFT/RIGHT spot names
   (`SLOT LEFT`); `ProtoExporter.Recolor` turns them into BLUE/ORANGE for
   display. Getting this backwards means the label will not resolve and the
   export will throw.
3. Add `KidNames[num]` and `Ball[num]` in `ProtoExporter.cs`. `Ball` is the
   thrower's *first read*, or the kid who *ends up* with the ball — one rule, no
   judgement calls. Every `Handoff` segment names who takes it (`To:`); the
   exporter follows that chain from the thrower and refuses a play where it does
   not end at `Ball`. A reverse is two handoffs in a row, the second drawn from
   the first runner's line to the second runner's. On a pass play a handoff is a
   fake and the ball stays with the thrower — unless the play is in
   `Throwers`, which names the kid who throws it instead (JET PASS: the sweep
   man). Then the chain has to reach him, his run has to end behind the line,
   and `Ball` is *his* first read.
4. Only Week 1 ships with plays, so a new play goes in no week unless it is
   meant to be part of the deck a new tablet starts with.
5. `check` until clean, then `dotnet test`.

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

The handoff rule is time-aware in the same way. A handoff segment starts on
the giver's line and ends where the receiver is at the moment the giver gets
there, within 2 yards; the pitch is the long case at 1.4. Before this, the
second exchange of a reverse was two run lines passing within half a yard of
each other, which the checker could not see and the tablet could not animate.

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

**Match the verification to the change.** The full suite is about four minutes
and most changes do not need it. `check` runs in seconds and is always run —
it is what gates the deploy. A change to one screen's drawing or behaviour
gets the one class that covers it:

```
dotnet test --filter "FullyQualifiedName~BallChecks"
```

The whole suite is for what it was built to catch: layout across the five
sizes, storage, sync and the service worker. Text, copy and colours need
`check` and nothing more.

The app opens on an intro screen — art and one START button — so
`AppFixture.OpenAppAsync` taps through it and hands every other check the deck.
Pass `intro: true` to stay on it; `IntroChecks` is the only thing that does.

`RenameChecks`, `ResetChecks`, `SyncChecks`, `FormatChecks` and `PwaChecks` run against the hosted
build over a local `StaticSite` rather than the standalone file, because all of
them are about storage and service workers and a `file://` origin has neither.
`SyncChecks` opens each tablet in its own browser context: headless Chromium has
no share sheet, so Export falls through to a download that the check captures
and feeds to the other tablet's hidden file input.

`LabelChecks` sweeps all 75 plays of the three formats × 2 mirror states × 2
stages × 5 viewports = 1,500 states. Several checks pad the library out to 100
plays (`AppFixture.InjectPlaysAsync`, ids `x_` so they never collide with a
format's) to judge the UI at a size it has not reached yet. `OpenAppAsync`'s
`players:` opens a tablet already set to 5 or 7 on the field.

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
  permanently out of reach. Setup is now tabbed and its rows start at the top,
  under the tab that chose them; if they are ever centred again, use auto
  margins on the first and last child, which collapse to zero once the group
  does not fit.
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
  `fitDeck()` adds the vertical pass: first the rows around the name drop to
  their smaller sizes (`.tight`, the same sizes a 3- or 4-column grid always
  uses), then the tagline goes because it is the only optional line, then the
  name shrinks for the whole deck at once, because one size across the grid is
  what makes it scannable. `fitNames()` tries one line before two: a name that
  fits on one line at `ONE_LINE` (72%) of its full size shrinks to do so, and
  only a name that cannot wraps.
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

The live site is the published **HuddleBoard.Web**: the pages at `/` and the
app at `/app/` (see "The app lives at `/app/`" above). From Visual Studio,
right-click **HuddleBoard.Web** → **Publish** → Azure App Service. Publishing
runs the build first and stages `dist/deploy/` into `wwwroot/app/`, and the host
applies the response rules itself.

`dist/deploy/` on its own is still a static site — no server code, no build
step — for anyone hosting just the app. Drop it at the root of any static host;
the config file for each is included. Only two things actually matter: serve
`.webmanifest` as `application/manifest+json`, and send `Cache-Control:
no-cache` for `index.html` and `sw.js`. Miss the second and tablets never see a
new build.

### The pipeline

`.github/workflows/deploy.yml` deploys every push to `master` to the
`huddleboard` App Service (`rg-default`, westus2, Windows). It runs `check`,
then `dotnet publish src/HuddleBoard.Web` in the same step that exports
`HB_VERSION` (publish runs `build`, so it inherits the version), and deploys the
publish folder with `clean: true` — the old static app sat at the root of
`wwwroot`, and a stale `/index.html` there would still be served. Nothing in
`dist/` is committed, so CI builds it from source.

**Keep the package small.** The first publish was 157 MB zipped and the deploy
failed with "not enough space on the disk": a portable publish carries
SkiaSharp's native binaries for every platform, and its native symbols are
86 MB each. CI publishes for one runtime only, and the Web project drops
packages' `.pdb` files (`DropPackageSymbols`), which brings it to about 13 MB.
The site never draws with Skia — only `build` does, and a published site does
not run it.

**That runtime is `win-x86`, and it has to match the worker.** The App Service
runs a 32-bit worker process (`use32BitWorkerProcess`; the Free plan allows
nothing else). A RID-specific publish compiles the site's own assembly for that
platform, so a `win-x64` publish is x64-only and a 32-bit worker cannot load it:
the deploy reports success and every request is *500.30 — ASP.NET Core app
failed to start*. Moving to a 64-bit worker means changing the app's Platform
setting and the workflow's `-r` together. To reproduce locally, run the publish
with the 32-bit `dotnet` under `C:\Program Files (x86)\dotnet`.

The App Service runs the **.NET 10 stack on Windows with Always On**. A static
site never cold-started; an ASP.NET one does after idling, and the first open
before a game should not be the slow one. Both are one-time settings on the app,
not in the workflow:

```
az webapp config set -g rg-default -n huddleboard --net-framework-version v10.0 --always-on true
```

If the plan ever stops offering .NET 10, publish self-contained instead
(`--self-contained true`) — larger, so check the package against the disk.

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

## Licence

MIT, and deliberately plain MIT — copy it, fork it, sell it with your changes,
no attribution beyond keeping the notice with the copy. An extra clause bolted
on would be a different licence wearing the MIT name, which is worse than
choosing a different licence outright, so `LicenceChecks` asserts the text is
unmodified and that no non-commercial, share-alike, patent or trademark wording
has crept in.

The licence travels with the thing being licensed. Neither shipping form carries
the repository with it — a coach hands another coach one HTML file, or a zip
lands on somebody's static host — so `AppBuilder` inlines the notice into both
pages after the doctype (never before it: content ahead of the doctype is the
classic route into quirks mode) and writes `LICENSE.txt` into `dist/deploy/`.
`art/intro-art.png` ships under the same terms as the code.

## Out of scope (decided, not forgotten)

- **Sharing a drawn play between formats.** A 6-on-6 drawing is not a 5-on-5
  one with a kid missing; there is no conversion, by design.
- **Coach-authored plays in the shipped library.** The designer (rule 13)
  draws plays that live on one tablet and its sync partners; the library
  itself still grows by editing `PlaysMore.cs`. Not yet in the designer, and
  the obvious next steps: copy-and-edit of a shipped play, pre-snap motion
  (REVERSE and JET REVERSE start with it), a reverse that ends in a throw
  (the passer takes the ball from the THROWER and nobody else), a pitch on a
  run, the flea flicker (decided against for now), a rollout that throws on the run
  (partway along, rather than from the end), editable per-kid instructions, and
  printing or graduating a drawn play into the library.
- **Rewriting the app in Blazor.** The single HTML file is the product: it opens
  from a file, on a tablet, with no network and no runtime. A WASM runtime works
  against that, and every gotcha above would have to be re-solved.
- **The flip/orientation toggle.** Removed deliberately — all coaches teach the
  same way instead.
- **Parent/assistant logging apps.** The event log already records everything
  with millisecond timestamps and monotonic-vs-wall drift detection, so a v2
  reconciliation is possible without refactoring. Not built.
