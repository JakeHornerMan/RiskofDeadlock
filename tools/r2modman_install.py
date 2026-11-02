"""Put the creator's own DLL build into their r2modman profile, in place of every older Risk of Deadlock package (creator,
2026-11-02: "can we automate this process where we delete the old package and import the new dll in r2modman").

    python tools/r2modman_install.py <zip> [--profile Deadlock] [--wait]

Does what r2modman's Settings > Profile > Import local mod does, after removing the old ones by hand:
- every package in the profile that holds DeadlockOfRain.dll (or its disabled .old copy) is removed, from BepInEx/plugins and
  from the profile's mods.yml: the own-files builds and the old Thunderstore Risk_of_Deadlock alike (the same plugin twice
  clashes; Thunderstore was given up on 2026-11-02);
- r2modman's download cache of those packages goes too (the newest import is kept there, as r2modman keeps it);
- the zip goes to BepInEx/plugins/Unknown-<name>/ with r2modman's mm_v2_manifest.json, and its entry is added to mods.yml.
r2modman keeps mods.yml in memory and writes it back on close, so the script closes r2modman first and starts it again at the end;
it stops only while the game is running (the creator may be playing). Never upload the zip: its DLL holds Deadlock's files.
"""
import json, os, re, shutil, subprocess, sys, time, zipfile

args = sys.argv[1:]
profile = 'Deadlock'
# --wait: if the game is running (the creator is playing), wait for it to close instead of stopping
wait = '--wait' in args
if wait: args.remove('--wait')
if '--profile' in args:
    i = args.index('--profile'); profile = args[i + 1]; del args[i:i + 2]
if len(args) != 1 or not args[0].endswith('.zip'):
    print(__doc__.split('\n\n')[1]); sys.exit(2)
zpath = args[0]
GAME = os.path.join(os.environ['APPDATA'], 'r2modmanPlus-local', 'RiskOfRain2')
P = os.path.join(GAME, 'profiles', profile)
plugins, mods_yml = os.path.join(P, 'BepInEx', 'plugins'), os.path.join(P, 'mods.yml')
if not os.path.isdir(plugins) or not os.path.exists(mods_yml):
    print(f'No r2modman profile "{profile}" at {P}'); sys.exit(1)


def running(name):
    out = subprocess.run(['powershell', '-NoProfile', '-Command',
                          f"Get-Process -Name '{name}' -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Id"],
                         capture_output=True, text=True).stdout.strip()
    return bool(out)


def ps(cmd):
    return subprocess.run(['powershell', '-NoProfile', '-Command', cmd], capture_output=True, text=True).stdout.strip()


# a running game means the creator is playing: never close it
if running('Risk of Rain 2'):
    if not wait:
        print('Risk of Rain 2 is running (you may be playing): close the game, then run this again (or pass --wait).'); sys.exit(1)
    print('== Risk of Rain 2 is running: installing as soon as it closes', flush=True)
    while running('Risk of Rain 2'): time.sleep(10)
    time.sleep(5)
# r2modman keeps mods.yml in memory and writes it back on close, so it is closed first (asked to close like its X button, so it
# saves; forced after 15 s) and started again at the end (creator, 2026-11-02: "add this into your process turning off modman
# and doing all this be independant")
r2exe = ps("Get-Process -Name r2modman -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Path")
if r2exe:
    print('== Closing r2modman (it is started again when the install is done)')
    ps("Get-Process -Name r2modman -ErrorAction SilentlyContinue | ForEach-Object { [void]$_.CloseMainWindow() }")
    for _ in range(30):
        if not running('r2modman'): break
        time.sleep(0.5)
    if running('r2modman'):
        ps("Stop-Process -Name r2modman -Force -ErrorAction SilentlyContinue"); time.sleep(1)

with zipfile.ZipFile(zpath) as z:
    manifest = json.loads(z.read('manifest.json').decode('utf-8-sig'))
