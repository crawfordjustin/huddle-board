import pathlib as _pathlib
import sys as _sys
_sys.path.insert(0, str(_pathlib.Path(__file__).resolve().parent.parent))
_OUT = _pathlib.Path(__file__).resolve().parent.parent / "dist" / "print"
_OUT.mkdir(parents=True, exist_ok=True)

from playwright.sync_api import sync_playwright
import pathlib
p = pathlib.Path(str(_OUT / "playbook.html")).resolve().as_uri()
with sync_playwright() as pw:
    b = pw.chromium.launch()
    pg = b.new_page()
    pg.goto(p, wait_until="networkidle")
    pg.pdf(path=str(_OUT / "8U-Flag-Football-Playbook.pdf"), format="Letter", print_background=True,
           margin={"top":"0","bottom":"0","left":"0","right":"0"})
    b.close()
print("pdf ok")
