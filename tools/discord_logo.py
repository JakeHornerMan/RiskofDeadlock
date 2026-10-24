"""Draws the Discord logo (white, transparent background) into plugin/Assets/discord.png, embedded in the DLL for the
title-screen button (Runtime/CommunityButton.cs). The outline is Discord's public brand path; no image is downloaded."""
import math, re, sys
from PIL import Image, ImageDraw

PATH = ("M107.7,8.07A105.15,105.15,0,0,0,81.47,0a72.06,72.06,0,0,0-3.36,6.83A97.68,97.68,0,0,0,49,6.83,72.37,72.37,0,0,0,45.64,0,"
        "105.89,105.89,0,0,0,19.39,8.09C2.79,32.65-1.71,56.6.54,80.21h0A105.73,105.73,0,0,0,32.71,96.36,77.7,77.7,0,0,0,39.6,85.25"
        "a68.42,68.42,0,0,1-10.85-5.18c.91-.66,1.8-1.34,2.66-2a75.57,75.57,0,0,0,64.32,0c.87.71,1.76,1.39,2.66,2a68.68,68.68,0,0,1"
        "-10.87,5.19,77,77,0,0,0,6.89,11.1A105.25,105.25,0,0,0,126.6,80.22h0C129.24,52.84,122.09,29.11,107.7,8.07Z"
        "M42.45,65.69C36.18,65.69,31,60,31,53s5-12.74,11.43-12.74S54,46,53.89,53,48.84,65.69,42.45,65.69Z"
        "m42.24,0C78.41,65.69,73.25,60,73.25,53s5-12.74,11.44-12.74S96.23,46,96.12,53,91.08,65.69,84.69,65.69Z")
W, H = 127.14, 96.36

def tokens(d):
    for m in re.finditer(r"[A-Za-z]|-?(?:\d+\.?\d*|\.\d+)(?:e-?\d+)?", d):
        yield m.group()

def arc(p0, rx, ry, phi, fa, fs, p1, n=24):
    (x1, y1), (x2, y2) = p0, p1
    c, s = math.cos(math.radians(phi)), math.sin(math.radians(phi))
    dx, dy = (x1 - x2) / 2, (y1 - y2) / 2
    x1p, y1p = c * dx + s * dy, -s * dx + c * dy
    lam = x1p**2 / rx**2 + y1p**2 / ry**2
    if lam > 1: rx, ry = rx * math.sqrt(lam), ry * math.sqrt(lam)
    num = rx**2 * ry**2 - rx**2 * y1p**2 - ry**2 * x1p**2
    den = rx**2 * y1p**2 + ry**2 * x1p**2
    k = math.sqrt(max(0, num / den)) * (-1 if fa == fs else 1)
    cxp, cyp = k * rx * y1p / ry, -k * ry * x1p / rx
    cx, cy = c * cxp - s * cyp + (x1 + x2) / 2, s * cxp + c * cyp + (y1 + y2) / 2
    ang = lambda ux, uy, vx, vy: math.atan2(ux * vy - uy * vx, ux * vx + uy * vy)
    t1 = ang(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry)
    dt = ang((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry)
    if not fs and dt > 0: dt -= 2 * math.pi
    if fs and dt < 0: dt += 2 * math.pi
    out = []
    for i in range(1, n + 1):
        t = t1 + dt * i / n
        out.append((cx + rx * math.cos(t) * c - ry * math.sin(t) * s, cy + rx * math.cos(t) * s + ry * math.sin(t) * c))
    return out

def bez(p0, p1, p2, p3, n=16):
    return [tuple((1-t)**3*a + 3*(1-t)**2*t*b + 3*(1-t)*t*t*cc + t**3*d for a, b, cc, d in zip(p0, p1, p2, p3))
            for t in (i / n for i in range(1, n + 1))]

def parse(d):
    toks = list(tokens(d)); i = 0; subs = []; cur = []; pos = (0, 0); start = (0, 0); cmd = None; last_c2 = None
    num = lambda: float(toks[i])
    while i < len(toks):
        if re.match(r"[A-Za-z]", toks[i]): cmd = toks[i]; i += 1
        rel = cmd.islower(); C = cmd.upper(); ox, oy = pos if rel else (0, 0)
        def take(k):
            nonlocal i
            v = [float(t) for t in toks[i:i + k]]; i += k; return v
        if C == "Z":
            if cur: subs.append(cur)
            cur = []; pos = start; last_c2 = None; continue
        if C == "M":
            x, y = take(2); pos = start = (ox + x, oy + y); cur = [pos]; cmd = "l" if rel else "L"; last_c2 = None
        elif C == "L":
            x, y = take(2); pos = (ox + x, oy + y); cur.append(pos); last_c2 = None
        elif C == "H":
            x, = take(1); pos = ((pos[0] if rel else 0) + x, pos[1]); cur.append(pos); last_c2 = None
        elif C == "V":
            y, = take(1); pos = (pos[0], (pos[1] if rel else 0) + y); cur.append(pos); last_c2 = None
        elif C == "C":
            a = take(6); p1, p2, p3 = (ox + a[0], oy + a[1]), (ox + a[2], oy + a[3]), (ox + a[4], oy + a[5])
            cur += bez(pos, p1, p2, p3); last_c2 = p2; pos = p3
        elif C == "S":
            a = take(4); p1 = (2 * pos[0] - last_c2[0], 2 * pos[1] - last_c2[1]) if last_c2 else pos
            p2, p3 = (ox + a[0], oy + a[1]), (ox + a[2], oy + a[3]); cur += bez(pos, p1, p2, p3); last_c2 = p2; pos = p3
        elif C == "A":
            rx, ry, phi, fa, fs, x, y = take(7); p1 = (ox + x, oy + y)
            cur += arc(pos, rx, ry, phi, int(fa), int(fs), p1); pos = p1; last_c2 = None
        else: raise ValueError(cmd)
    if cur: subs.append(cur)
    return subs

def main(out, size=256, pad=8):
    ss = 4; S = size * ss; scale = (S - 2 * pad * ss) / W; h = int(H * scale + 2 * pad * ss)
    img = Image.new("L", (S, h), 0); dr = ImageDraw.Draw(img)
    for k, sub in enumerate(parse(PATH)):
        dr.polygon([(pad * ss + x * scale, pad * ss + y * scale) for x, y in sub], fill=0 if k else 255)
    mask = img.resize((size, h // ss), Image.LANCZOS)
    rgba = Image.new("RGBA", mask.size, (255, 255, 255, 0)); rgba.putalpha(mask); rgba.save(out)
    print(out, rgba.size)

if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "plugin/Assets/discord.png")
