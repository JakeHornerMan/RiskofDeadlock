"""Installs a package's Thunderstore dependencies (and theirs) that are missing, the way r2modman lays them out: a package's
plugins/ files in BepInEx/plugins/<Owner-Name>/, its patchers/ in BepInEx/patchers/<Owner-Name>/ (creator, 2026-11-11: R2API for
the Troopers, "install it into that profile myself"; R2Wiki Cookbook: dependencies are listed, never bundled).

    python tools/thunderstore_deps.py <manifest.json> --profile Deadlock     into the r2modman profile (mods.yml entries too)
    python tools/thunderstore_deps.py <manifest.json> --steam                into the Steam copy (the test runs use it)

BepInExPack is never installed here (the profile and the Steam copy have their own). r2modman must be closed for --profile
(r2modman_install.py closes it first and calls this). Downloads only from thunderstore.io; nothing of ours is uploaded.
"""
import io, json, os, re, shutil, sys, time, urllib.request, zipfile

args = sys.argv[1:]
if not args or not args[0].endswith('.json'): print(__doc__); sys.exit(2)
manifest = json.load(open(args[0], encoding='utf-8-sig'))
STEAM = r'C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2'
if '--steam' in args:
    base, mods_yml = os.path.join(STEAM, 'BepInEx'), None
else:
    profile = args[args.index('--profile') + 1] if '--profile' in args else 'Deadlock'
    P = os.path.join(os.environ['APPDATA'], 'r2modmanPlus-local', 'RiskOfRain2', 'profiles', profile)
    base, mods_yml = os.path.join(P, 'BepInEx'), os.path.join(P, 'mods.yml')
SKIP = ('bbepis-BepInExPack',)

def have(name):
    if os.path.isdir(os.path.join(base, 'plugins', name)) or os.path.isdir(os.path.join(base, 'patchers', name)): return True
    return bool(mods_yml and re.search(r'(?m)^  name: ' + re.escape(name) + r'\s*$', open(mods_yml, encoding='utf-8').read()))

def get(url, timeout=60):
    # a plain client name: thunderstore.io refuses Python's default one (403)
    return urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'mod-tools/1.0'}), timeout=timeout).read()

def info(owner, name):
    return json.loads(get(f'https://thunderstore.io/api/experimental/package/{owner}/{name}/'))['latest']

done, entries = set(), []
def install(dep):
    parts = dep.split('-')
    owner, name = parts[0], '-'.join(parts[1:-1])
    full = f'{owner}-{name}'
    if full in done or full in SKIP: return
    done.add(full)
    meta = info(owner, name)
    # the newest version when it is newer than asked (r2modman does the same); its own dependencies first
    use = meta['version_number']
    for d in meta['dependencies']: install(d)
    if have(full): print(f'   {full}: already there'); return
    data = get(meta['download_url'], 120)
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        for f in z.infolist():
            if f.is_dir(): continue
            n = f.filename.replace('\\', '/')
            # some packages (HookGenPatcher) nest everything under BepInEx/
            if n.lower().startswith('bepinex/'): n = n[len('bepinex/'):]
            low = n.lower()
            if low.startswith('patchers/'): out = os.path.join(base, 'patchers', full, *n.split('/')[1:])
            elif low.startswith('plugins/'): out = os.path.join(base, 'plugins', full, *n.split('/')[1:])
            elif low.startswith(('config/', 'core/')): continue
            else: out = os.path.join(base, 'plugins', full, *n.split('/'))
            os.makedirs(os.path.dirname(out), exist_ok=True)
            with z.open(f) as s, open(out, 'wb') as d: shutil.copyfileobj(s, d)
    major, minor, patch = (int(x) for x in use.split('.'))
    entries.append(f"""- manifestVersion: 1
  name: {full}
  authorName: {owner}
  websiteUrl: ''
  displayName: {name}
  description: ''
  gameVersion: ''
  networkMode: ''
  packageType: ''
  installMode: ''
  installedAtTime: {int(time.time() * 1000)}
  loaders: []
  dependencies:{''.join(f'{chr(10)}    - {d}' for d in meta['dependencies']) or ' []'}
  incompatibilities: []
  optionalDependencies: []
  versionNumber:
    major: {major}
    minor: {minor}
    patch: {patch}
  enabled: true
  onlineSource: true
  trustedPackage: false
""")
    print(f'   {full} {use}: installed')

for d in manifest.get('dependencies', []): install(d)
if mods_yml and entries:
    t = open(mods_yml, encoding='utf-8').read()
    open(mods_yml, 'w', encoding='utf-8', newline='\n').write(t if t.endswith('\n') else t + '\n')
    open(mods_yml, 'a', encoding='utf-8', newline='\n').write(''.join(entries))
print(f'== Thunderstore dependencies: {len(entries)} installed into {base}')
