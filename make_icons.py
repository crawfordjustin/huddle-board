# -*- coding: utf-8 -*-
"""App icons: the field mark, drawn so it still reads at 48px."""
import pathlib

from PIL import Image, ImageDraw

DEPLOY = pathlib.Path(__file__).resolve().parent / "dist" / "deploy"

OOB, FIELD, BLUE, ORANGE, GREEN, BALL = ((30,37,33), (247,249,246), (29,111,208),
                                         (228,97,15), (18,122,77), (255,196,0))

def draw(size, pad_frac):
    S = size * 4                                    # supersample, then downscale
    im = Image.new("RGBA", (S, S), OOB + (255,))
    d = ImageDraw.Draw(im)
    pad = int(S * pad_frac)
    fw = S - pad * 2
    # playing surface
    fx0, fx1 = pad + int(fw * .17), S - pad - int(fw * .17)
    fy0, fy1 = pad, S - pad
    d.rectangle([fx0, fy0, fx1, fy1], fill=FIELD)
    # the two sidelines
    bw = max(3, int(fw * .055))
    d.rectangle([fx0, fy0, fx0 + bw, fy1], fill=BLUE)
    d.rectangle([fx1 - bw, fy0, fx1, fy1], fill=ORANGE)
    # line of scrimmage
    los = fy0 + int((fy1 - fy0) * .62)
    d.rectangle([fx0 + bw, los, fx1 - bw, los + max(2, int(S*.012))], fill=(174,184,176))
    # one route, breaking upfield
    cx = (fx0 + fx1) // 2
    lw = max(4, int(fw * .085))
    d.line([(cx - int(fw*.10), fy1 - int((fy1-fy0)*.16)), (cx - int(fw*.02), los),
            (cx + int(fw*.09), fy0 + int((fy1-fy0)*.14))], fill=GREEN, width=lw,
           joint="curve")
    # the ball
    r = int(fw * .085)
    bx, by = cx - int(fw*.10), fy1 - int((fy1-fy0)*.16)
    d.ellipse([bx-r, by-r, bx+r, by+r], fill=BALL, outline=OOB, width=max(2,int(S*.008)))
    return im.resize((size, size), Image.LANCZOS)

for name, size, pad in [("icon-192.png",192,.09), ("icon-512.png",512,.09),
                        ("icon-maskable-512.png",512,.20)]:
    DEPLOY.mkdir(parents=True, exist_ok=True)
    draw(size, pad).save(DEPLOY / name)
    print("wrote", DEPLOY / name)
