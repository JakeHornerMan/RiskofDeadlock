"""Loudness over time of decoded Deadlock sounds (docs/08 §3: check a sound's length and shape before picking it).

  python tools/sound_env.py <file.wav> [more.wav ...] [--step 0.05]

Prints each file's length, its loudest moment and a bar per step (RMS in dB below the file's peak), so a cast that is
really a 3 s build-up, or a hit whose thump comes 0.4 s in, shows at a glance. Decode the .vsnd first (doc 01 §8: the VRF
CLI writes .mp3, then `ffmpeg -i x.mp3 x.wav`; or the extractor's own cache, %LOCALAPPDATA%\\DeadlockOfRain\\cache\\sounds).
16-bit PCM WAV only (what both produce).
"""
import math, struct, sys, wave


def envelope(path, step):
    with wave.open(path, 'rb') as w:
        ch, rate, n = w.getnchannels(), w.getframerate(), w.getnframes()
        data = w.readframes(n)
    samples = struct.unpack('<%dh' % (len(data) // 2), data)
    mono = [sum(samples[i:i + ch]) / ch for i in range(0, len(samples) - ch + 1, ch)]
    per = max(1, int(rate * step))
    rms = []
    for i in range(0, len(mono), per):
        chunk = mono[i:i + per]
        rms.append(math.sqrt(sum(x * x for x in chunk) / len(chunk)) if chunk else 0.0)
    return len(mono) / rate, rms


def main():
    args = sys.argv[1:]
    step = 0.05
    if '--step' in args:
        i = args.index('--step'); step = float(args[i + 1]); del args[i:i + 2]
    for p in args:
        length, rms = envelope(p, step)
        peak = max(rms) or 1.0
        loud = rms.index(peak) * step
        # where the sound is within 20 dB of its peak: its "body"
        body = [i * step for i, r in enumerate(rms) if r > peak * 0.1]
        span = f'{body[0]:.2f}-{body[-1] + step:.2f} s' if body else '-'
        print(f'{p}: {length:.2f} s, loudest at {loud:.2f} s, body {span}')
        for i, r in enumerate(rms):
            db = 20 * math.log10(r / peak) if r > 0 else -60
            bar = '#' * max(0, int((db + 40) / 2))
            print(f'  {i * step:5.2f} {db:6.1f} dB {bar}')


if __name__ == '__main__':
    main()
