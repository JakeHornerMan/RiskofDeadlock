#!/usr/bin/env python3
"""Short summaries of Deadlock particle recipes dumped by vrfdump (docs/VfxRebuild/README.md).

usage: python tools/vpcf_summary.py <folder of .txt dumps> [name-substring ...]  > summary.txt

For each recipe: particle cap, how it emits, lifetime, size, colours (as hex), blend, textures/models, the operators that
shape its motion, and its child recipes. Read to rebuild the look in Unity; nothing here is shipped.
"""
import os
import re
import sys

TOKEN = re.compile(r'\s*(?:(<!--.*?-->)|([{}\[\],=])|((?:resource(?:_compiled)?|soundevent|panorama|subclass):)?"((?:[^"\\]|\\.)*)"|([^\s{}\[\],=]+))', re.S)

FIELDS = {0: 'position', 1: 'lifetime', 3: 'radius', 4: 'roll', 5: 'rollspeed', 6: 'color', 7: 'alpha', 8: 'creation time', 9: 'sequence',
          10: 'trail length', 12: 'yaw', 16: 'alpha2', 18: 'radius scale?', 20: 'pitch', 21: 'normal'}


def parse(text):
    toks = []
    for m in TOKEN.finditer(text):
        if m.group(1):
            continue
        if m.group(2):
            toks.append(('p', m.group(2)))
        elif m.group(4) is not None:
            toks.append(('s', m.group(4)))
        elif m.group(5):
            toks.append(('w', m.group(5)))
    pos = 0

    def value():
        nonlocal pos
        kind, tok = toks[pos]
        if kind == 'p' and tok == '{':
            pos += 1
            d = {}
            while toks[pos] != ('p', '}'):
                key = toks[pos][1]
                pos += 1
                if toks[pos] == ('p', '='):
                    pos += 1
                d[key] = value()
                if toks[pos] == ('p', ','):
                    pos += 1
            pos += 1
            return d
        if kind == 'p' and tok == '[':
            pos += 1
            arr = []
            while toks[pos] != ('p', ']'):
                arr.append(value())
                if toks[pos] == ('p', ','):
                    pos += 1
            pos += 1
            return arr
        pos += 1
        if kind == 'w':
            try:
                return float(tok)
            except ValueError:
                return {'true': True, 'false': False}.get(tok, tok)
        return tok

    return value()


def hexcol(c):
    try:
        return '#%02X%02X%02X' % tuple(int(max(0, min(255, round(x)))) for x in c[:3]) + (('/a%d' % round(c[3])) if len(c) > 3 and c[3] != 255 else '')
    except Exception:
        return str(c)


def num(v):
    """A particle float input as text: literal, random range or 'curve'."""
    if isinstance(v, (int, float)):
        return '%g' % v
    if isinstance(v, dict):
        t = v.get('m_nType', '')
        if t == 'PF_TYPE_LITERAL':
            return '%g' % v.get('m_flLiteralValue', 0)
        if 'RANDOM' in t:
            return '%g..%g' % (v.get('m_flRandomMin', 0), v.get('m_flRandomMax', 0))
        if t == 'PF_TYPE_NAMED_VALUE':
            return 'named:' + str(v.get('m_NamedValue'))
        if 'CONTROL_POINT' in t:
            return 'cp%g' % v.get('m_nControlPoint', 0)
        if 'CURVE' in str(v.get('m_nMapType', '')) or t.endswith('AGE') or 'PARTICLE' in t:
            sp = v.get('m_Curve', {}).get('m_spline', [])
            pts = ' '.join('%g:%g' % (p.get('x', 0), p.get('y', 0)) for p in sp[:6] if isinstance(p, dict))
            return '%s[%g>%g%s]' % (t.replace('PF_TYPE_', '').lower(), v.get('m_flOutput0', 0), v.get('m_flOutput1', 0), (' curve ' + pts) if pts else '')
        return t.replace('PF_TYPE_', '').lower()
    return str(v)


def colours(node, out, path=''):
    if isinstance(node, dict):
        for k, v in node.items():
            if isinstance(v, list) and 3 <= len(v) <= 4 and all(isinstance(x, (int, float)) for x in v) and 'olor' in k:
                out.append((k.replace('m_', ''), hexcol(v)))
            elif k == 'm_Stops' and isinstance(v, list):
                stops = ' '.join('%g:%s' % (s.get('m_Position', s.get('m_flPosition', 0)), hexcol(s.get('m_Color', []))) for s in v if isinstance(s, dict))
                out.append(('gradient', stops))
            else:
                colours(v, out, k)
    elif isinstance(node, list):
        for v in node:
            colours(v, out, path)


