"""Downloads the R2API assemblies the creator's own DLL build compiles against (Troopers, 2026-11-11; docs/11) into the
gitignored build/ref/. Compile-time only: R2API is never bundled; the r2modman package lists it as a dependency
(R2Wiki Cookbook), so r2modman installs it beside the mod. Same versions as DllBuilds/package/manifest.json."""
import io, os, sys, urllib.request, zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, 'build', 'ref')
PKGS = [('r2api.core', '5.3.0', 'R2API.Core.dll'), ('r2api.director', '3.1.3', 'R2API.Director.dll'),
        ('r2api.addressables', '1.1.3', 'R2API.Addressables.dll')]
os.makedirs(OUT, exist_ok=True)
for pid, ver, dll in PKGS:
    dst = os.path.join(OUT, dll)
    if os.path.exists(dst) and '--force' not in sys.argv: continue
    url = f'https://api.nuget.org/v3-flatcontainer/{pid}/{ver}/{pid}.{ver}.nupkg'
    data = urllib.request.urlopen(url, timeout=60).read()
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        name = next(n for n in z.namelist() if n.endswith('/' + dll))
        open(dst, 'wb').write(z.read(name))
    print(f'fetch_r2api: {dll} {ver}')
print('fetch_r2api: ' + ', '.join(p[2] for p in PKGS) + ' in ' + OUT)
