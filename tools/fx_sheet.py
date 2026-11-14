#!/usr/bin/env python3
"""Contact sheets of the fps bench's effect screenshots (FPS=1 SHOTS=<folder> tools/run_bench.sh, docs/VfxRebuild/README.md).

usage: python tools/fx_sheet.py <shots folder> <out folder> [hero ...] [--every 2] [--crop]
       python tools/fx_sheet.py --pair <before shots> <after shots> <out folder> [hero ...]

One sheet per hero and move: its frames in order (every Nth with --every), each cut to the middle of the screen with --crop
(where the hero and the pack are). --pair puts before on the top row and after on the bottom row, 6 frames each.
"""
import os
import re
import sys
from PIL import Image, ImageDraw

NAMES = {'primary': 'M1', 'secondary': 'M2', 'utility': 'Shift', 'special': 'R', 'equipment': 'Q'}


def frames(folder):
    out = {}
    for f in sorted(os.listdir(folder)):
        m = re.match(r'deadlock(\w+?)_(primary|secondary|utility|special|equipment)_(\d+)\.png$', f)
        if m:
            out.setdefault((m.group(1), m.group(2)), []).append(os.path.join(folder, f))
    return out


def tile(path, w, h, crop):
    im = Image.open(path).convert('RGB')
    if crop:
        W, H = im.size
        im = im.crop((int(W * 0.2), int(H * 0.12), int(W * 0.8), int(H * 0.88)))
    return im.resize((w, h), Image.LANCZOS)


def main():
    a = sys.argv[1:]
    crop = '--crop' in a
    every = 1
    if '--every' in a:
        every = int(a[a.index('--every') + 1])
        del a[a.index('--every'):a.index('--every') + 2]
    a = [x for x in a if x != '--crop']
    if a and a[0] == '--pair':
        before, after, out = a[1], a[2], a[3]
        heroes = a[4:]
        fb, fa = frames(before), frames(after)
        os.makedirs(out, exist_ok=True)
        for key in sorted(fa):
            if heroes and key[0] not in heroes or key not in fb:
                continue
            w, h = 320, 200
            sheet = Image.new('RGB', (w * 6, h * 2 + 24), (0, 0, 0))
            for row, lst in enumerate((fb[key], fa[key])):
                pick = [lst[i] for i in range(1, len(lst), max(1, len(lst) // 6))][:6]
                for i, p in enumerate(pick):
                    sheet.paste(tile(p, w, h, True), (i * w, 24 + row * h))
            d = ImageDraw.Draw(sheet)
            d.text((6, 6), f'{key[0]} {NAMES[key[1]]}: before (top), after (bottom)', fill=(255, 255, 0))
            sheet.save(os.path.join(out, f'{key[0]}_{key[1]}.jpg'), quality=88)
        return
    folder, out = a[0], a[1]
    heroes = a[2:]
    os.makedirs(out, exist_ok=True)
    for key, lst in sorted(frames(folder).items()):
        if heroes and key[0] not in heroes:
            continue
        lst = lst[::every]
        cols = 4
        w, h = 480, 300 if crop else 270
        rows = (len(lst) + cols - 1) // cols
        sheet = Image.new('RGB', (w * cols, h * rows), (0, 0, 0))
        for i, p in enumerate(lst):
            sheet.paste(tile(p, w, h, crop), ((i % cols) * w, (i // cols) * h))
        ImageDraw.Draw(sheet).text((6, 6), f'{key[0]} {NAMES[key[1]]}', fill=(255, 255, 0))
        sheet.save(os.path.join(out, f'{key[0]}_{key[1]}.jpg'), quality=88)


if __name__ == '__main__':
    main()
