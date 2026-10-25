"""Draws the released heroes' heads in a grid (4 a row, transparent background) into plugin/Assets/heroes.png, embedded
in the DLL for the title screen's "Deadlock Hero Info" button (Runtime/CommunityButton.cs). From our own in-game renders in
build/pages/img (tools/portraits.py, PORTRAITS=1 run_bench.sh), never Deadlock's art files; the heroes from
thunderstore/release.json."""
import json, os
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CELL = 128          # each head's square
COLS = 4            # heads per row (two rows for eight heroes)
TOP = 0.30          # the part of each figure kept, from the top (the head)

ids = json.load(open(os.path.join(ROOT, 'thunderstore', 'release.json'), encoding='utf-8'))['heroes']
rows = (len(ids) + COLS - 1) // COLS
out = Image.new('RGBA', (COLS * CELL, rows * CELL))
for n, h in enumerate(ids):
    im = Image.open(os.path.join(ROOT, 'build', 'pages', 'img', h + '.webp')).convert('RGBA')
    im = im.crop(im.getbbox())
    top = im.crop((0, 0, im.width, round(im.height * TOP)))
    # a square around the top of the figure, centred on what is drawn there (the head, not the whole width)
    bx = top.getbbox()
    side = max(bx[3] - bx[1], 1)
    cx = (bx[0] + bx[2]) // 2
    sq = Image.new('RGBA', (side, side))
    sq.alpha_composite(top.crop((cx - side // 2, bx[1], cx - side // 2 + side, bx[1] + side)).convert('RGBA'), (0, 0))
    out.alpha_composite(sq.resize((CELL, CELL), Image.LANCZOS), ((n % COLS) * CELL, (n // COLS) * CELL))
path = os.path.join(ROOT, 'plugin', 'Assets', 'heroes.png')
out.save(path)
print(f'{path}: {out.width}x{out.height}, {len(ids)} heroes ({", ".join(ids)})')
