"""Dev check: every cached DLM model and its materials whose settings differ from plain (tint, alpha mode, DLM2 emission and
vertex-colour flag). Run after any extractor change, before testing, to see which heroes and props changed look."""
import struct, sys, os, glob
def rs(f):
    n=0; sh=0
    while True:
        b=f.read(1)[0]; n|=(b&0x7f)<<sh; sh+=7
        if b<0x80: break
    return f.read(n).decode('utf-8')
def i32(f): return struct.unpack('<i',f.read(4))[0]
def fl(f,n): return struct.unpack('<%df'%n,f.read(4*n))
def read(p):
    f=open(p,'rb'); mg=f.read(4)
    nb=i32(f)
    for _ in range(nb): rs(f); f.read(4+12+16+12+64)
    nv=i32(f); f.read(nv*(12+12+8+32))
    ns=i32(f)
    for _ in range(ns): i32(f); n=i32(f); f.read(4*n)
    nm=i32(f); mats=[]
    for _ in range(nm):
        name=rs(f); a=rs(f); nn=rs(f); am=i32(f); cut=fl(f,1)[0]; col=fl(f,4); mats.append([name,a,am,col])
    nc=i32(f)
    for _ in range(nc):
        rs(f); fl(f,2); i32(f); nt=i32(f)
        for _ in range(nt):
            i32(f); n=i32(f); f.read(12*n); n=i32(f); f.read(16*n)
    if mg==b'DLM2':
        hc=i32(f)
        if hc: f.read(16*nv)
        for m in mats: e=fl(f,3); vc=i32(f); m+= [e,vc]
    return mg,mats
root=os.path.expandvars(r'%LOCALAPPDATA%\DeadlockOfRain\cache')
for p in sorted(glob.glob(root+r'\heroes\*\*.dlm')+glob.glob(root+r'\props\*\*.dlm')):
    mg,mats=read(p); print(os.path.basename(p), mg)
    for m in mats:
        extra=m[4:] if len(m)>4 else []
        if m[3]!=(1.0,1.0,1.0,1.0) or (extra and (any(extra[0]) or extra[1])) or m[2]:
            print('   ',m[0][:40], 'alpha',m[2],'col',tuple(round(x,2) for x in m[3]), 'em',tuple(round(x,2) for x in extra[0]) if extra else '', 'vc',extra[1] if extra else '')
