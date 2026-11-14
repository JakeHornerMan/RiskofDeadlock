#!/usr/bin/env python3
"""Compare fps bench results (FPS=1 tools/run_bench.sh, docs/VfxRebuild/README.md).

usage: python tools/fps_compare.py --before b1.txt [b2.txt ...] --after a1.txt [a2.txt ...] [--hero haze] [--md]
       python tools/fps_compare.py --noise r1.txt r2.txt r3.txt      (spread between runs of the same build)

Each file holds "fps <hero> <pass>: key=value ..." lines. A pass's cost is its average frame time minus the average of
the same run's idle passes (idle, idle2), so the stage, the day and a warm PC cancel out. With several files per side the
median run is used. Exits 1 when a gate fails:
  cost over idle   after <= before + max(5 %, 0.3 ms, NOISE ms)
  p99 over idle    after <= before + max(10 %, NOISE ms)
  worst frame      after <= before + 5 ms + NOISE
  first-use hitch  after <= max(50 ms, before + NOISE)
  live particles   after peak <= max(before peak, budget)   (reported, not a failure: a richer effect may use more)
NOISE (default 1.0 ms) is the run-to-run spread measured with --noise; set it with --noise-ms.
"""
import argparse
import re
import statistics
import sys

PASSES = ['primary', 'secondary', 'utility', 'special', 'equipment', 'all', 'x4']
NAMES = {'primary': 'M1', 'secondary': 'M2', 'utility': 'Shift', 'special': 'R', 'equipment': 'Q', 'all': 'all', 'x4': 'x4'}


def load(path):
    """{hero: {pass: {key: float}}} for one run, with 'cost' and 'p99cost' added."""
    out = {}
    for line in open(path, encoding='utf-8', errors='replace'):
        m = re.match(r'fps (\S+) (\S+): (.*)', line.strip())
        if not m:
            continue
        hero = m.group(1).replace('Deadlock', '').lower()
        vals = {}
        for kv in m.group(3).split():
            k, _, v = kv.partition('=')
            try:
                vals[k] = float(v)
            except ValueError:
                vals[k] = v
        out.setdefault(hero, {})[m.group(2)] = vals
    for hero, passes in out.items():
        idles = [passes[k] for k in ('idle', 'idle2') if k in passes]
        if not idles:
            continue
        base = statistics.mean(i['avg'] for i in idles)
        base99 = statistics.mean(i['p99'] for i in idles)
        for name, v in passes.items():
            v['cost'] = v['avg'] - base
            v['p99cost'] = v['p99'] - base99
            v['idle'] = base
    return out


def median_of(runs, hero, name, key):
    vals = [r[hero][name][key] for r in runs if hero in r and name in r[hero] and key in r[hero][name]]
    return statistics.median(vals) if vals else None


def spread_of(runs, hero, name, key):
    vals = [r[hero][name][key] for r in runs if hero in r and name in r[hero] and key in r[hero][name]]
    return max(vals) - min(vals) if len(vals) > 1 else 0.0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--before', nargs='*', default=[])
    ap.add_argument('--after', nargs='*', default=[])
    ap.add_argument('--noise', nargs='*', default=[])
    ap.add_argument('--noise-ms', type=float, default=1.0)
    ap.add_argument('--hero', default='')
    ap.add_argument('--md', action='store_true')
    a = ap.parse_args()

    if a.noise:
        runs = [load(f) for f in a.noise]
        worst = 0.0
        for hero in sorted({h for r in runs for h in r}):
            for name in PASSES:
                s = spread_of(runs, hero, name, 'cost')
                worst = max(worst, s)
                print(f'{hero:10} {NAMES[name]:6} cost spread {s:5.2f} ms over {len(runs)} runs '
                      f'(median {median_of(runs, hero, name, "cost") or 0:5.2f})')
        print(f'noise floor (largest spread): {worst:.2f} ms')
        return 0

    before = [load(f) for f in a.before]
    after = [load(f) for f in a.after]
    heroes = sorted({h for r in after for h in r} & {h for r in before for h in r})
    if a.hero:
        heroes = [h for h in heroes if h in a.hero.split(',')]
    n = a.noise_ms
    failed = []
    sep = ' | ' if a.md else '  '
    head = ['hero', 'pass', 'idle b/a ms', 'cost before', 'cost after', 'change', 'p99 b/a', 'worst b/a', 'first b/a', 'particles b/a', 'lights b/a', 'verdict']
    if a.md:
        print('| ' + ' | '.join(head) + ' |')
        print('|' + '---|' * len(head))
    else:
        print(sep.join(head))
    for hero in heroes:
        for name in PASSES:
            cb, ca = median_of(before, hero, name, 'cost'), median_of(after, hero, name, 'cost')
            if cb is None or ca is None:
                continue
            g = lambda runs, k: median_of(runs, hero, name, k) or 0.0
            why = []
            if ca > cb + max(0.05 * abs(cb), 0.3, n):
                why.append('cost')
            if g(after, 'p99cost') > g(before, 'p99cost') + max(0.10 * abs(g(before, 'p99cost')), n):
                why.append('p99')
            if g(after, 'worst') > g(before, 'worst') + 5 + n:
                why.append('worst frame')
            if g(after, 'first') > max(50.0, g(before, 'first') + n):
                why.append('first use')
            verdict = 'ok' if not why else 'FAIL: ' + ', '.join(why)
            if why:
                failed.append(f'{hero} {NAMES[name]}: {", ".join(why)}')
            row = [hero, NAMES[name], f'{g(before, "idle"):.1f}/{g(after, "idle"):.1f}', f'{cb:+.2f}', f'{ca:+.2f}', f'{ca - cb:+.2f}',
                   f'{g(before, "p99"):.1f}/{g(after, "p99"):.1f}', f'{g(before, "worst"):.0f}/{g(after, "worst"):.0f}',
                   f'{g(before, "first"):.0f}/{g(after, "first"):.0f}',
                   f'{g(before, "particles_peak"):.0f}/{g(after, "particles_peak"):.0f}',
                   f'{g(before, "lights_peak"):.0f}/{g(after, "lights_peak"):.0f}', verdict]
            print(('| ' + ' | '.join(row) + ' |') if a.md else sep.join(row))
    if failed:
        print('\nFPS GATE FAILED: ' + '; '.join(failed))
        return 1
    print('\nFPS GATE PASSED')
    return 0


if __name__ == '__main__':
    sys.exit(main())
