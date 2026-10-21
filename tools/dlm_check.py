"""Dev check of a DLM file: skin the mesh with the rest skeleton and report how far vertices move (should be ~0)."""
import struct, sys
import numpy as np

def rs(f):
    n = 0; shift = 0
    while True:
        b = f.read(1)[0]; n |= (b & 0x7f) << shift; shift += 7
        if b < 0x80: break
    return f.read(n).decode('utf-8')

def quat_mat(x, y, z, w):
    return np.array([[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
                     [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
                     [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]])

def read(path):
    f = open(path, 'rb')
    assert f.read(4) in (b'DLM1', b'DLM2')
    nb = struct.unpack('<i', f.read(4))[0]
    bones = []
    for i in range(nb):
        name = rs(f); parent = struct.unpack('<i', f.read(4))[0]
        p = struct.unpack('<3f', f.read(12)); q = struct.unpack('<4f', f.read(16)); s = struct.unpack('<3f', f.read(12))
        bp = np.array(struct.unpack('<16f', f.read(64))).reshape(4, 4)
        bones.append((name, parent, p, q, s, bp))
    nv = struct.unpack('<i', f.read(4))[0]
    pos = np.frombuffer(f.read(nv * 12), dtype='<f4').reshape(nv, 3)
    f.read(nv * 12); f.read(nv * 8)
    bw = np.frombuffer(f.read(nv * 32), dtype=np.uint8).reshape(nv, 32)
    idx = bw[:, :16].copy().view('<i4').reshape(nv, 4); wt = bw[:, 16:].copy().view('<f4').reshape(nv, 4)
    return bones, pos, idx, wt

bones, pos, idx, wt = read(sys.argv[1])
world = [None] * len(bones)
def W(i):
    if world[i] is not None: return world[i]
    name, parent, p, q, s, bp = bones[i]
    m = np.eye(4); m[:3, :3] = quat_mat(*q) * np.array(s); m[:3, 3] = p
    world[i] = (W(parent) @ m) if parent >= 0 else m
    return world[i]
skin = [W(i) @ bones[i][5] for i in range(len(bones))]
dev = np.array([np.abs(skin[i] - np.eye(4)).max() for i in range(len(bones))])
print('bones', len(bones), 'verts', len(pos), 'height', pos[:, 1].max() - pos[:, 1].min())
bad = np.where(dev > 1e-2)[0]
print('bones whose world*bindpose != I:', len(bad), [bones[i][0] for i in bad[:15]])
used = set(idx[wt > 0.01].ravel().tolist())
print('weighted bones that are bad:', len(used & set(bad.tolist())), 'max idx', idx.max(), 'parents of bad:', [bones[bones[i][1]][0] if bones[i][1] >= 0 else None for i in bad[:8]])

# ---- clips: pose with frame 0 of a clip and measure the skinned mesh
def read_rest(path):
    f = open(path, 'rb'); f.read(4)
    nb = struct.unpack('<i', f.read(4))[0]
    for i in range(nb): rs(f); f.read(4 + 12 + 16 + 12 + 64)
    nv = struct.unpack('<i', f.read(4))[0]; f.read(nv * (12 + 12 + 8 + 32))
    ns = struct.unpack('<i', f.read(4))[0]
    for s in range(ns): f.read(4); n = struct.unpack('<i', f.read(4))[0]; f.read(4 * n)
    nm = struct.unpack('<i', f.read(4))[0]
    for m in range(nm): rs(f); rs(f); rs(f); f.read(4 + 4 + 16)
    nc = struct.unpack('<i', f.read(4))[0]
    clips = {}
    for c in range(nc):
        name = rs(f); length, fps = struct.unpack('<2f', f.read(8)); frames = struct.unpack('<i', f.read(4))[0]
        nt = struct.unpack('<i', f.read(4))[0]; tracks = {}
        for t in range(nt):
            b = struct.unpack('<i', f.read(4))[0]
            npf = struct.unpack('<i', f.read(4))[0]; P = np.frombuffer(f.read(12 * npf), '<f4').reshape(npf, 3)
            nr = struct.unpack('<i', f.read(4))[0]; Q = np.frombuffer(f.read(16 * nr), '<f4').reshape(nr, 4)
            tracks[b] = (P, Q)
        clips[name] = (length, frames, tracks)
    return clips

clips = read_rest(sys.argv[1])
clip = sys.argv[2] if len(sys.argv) > 2 else 'weapon_stand_idle'
length, frames, tracks = clips[clip]
world = [None] * len(bones)
def W2(i):
    if world[i] is not None: return world[i]
    name, parent, p, q, s, bp = bones[i]
    if i in tracks:
        P, Q = tracks[i]
        if len(P): p = P[0]
        q = Q[0]
    m = np.eye(4); m[:3, :3] = quat_mat(*q) * np.array(s); m[:3, 3] = p
    world[i] = (W2(parent) @ m) if parent >= 0 else m
    return world[i]
skin = np.array([W2(i) @ bones[i][5] for i in range(len(bones))])
v = np.c_[pos, np.ones(len(pos))]
out = np.zeros((len(pos), 3))
for k in range(4):
    out += wt[:, k:k+1] * np.einsum('nij,nj->ni', skin[idx[:, k]], v)[:, :3]
print(f'clip {clip}: {len(tracks)} tracks, posed bounds min {out.min(0).round(2)} max {out.max(0).round(2)}')
moved = [(bones[i][0], np.round(np.array(tracks[i][0][0]) - np.array(bones[i][2]), 3)) for i in tracks if len(tracks[i][0])][:6]
print('first track pos deltas vs rest:', moved)
