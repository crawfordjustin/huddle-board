# -*- coding: utf-8 -*-
"""Shared plumbing for the verification suite.

Every check drives a real Chromium against a real build. There are no unit
tests here on purpose: almost every bug this project has actually had was a
layout or timing bug that only shows up once the thing is on screen at a
particular size.
"""
import contextlib
import functools
import http.server
import os
import pathlib
import socketserver
import subprocess
import sys
import threading

ROOT = pathlib.Path(__file__).resolve().parent.parent
DIST = ROOT / "dist"
sys.path.insert(0, str(ROOT))          # so tests can import plays / spots

# the five tablet shapes worth caring about: two landscape, two portrait, and
# the short landscape that catches anything relying on vertical room
SIZES = [
    ("landscape 16:10", 1600, 1000),
    ("landscape 4:3", 1280, 960),
    ("portrait 10in", 1200, 1920),
    ("portrait small", 800, 1280),
    ("landscape small", 1024, 600),
]


def app_uri():
    """file:// URI of the standalone build."""
    p = DIST / "HuddleBoard.html"
    if not p.exists():
        raise SystemExit("no build at %s — run `make build` first" % p)
    return p.as_uri()


def build(version=None):
    """Rebuild dist/. Pass a version to fake a new deploy."""
    env = dict(os.environ)
    if version:
        env["HB_VERSION"] = version
    return subprocess.run([sys.executable, "build_app.py"], cwd=ROOT,
                          env=env, capture_output=True, text=True)


@contextlib.contextmanager
def serve(directory=None, port=0):
    """Serve a directory over http so service-worker checks can run.

    Port 0 lets the OS pick, which is what stops two checks running back to
    back from fighting over a fixed port.
    """
    directory = str(directory or (DIST / "deploy"))

    class Quiet(http.server.SimpleHTTPRequestHandler):
        def log_message(self, *a):        # a request log per asset drowns the results
            pass

    handler = functools.partial(Quiet, directory=directory)
    with socketserver.TCPServer(("127.0.0.1", port), handler) as httpd:
        httpd.allow_reuse_address = True
        t = threading.Thread(target=httpd.serve_forever, daemon=True)
        t.start()
        try:
            yield "http://127.0.0.1:%d" % httpd.server_address[1]
        finally:
            httpd.shutdown()


# Clone the real plays up to N so the UI can be judged at a size the library
# will not reach for a while. Returns the size of the data in bytes.
INJECT_PLAYS = """(n) => {
  const base = DATA.plays.slice();
  const out = [];
  for (let i = 0; i < n; i++){
    const b = JSON.parse(JSON.stringify(base[i % base.length]));
    b.id = "s_" + i; b.num = i + 1;
    const suffix = " " + (Math.floor(i / base.length) + 1);
    b.coachName += suffix; b.kidName += suffix;
    out.push(b);
  }
  DATA.plays = out;
  return JSON.stringify(DATA).length;
}"""


def open_app(browser, w, h, plays=None):
    """New page with the app loaded, optionally padded out to `plays` plays."""
    pg = browser.new_page(viewport={"width": w, "height": h})
    errs = []
    pg.on("pageerror", lambda e: errs.append(str(e)))
    pg.goto(app_uri())
    pg.wait_for_timeout(400)
    if plays:
        pg.evaluate(INJECT_PLAYS, plays)
    return pg, errs


def report(name, ok, detail=""):
    print("%-28s %s %s" % (name, "OK  " if ok else "FAIL", detail))
    return 0 if ok else 1
