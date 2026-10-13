import struct, sys
import numpy as np
src = open('tools/dlm_check.py').read().split('bones, pos, idx, wt = read')[0]
exec(src)
bones, pos, idx, wt = read(sys.argv[1])
f = open(sys.argv[1], 'rb'); f.read(4)
nb = struct.unpack('<i', f.read(4))[0]
for i in range(nb): rs(f); f.read(4 + 12 + 16 + 12 + 64)
nv = struct.unpack('<i', f.read(4))[0]; f.read(nv * (12 + 12 + 8 + 32))
ns = struct.unpack('<i', f.read(4))[0]
subs = []
for s in range(ns):
    mat = struct.unpack('<i', f.read(4))[0]; n = struct.unpack('<i', f.read(4))[0]
    subs.append(np.frombuffer(f.read(4 * n), '<i4'))
print('weights: sum min/max', wt.sum(1).min(), wt.sum(1).max(), 'neg', (wt < 0).sum(), 'nan', np.isnan(wt).sum(), 'idx<0', (idx < 0).sum(), 'idx>=nb', (idx >= nb).sum())
for s, ind in enumerate(subs):
    v = np.unique(ind)
    w = wt[v]; i = idx[v]
    print(f'sub {s}: verts {len(v)} pos y {pos[v,1].min():.2f}..{pos[v,1].max():.2f}  weight sum {w.sum(1).min():.3f}..{w.sum(1).max():.3f}  bones used {len(np.unique(i[w>0.001]))}  top bone {bones[np.bincount(i[:,0]).argmax()][0]}')
f = open(sys.argv[1], 'rb'); f.read(4)
nb = struct.unpack('<i', f.read(4))[0]
for i in range(nb): rs(f); f.read(4 + 12 + 16 + 12 + 64)
nv = struct.unpack('<i', f.read(4))[0]; f.read(nv * 12)
nrm = np.frombuffer(f.read(nv * 12), '<f4').reshape(nv, 3)
uv = np.frombuffer(f.read(nv * 8), '<f4').reshape(nv, 2)
L = np.linalg.norm(nrm, axis=1)
for s, ind in enumerate(subs):
    v = np.unique(ind)
    print(f'sub {s}: normal len {np.nanmin(L[v]):.3f}..{np.nanmax(L[v]):.3f} nan {np.isnan(L[v]).sum()}  uv {uv[v].min(0).round(2)}..{uv[v].max(0).round(2)} uvnan {np.isnan(uv[v]).sum()}')
