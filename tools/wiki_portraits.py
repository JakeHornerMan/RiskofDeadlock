"""Downloads each hero's hero-select card from deadlock.wiki into build/pages/img/<id>_card.webp for the kit pages.

The creator asked for the wiki's portraits on the pages (2026-10-25: "just pull the ones fomr the wiki for their eneter
gaim portrait!"); the file is `<Display_Name>_card.png` on cdn.deadlock.wiki (280 x 380, the hero-pick screen portrait).
build/ is gitignored: the cards are never committed or shipped in a package.

Run: python tools/wiki_portraits.py [hero ids], then python tools/kit_pages.py."""
import io, json, os, sys, urllib.parse, urllib.request
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IMG = os.path.join(ROOT, 'build', 'pages', 'img')
os.makedirs(IMG, exist_ok=True)
HEROES = json.load(open(os.path.join(ROOT, 'design', 'heroes.json'), encoding='utf-8'))['rows']
want = set(sys.argv[1:])
failed = []
for h in HEROES:
    if want and h['id'] not in want: continue
    url = 'https://cdn.deadlock.wiki/' + urllib.parse.quote(h['displayName'].replace(' ', '_') + '_card.png')
    try:
        data = urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'}), timeout=30).read()
        im = Image.open(io.BytesIO(data)).convert('RGB')
        im.save(os.path.join(IMG, h['id'] + '_card.webp'), quality=90)
        print(f'{h["id"]}: {im.size[0]}x{im.size[1]} from {url}')
    except Exception as e:
        failed.append(h['id']); print(f'{h["id"]}: FAILED {url} ({e})')
sys.exit(1 if failed else 0)