def refs(node, out):
    if isinstance(node, dict):
        for k, v in node.items():
            if isinstance(v, str) and ('/' in v) and (v.endswith(('.vtex', '.vmat', '.vmdl', '.vpcf', '.vsnap'))):
                out.append((k.replace('m_', ''), v))
            else:
                refs(v, out)
    elif isinstance(node, list):
        for v in node:
            refs(v, out)


def op_line(op):
    c = op.get('_class', '?').replace('C_OP_', '').replace('C_INIT_', '')
    bits = []
    if c == 'InitFloat':
        bits.append('%s=%s' % (FIELDS.get(int(op.get('m_nOutputField', 3)), 'field%g' % op.get('m_nOutputField', 3)), num(op.get('m_InputValue'))))
    elif c in ('ContinuousEmitter',):
        bits.append('rate=%s for %s' % (num(op.get('m_flEmitRate')), num(op.get('m_flEmissionDuration'))))
    elif c in ('InstantaneousEmitter',):
        bits.append('count=%s at %s' % (num(op.get('m_nParticlesToEmit')), num(op.get('m_flStartTime'))))
    else:
        for k, v in op.items():
            if k in ('_class', 'm_nOpEndCapState', 'm_Notes') or k.startswith('m_bDisable'):
                continue
            if isinstance(v, (int, float)) and not isinstance(v, bool) and v not in (0, 1, -1) and len(bits) < 6:
                bits.append('%s=%g' % (k.replace('m_fl', '').replace('m_n', '').replace('m_', ''), v))
            elif isinstance(v, dict) and 'm_nType' in v and len(bits) < 6:
                s = num(v)
                if s not in ('0', '1'):
                    bits.append('%s=%s' % (k.replace('m_fl', '').replace('m_n', '').replace('m_', ''), s))
            elif isinstance(v, list) and v and all(isinstance(x, (int, float)) for x in v) and len(v) == 3 and any(v) and 'olor' not in k and len(bits) < 6:
                bits.append('%s=%s' % (k.replace('m_vec', '').replace('m_v', '').replace('m_', ''), '/'.join('%g' % x for x in v)))
            elif isinstance(v, str) and v.startswith(('PARTICLE_', 'PF_', 'P_', 'SEQUENCE_')) is False and k in ('m_nOutputBlendMode', 'm_nOrientationType', 'm_nColorBlendType'):
                bits.append('%s=%s' % (k.replace('m_n', ''), v))
    return c + ('(' + ', '.join(bits) + ')' if bits else '')


