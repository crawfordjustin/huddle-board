# Huddle Board

A sideline play tool for 8U flag football — 6-on-6, no blocking, rusher seven
yards back, run and no-run zones.

The coach picks a play on a tablet, turns the screen around, and the kids watch
it animate. Every job is one of nine shapes, sides are BLUE and ORANGE instead
of left and right, and the play tells each kid where he lines up and what he
does. Works offline.

<p align="center">
  <img src="docs/deck.png" width="49%" alt="The deck screen">
  <img src="docs/play.png" width="49%" alt="A play, lined up">
</p>

## Run it

Nothing to install for the coaches — `dist/HuddleBoard.html` is the entire app
in one file. Copy it to a tablet and open it.

For a team, host `dist/deploy/` (free Azure App Service works) so tablets get
updates and can install to the home screen. `dist/deploy/README.md` has the
per-host setup; the short version is serve `.webmanifest` with the right MIME
type and don't cache `index.html` or `sw.js`.

## Work on it

```
python -m venv .venv && . .venv/bin/activate      # Windows: .venv\Scripts\activate
pip install -r requirements.txt
playwright install chromium

make build      # -> dist/
make check      # fast play-library check
make test       # the full suite, drives a real browser
```

`make print` regenerates the paper fallbacks — playbook, field cards, rotation
sheet — as PDFs.

## What's in here

| | |
|---|---|
| `huddle_src.html` | the whole app: markup, styles, script |
| `plays.py`, `plays_more.py` | the 24 plays — route geometry in yards |
| `spots.py`, `spots_more.py` | what the tablet says to a seven-year-old |
| `check_plays.py` | legality, collision and vocabulary checker |
| `export_proto.py`, `build_app.py` | the build |
| `print/` | the paper playbook pipeline |
| `tests/` | verification — real browser, five tablet shapes |

**`CLAUDE.md` is the file to read before changing anything.** It has the design
rules that are load-bearing, the coordinate system, how to add a play, and the
list of mistakes already made so they don't get made again.

## The idea

Most youth playbooks are built for the coach. This one is built for the moment
a coach has forty seconds, six children, and one of them has never played
before. So: no left and right, no letters on the field, one default rule that
covers any kid who forgets his job, and a deck small enough to find a play
without looking.
