"""Dev check of a hero's holding pose: for each clip (frame 0 by default) report where the gun points relative to the
model's forward (+Z in Unity space) and how far the gun bone sits from the gun hand.
usage: python tools/dlm_pose.py <hero> [frame] [clip-substring]
"""
import os, struct, sys
import numpy as np

CACHE = os.path.join(os.environ['LOCALAPPDATA'], 'DeadlockOfRain', 'cache', 'heroes')

def rs(f):
    n = 0; shift = 0
    while True:
        b = f.read(1)[0]; n |= (b & 0x7f) << shift; shift += 7
        if b < 0x80: break
    return f.read(n).decode('utf-8')

def I(f): return struct.unpack('<i', f.read(4))[0]
def F(f, n): return struct.unpack(f'<{n}f', f.read(4 * n))

def read(hero):
    f = open(os.path.join(CACHE, hero, hero + '.dlm'), 'rb')
    assert f.read(4) in (b'DLM1', b'DLM2')
    bones = []
    for _ in range(I(f)):
        name = rs(f); parent = I(f); p = F(f, 3); q = F(f, 4); s = F(f, 3); F(f, 16)
        bones.append({'name': name, 'parent': parent, 'pos': p, 'rot': q, 'scale': s})
    nv = I(f); f.read(nv * (12 + 12 + 8 + 32))
    for _ in range(I(f)):
        I(f); f.read(I(f) * 4)
    for _ in range(I(f)):
        rs(f); rs(f); rs(f); f.read(4 + 4 + 16)
    clips = {}
    for _ in range(I(f)):
        name = rs(f); length, fps = F(f, 2); frames = I(f)
        tracks = {}
        for _ in range(I(f)):
            b = I(f)
            pos = [F(f, 3) for _ in range(I(f))]
            rot = [F(f, 4) for _ in range(I(f))]
            tracks[b] = (pos, rot)
        clips[name] = (length, fps, frames, tracks)
    return bones, clips

def qm(q):
    x, y, z, w = q
    return np.array([[1-2*(y*y+z*z), 2*(x*y-z*w), 2*(x*z+y*w)],
                     [2*(x*y+z*w), 1-2*(x*x+z*z), 2*(y*z-x*w)],
                     [2*(x*z-y*w), 2*(y*z+x*w), 1-2*(x*x+y*y)]])

def pose(bones, clip, frame):
    """World matrices with the clip applied (root position tracks skipped like the plugin does)."""
    tracks = clip[3] if clip else {}
    world = [None] * len(bones)
    for i, b in enumerate(bones):
        p, q = b['pos'], b['rot']
        if i in tracks:
            pos, rot = tracks[i]
            is_root = b['parent'] < 0 or b['name'] == 'root_motion'
            if pos and not is_root: p = pos[min(frame, len(pos) - 1)]
            if rot: q = rot[min(frame, len(rot) - 1)]
        m = np.eye(4); m[:3, :3] = qm(q) * np.array(b['scale']); m[:3, 3] = p
        world[i] = world[b['parent']] @ m if b['parent'] >= 0 else m
    return world

def main():
    hero = sys.argv[1]; frame = int(sys.argv[2]) if len(sys.argv) > 2 else 0
    sub = sys.argv[3] if len(sys.argv) > 3 else ''
    bones, clips = read(hero)
    idx = {b['name']: i for i, b in enumerate(bones)}
    hands = [h for h in ['hand_R', 'hand_L'] if h in idx]
    muzzles = {'hand_R': next((m for m in ['muzzle_fx'] if m in idx), None), 'hand_L': 'muzzle_fx_L' if 'muzzle_fx_L' in idx else None}
    for name in sorted(clips):
        if sub not in name: continue
        w = pose(bones, clips[name], frame)
        out = []
        for h in hands:
            m = muzzles[h]
            if not m: continue
            hp = w[idx[h]][:3, 3]; mp = w[idx[m]][:3, 3]
            d = mp - hp; d /= np.linalg.norm(d)
            yaw = np.degrees(np.arctan2(d[0], d[2])); pitch = np.degrees(np.arcsin(d[1]))
            out.append(f'{h}: yaw {yaw:6.1f} pitch {pitch:6.1f} hand@({hp[0]:.2f},{hp[1]:.2f},{hp[2]:.2f})')
        if 'weapon' in idx and 'hand_R' in idx:
            g = w[idx['weapon']][:3, 3]; hp = w[idx['hand_R']][:3, 3]
            out.append(f'weapon-hand_R {np.linalg.norm(g - hp):.3f}')
        print(f'{name:40s} ' + ' | '.join(out))

if __name__ == '__main__':
    main()