def summarise(path):
    d = parse(open(path, encoding='utf-8', errors='replace').read())
    name = os.path.basename(path)[:-4]
    lines = ['== ' + name]
    head = ['max %g' % d.get('m_nMaxParticles', 0)]
    for k, label in (('m_flConstantRadius', 'radius'), ('m_flConstantLifespan', 'life'), ('m_nConstantSequenceNumber', 'seq')):
        if k in d and d[k] not in (5, 1, 0):
            head.append('%s %g' % (label, d[k]))
    if 'm_ConstantColor' in d:
        head.append('colour ' + hexcol(d['m_ConstantColor']))
    lines.append('  ' + ', '.join(head))
    for key, label in (('m_Emitters', 'emit'), ('m_Initializers', 'init'), ('m_Operators', 'ops'), ('m_ForceGenerators', 'forces'), ('m_Constraints', 'limits')):
        ops = [o for o in d.get(key, []) if isinstance(o, dict) and not o.get('m_bDisableOperator')]
        if ops:
            lines.append('  %s: %s' % (label, '; '.join(op_line(o) for o in ops)))
    for r in d.get('m_Renderers', []):
        if not isinstance(r, dict) or r.get('m_bDisableOperator'):
            continue
        bits = [r.get('_class', '?').replace('C_OP_', '')]
        for k in ('m_nOutputBlendMode', 'm_nOrientationType', 'm_bAdditive', 'm_flAnimationRate', 'm_flOverbrightFactor', 'm_flAddSelfAmount', 'm_flRadiusScale', 'm_flAlphaScale',
                  'm_bSaturateColorPreAlphaBlend', 'm_flSelfIllumAmount', 'm_nSequenceCombineMode', 'm_flRefractAmount'):
            if k in r and r[k] not in (False, 0):
                bits.append('%s=%s' % (k.replace('m_fl', '').replace('m_n', '').replace('m_b', ''), num(r[k]) if not isinstance(r[k], (str, bool)) else r[k]))
        rr = []
        refs(r, rr)
        bits += ['%s' % v for _, v in rr]
        cc = []
        colours(r, cc)
        bits += ['%s %s' % c for c in cc[:4]]
        lines.append('  render: ' + ' | '.join(bits))
    cc = []
    for key in ('m_Initializers', 'm_Operators'):
        colours(d.get(key, []), cc)
    seen = []
    for c in cc:
        if c not in seen:
            seen.append(c)
    if seen:
        lines.append('  colours: ' + '; '.join('%s %s' % c for c in seen[:10]))
    kids = []
    for ch in d.get('m_Children', []):
        if isinstance(ch, dict) and not ch.get('m_bDisableChild'):
            ref = ch.get('m_ChildRef', '')
            kids.append(os.path.basename(str(ref)).replace('.vpcf', '') + (' +%gs' % ch['m_flDelay'] if ch.get('m_flDelay') else '') + (' (endcap)' if ch.get('m_bEndCap') else ''))
    if kids:
        lines.append('  children: ' + ', '.join(kids))
    return '\n'.join(lines)


def brief(path):
    """One line: name | cap and emit | every colour | textures | add or alpha | life and size."""
    d = parse(open(path, encoding='utf-8', errors='replace').read())
    cc = []
    colours(d, cc)
    cols = []
    for k, v in cc:
        for h in re.findall(r'#[0-9A-F]{6}', v):
            if h not in cols and h not in ('#000000', '#FFFFFF'):
                cols.append(h)
    rr = []
    refs(d.get('m_Renderers', []), rr)
    tex = sorted({os.path.basename(v).split('.')[0] for _, v in rr})
    em = []
    for o in d.get('m_Emitters', []):
        if isinstance(o, dict):
            em.append(op_line(o).replace('InstantaneousEmitter', 'burst').replace('ContinuousEmitter', 'rate').replace(' at None', '').replace(' for None', ''))
    lf = [op_line(o) for o in d.get('m_Initializers', []) if isinstance(o, dict) and o.get('_class') == 'C_INIT_InitFloat' and int(o.get('m_nOutputField', 3)) in (1, 3)]
    kinds = sorted({r.get('_class', '').replace('C_OP_Render', '') for r in d.get('m_Renderers', []) if isinstance(r, dict)})
    add = any('ADD' in str(r.get('m_nOutputBlendMode', '')) for r in d.get('m_Renderers', []) if isinstance(r, dict))
    kids = len([c for c in d.get('m_Children', []) if isinstance(c, dict)])
    return '%s | max %g %s | %s | %s | %s%s | %s%s' % (os.path.basename(path)[:-4], d.get('m_nMaxParticles', 0), ' '.join(em), ' '.join(cols[:8]) or '-', ','.join(tex) or '-',
                                                 '/'.join(kinds) or '-', ' add' if add else '', ' '.join(x.replace('InitFloat', '') for x in lf), (' | %d children' % kids) if kids else '')


def main():
    if sys.argv[1] == '--brief':
        folder = sys.argv[2]
        wants = [w.lower() for w in sys.argv[3:]]
        for f in sorted(os.listdir(folder)):
            if f.endswith('.txt') and (not wants or any(w in f.lower() for w in wants)):
                try:
                    print(brief(os.path.join(folder, f)))
                except Exception as e:
                    print(f[:-4], '| unreadable:', e)
        return
    folder = sys.argv[1]
    wants = [w.lower() for w in sys.argv[2:]]
    for f in sorted(os.listdir(folder)):
        if not f.endswith('.txt') or wants and not any(w in f.lower() for w in wants):
            continue
        try:
            print(summarise(os.path.join(folder, f)))
        except Exception as e:  # one unreadable recipe never stops the rest
            print('== %s\n  (could not be read: %s)' % (f[:-4], e))


if __name__ == '__main__':
    main()
