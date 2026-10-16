"""Cuts out the studio renders for the kit pages (dev only).

`PORTRAITS=1 bash tools/run_bench.sh 0` renders every hero, its RoR2 match and its ult figure twice into build/portraits
(<name>_black.png and <name>_white.png: same frame, black and white background). Here:
  alpha = 1 - (white - black)            (a pixel that shows the background changes by the full 0..1 range)
  colour = black / alpha                 (un-premultiply)
then crop to the subject with a small margin and save build/pages/img/<name>.webp (transparent), which
tools/kit_pages.py embeds into the pages. These are our own renders of the mod in game; no Deadlock art files are used.

usage: python tools/portraits.py [exposure]   (exposure multiplies the colour, default 1.0)
"""
import os, sys
from PIL import Image, ImageChops
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, 'build', 'portraits')
OUT = os.path.join(ROOT, 'build', 'pages', 'img')
EXPOSURE = float(sys.argv[1]) if len(sys.argv) > 1 else 1.0
os.makedirs(OUT, exist_ok=True)

names = sorted({f.rsplit('_', 1)[0] for f in os.listdir(SRC) if f.endswith('_black.png')})
for name in names:
    b = np.asarray(Image.open(os.path.join(SRC, name + '_black.png')).convert('RGB'), dtype=np.float32) / 255
    w = np.asarray(Image.open(os.path.join(SRC, name + '_white.png')).convert('RGB'), dtype=np.float32) / 255
    a = np.clip(1.0 - (w - b).mean(axis=2), 0, 1)
    a[a < 0.02] = 0
    rgb = np.where(a[..., None] > 0, b / np.maximum(a[..., None], 1e-3), 0) * EXPOSURE
    img = np.dstack([np.clip(rgb, 0, 1), a])
    im = Image.fromarray((img * 255).astype(np.uint8), 'RGBA')
    box = im.getchannel('A').point(lambda v: 255 if v > 8 else 0).getbbox()
    if not box:
        print(f'{name}: empty render, skipped'); continue
    m = int(0.04 * max(box[2] - box[0], box[3] - box[1]))
    box = (max(0, box[0] - m), max(0, box[1] - m), min(im.width, box[2] + m), min(im.height, box[3] + m))
    im = im.crop(box)
    if im.height > 900: im = im.resize((round(im.width * 900 / im.height), 900), Image.LANCZOS)
    path = os.path.join(OUT, name + '.webp')
    im.save(path, 'WEBP', quality=88, method=6)
    print(f'{name}: {im.width}x{im.height}, {os.path.getsize(path) // 1024} KB')
