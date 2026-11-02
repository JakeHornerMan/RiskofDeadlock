"""Put the creator's own DLL build into their r2modman profile, in place of every older Risk of Deadlock package (creator,
2026-11-02: "can we automate this process where we delete the old package and import the new dll in r2modman").

    python tools/r2modman_install.py <zip> [--profile Deadlock]

Does what r2modman's Settings > Profile > Import local mod does, after removing the old ones by hand:
- every package in the profile that holds DeadlockOfRain.dll (or its disabled .old copy) is removed, from BepInEx/plugins and
  from the profile's mods.yml: the own-files builds and the old Thunderstore Risk_of_Deadlock alike (the same plugin twice
  clashes; Thunderstore was given up on 2026-11-02);
- r2modman's download cache of those packages goes too (the newest import is kept there, as r2modman keeps it);
- the zip goes to BepInEx/plugins/Unknown-<name>/ with r2modman's mm_v2_manifest.json, and its entry is added to mods.yml.
r2modman keeps mods.yml in memory and writes it back on any change, so r2modman and the game must both be closed; the script
stops otherwise. Never upload the zip: its DLL holds Deadlock's files.
"""
import json, os, re, shutil, subprocess, sys, time, zipfile

args = sys.argv[1:]
profile = 'Deadlock'
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


for app, what in (('Risk of Rain 2', 'Risk of Rain 2'), ('r2modman', 'r2modman')):
    if running(app):
        print(f'{what} is open: close it, then run this again (r2modman writes its own mod list back on close).'); sys.exit(1)

with zipfile.ZipFile(zpath) as z:
    manifest = json.loads(z.read('manifest.json').decode('utf-8-sig'))
name = manifest['name']
full = 'Unknown-' + name
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
old_imports = [d for d in (os.listdir(cache) if os.path.isdir(cache) else []) if d.startswith('Unknown-') and
               any('DeadlockOfRain.dll' in fs for _, _, fs in os.walk(os.path.join(cache, d)))]
for d in set(gone + old_imports + [full]):
    if os.path.isdir(os.path.join(cache, d)): shutil.rmtree(os.path.join(cache, d))

# 3. the new package, as r2modman's import lays it out
now = int(time.time() * 1000)
mm = {'manifestVersion': 1, 'name': full, 'authorName': 'Unknown', 'websiteUrl': manifest.get('website_url', ''),
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
  authorName: Unknown
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
print('   Open r2modman and press Start modded.')
