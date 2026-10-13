import struct, sys
import numpy as np
exec(open('tools/dlm_check.py').read().split('bones, pos, idx, wt = read')[0])
f = open(sys.argv[1], 'rb'); f.read(4)
nb = struct.unpack('<i', f.read(4))[0]
for i in range(nb): rs(f); f.read(4 + 12 + 16 + 12 + 64)
nv = struct.unpack('<i', f.read(4))[0]
pos = np.frombuffer(f.read(nv * 12), '<f4').reshape(nv, 3); f.read(nv * (12 + 8 + 32))
ns = struct.unpack('<i', f.read(4))[0]
for s in range(ns):
    mat = struct.unpack('<i', f.read(4))[0]; n = struct.unpack('<i', f.read(4))[0]
    ind = np.frombuffer(f.read(4 * n), '<i4').reshape(-1, 3)
    e = np.linalg.norm(pos[ind[:, 0]] - pos[ind[:, 1]], axis=1)
    print(f'submesh {s} mat {mat}: tris {len(ind)}, idx range {ind.min()}..{ind.max()}, median edge {np.median(e):.4f} m, edges>0.3m {np.mean(e > 0.3):.2%}')