name = manifest['name']
# the author (creator, 2026-11-02: "make the mod by JakeHornerMan"); an import without one is r2modman's Unknown
author = manifest.get('author') or 'Unknown'
full = author + '-' + name
major, minor, patch = (int(x) for x in manifest['version_number'].split('.'))

# 1. every package holding our plugin goes (folder and mods.yml entry)
gone = []
for d in sorted(os.listdir(plugins)):
    fd = os.path.join(plugins, d)
    if os.path.isdir(fd) and any(f in os.listdir(fd) for f in ('DeadlockOfRain.dll', 'DeadlockOfRain.dll.old')):
        shutil.rmtree(fd); gone.append(d)
text = open(mods_yml, encoding='utf-8').read()
head, *blocks = re.split(r'(?m)^(?=- manifestVersion:)', text)
keep = [b for b in blocks if not (re.search(r'(?m)^  name: (.+)$', b) and re.search(r'(?m)^  name: (.+)$', b).group(1).strip() in gone + [full])]

# 2. r2modman's cache: our older imports and the gone packages (the new one is put back below)
cache = os.path.join(GAME, 'cache')
old_imports = [d for d in (os.listdir(cache) if os.path.isdir(cache) else []) if (d.startswith('Unknown-') or d.startswith(author + '-')) and
               any('DeadlockOfRain.dll' in fs for _, _, fs in os.walk(os.path.join(cache, d)))]
for d in set(gone + old_imports + [full]):
    if os.path.isdir(os.path.join(cache, d)): shutil.rmtree(os.path.join(cache, d))

# 3. the new package, as r2modman's import lays it out
now = int(time.time() * 1000)
mm = {'manifestVersion': 1, 'name': full, 'authorName': author, 'websiteUrl': manifest.get('website_url', ''),
      'displayName': name, 'description': manifest.get('description', ''), 'gameVersion': '', 'networkMode': '',
      'packageType': '', 'installMode': '', 'installedAtTime': now, 'loaders': [],
      'dependencies': manifest.get('dependencies', []), 'incompatibilities': [], 'optionalDependencies': [],
      'versionNumber': {'major': major, 'minor': minor, 'patch': patch}, 'enabled': True, 'icon': '',
      'onlineSource': False, 'trustedPackage': False}
for dest in (os.path.join(plugins, full), os.path.join(cache, full, manifest['version_number'])):
    os.makedirs(dest, exist_ok=True)
    with zipfile.ZipFile(zpath) as z: z.extractall(dest)
    json.dump(mm, open(os.path.join(dest, 'mm_v2_manifest.json'), 'w', encoding='utf-8'), separators=(',', ':'))
deps = ''.join(f'\n    - {d}' for d in mm['dependencies']) or ' []'
desc = mm['description'].replace("'", "''")
entry = f"""- manifestVersion: 1
  name: {full}
  authorName: {author}
  websiteUrl: ''
  displayName: {name}
  description: '{desc}'
  gameVersion: ''
  networkMode: ''
  packageType: ''
  installMode: ''
  installedAtTime: {now}
  loaders: []
  dependencies:{deps}
  incompatibilities: []
  optionalDependencies: []
  versionNumber:
    major: {major}
    minor: {minor}
    patch: {patch}
  enabled: true
  onlineSource: false
  trustedPackage: false
"""
keep = [b if b.endswith('\n') else b + '\n' for b in keep]
shutil.copy2(mods_yml, mods_yml + '.before-risk-of-deadlock')
open(mods_yml, 'w', encoding='utf-8', newline='\n').write(head + ''.join(keep) + entry)
print(f'== r2modman profile "{profile}": removed {", ".join(gone) or "nothing"}; installed {full} {manifest["version_number"]}')
if r2exe:
    # through PowerShell's Start-Process: a child of this script dies with the shell that ran it
    ps(f"Start-Process -FilePath '{r2exe}'")
    print('   r2modman started again: press Start modded.')
else:
    print('   Open r2modman and press Start modded.')
