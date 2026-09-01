# -*- coding: utf-8 -*-
"""Run every check and summarise. Exit code is non-zero if any of them failed.

A check "fails" if it prints the word FAIL, raises, or reports a JS error.
That is a blunt rule, but it means a new check only has to print like the
others to be picked up here.
"""
import pathlib
import subprocess
import sys
import time

HERE = pathlib.Path(__file__).resolve().parent
ROOT = HERE.parent

CHECKS = [
    ("geometry   ", ROOT / "check_plays.py", "every play is legal, safe and in the vocabulary"),
    ("names      ", HERE / "verify_names.py", "no clipped play names, every tile reachable"),
    ("library    ", HERE / "verify_lib.py", "filters, search and the empty state"),
    ("library fit", HERE / "verify_libclip.py", "library names at 14 and 100 plays"),
    ("labels     ", HERE / "verify_labels.py", "spot names stay on the field, no letter tags"),
    ("sidelines  ", HERE / "verify_sides.py", "OUR/THEIR SIDE centred, hatch reaches the edge"),
    ("defaults   ", HERE / "verify_default.py", "a fresh tablet shows real play names"),
    ("offline    ", HERE / "verify_local.py", "the standalone file works with no network"),
    ("fullscreen ", HERE / "verify_fs.py", "the full-screen button appears where it should"),
    ("pwa        ", HERE / "verify_pwa.py", "install, offline cache, update-on-tap"),
]


def main():
    bad = []
    for name, script, blurb in CHECKS:
        t0 = time.time()
        r = subprocess.run([sys.executable, str(script)], cwd=ROOT,
                           capture_output=True, text=True)
        out = r.stdout + r.stderr
        failed = (r.returncode != 0 or "FAIL" in out
                  or "Traceback" in out
                  or ("JS errors" in out and "JS errors                 : none" not in out))
        print("%s %-6s %5.1fs  %s" % (name, "FAIL" if failed else "ok", time.time() - t0, blurb))
        if failed:
            bad.append((name, out))

    print()
    if not bad:
        print("all %d checks passed" % len(CHECKS))
        return 0
    for name, out in bad:
        print("=" * 70)
        print(name.strip())
        print(out.strip()[-2500:])
    print("\n%d of %d checks FAILED" % (len(bad), len(CHECKS)))
    return 1


if __name__ == "__main__":
    sys.exit(main())
