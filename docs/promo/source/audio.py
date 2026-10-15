"""Original soundtrack for the PC Remote promo: 120 BPM bed + UI sound effects.

Everything is synthesised here (no samples). Cue times come from timeline.json so
the effects land on the same frames as the animation. Writes soundtrack.wav
(48 kHz, 16-bit stereo, exactly timeline.duration seconds), or the path given as
the first argument.
"""
import json
import sys
import wave
from pathlib import Path

import numpy as np

HERE = Path(__file__).resolve().parent
TL = json.loads((HERE / "timeline.json").read_text(encoding="utf-8"))
SR = 48000
DUR = TL["duration"]
N = int(round(DUR * SR))
rng = np.random.default_rng(20261015)


# ------------------------------------------------------------------ helpers
def tt(d):
    return np.arange(int(round(d * SR))) / SR


def mtof(m):
    return 440.0 * 2 ** ((m - 69) / 12)


def stereo(sig, pan=0.0):
    if sig.ndim == 2:
        return sig
    a = (pan + 1) * np.pi / 4
    return np.stack([sig * np.cos(a), sig * np.sin(a)], 1) * np.sqrt(2)


def place(bus, sig, t0, gain=1.0, pan=0.0):
    st = stereo(sig, pan) * gain
    k = min(int(0.015 * SR), len(st) // 4)  # end every buffer at zero so nothing ticks when it stops
    st[len(st) - k:] *= np.linspace(1, 0, k)[:, None]
    i0 = int(round(t0 * SR))
    if i0 < 0:
        st, i0 = st[-i0:], 0
    n = min(len(st), N - i0)
    if n > 0:
        bus[i0:i0 + n] += st[:n]


def fft_filter(x, resp):
    n = len(x)
    nf = 1 << int(np.ceil(np.log2(n + 8192)))
    X = np.fft.rfft(x, nf, axis=0)
    g = resp(np.fft.rfftfreq(nf, 1 / SR))
    X *= g[:, None] if x.ndim == 2 else g
    return np.fft.irfft(X, nf, axis=0)[:n]


def lp(fc, order=2):
    return lambda f: 1 / np.sqrt(1 + (f / fc) ** (2 * order))


def hp(fc, order=2):
    return lambda f: 1 / np.sqrt(1 + (fc / np.maximum(f, 1e-3)) ** (2 * order))


def bp(lo, hi, order=2):
    return lambda f: lp(hi, order)(f) * hp(lo, order)(f)


def svf(x, fc, q=0.8, mode="bp"):
    """Time-varying TPT state-variable filter (fc may be an array)."""
    fc = np.broadcast_to(np.asarray(fc, dtype=float), x.shape)
    g = np.tan(np.pi * np.clip(fc, 20, SR * 0.45) / SR)
    k = 1 / q
    a1 = 1 / (1 + g * (g + k))
    a2 = g * a1
    a3 = g * a2
    y = np.zeros_like(x)
    ic1 = ic2 = 0.0
    for i in range(len(x)):
        v3 = x[i] - ic2
        v1 = a1[i] * ic1 + a2[i] * v3
        v2 = ic2 + a2[i] * ic1 + a3[i] * v3
        ic1, ic2 = 2 * v1 - ic1, 2 * v2 - ic2
        y[i] = v1 if mode == "bp" else v2
    return y


def env(t, a, d):
    return np.minimum(t / max(a, 1e-4), 1) * np.exp(-np.maximum(t - a, 0) / d)


def norm(x, peak=1.0):
    m = np.max(np.abs(x))
    return x * (peak / m) if m > 0 else x


def normalized(fn):
    def wrapper(*args, **kwargs):
        return norm(fn(*args, **kwargs))
    wrapper.__name__ = fn.__name__
    return wrapper


def P(t, a, b):
    return np.clip((t - a) / (b - a), 0, 1)


# ------------------------------------------------------------------ instruments
def saw_add(f0, t, fc, nh=16, phase=0.0):
    s = np.zeros_like(t)
    for k in range(1, nh + 1):
        fk = f0 * k
        if fk > SR / 2 - 2000:
            break
        s += (1 / k) / np.sqrt(1 + (fk / fc) ** 4) * np.sin(2 * np.pi * fk * t + phase * k)
    return s


def pad_chord(notes, d, fc, rel=0.6, att=0.35):
    t = tt(d + rel)
    L = np.zeros_like(t)
    R = np.zeros_like(t)
    for m in notes:
        f = mtof(m)
        for det, pan in ((-8, -0.7), (0, 0.0), (8, 0.7)):
            s = saw_add(f * 2 ** (det / 1200), t, fc, phase=rng.uniform(0, 2 * np.pi))
            a = (pan + 1) * np.pi / 4
            L += s * np.cos(a)
            R += s * np.sin(a)
    e = np.minimum(t / att, 1) * np.where(t > d, np.exp(-(t - d) / (rel / 2.5)), 1)
    return np.stack([L * e, R * e], 1)


@normalized
def pluck(f, d=0.45, bright=1.0):
    t = tt(d)
    s = sum((0.62 ** (k - 1)) * np.sin(2 * np.pi * f * k * t) * np.exp(-t * (8 + 6 * k / bright)) for k in range(1, 8))
    return s * np.minimum(t / 0.002, 1)


@normalized
def bell(f, d=2.5, decay=0.8):
    t = tt(d)
    parts = [(1, 1, 1), (2.0, 0.45, 0.6), (2.76, 0.3, 0.45), (5.4, 0.16, 0.25), (8.93, 0.08, 0.15)]
    s = sum(a * np.sin(2 * np.pi * f * r * t) * np.exp(-t / (decay * dd)) for r, a, dd in parts)
    return s * np.minimum(t / 0.002, 1)


@normalized
def bass_note(f, d=0.2):
    t = tt(d + 0.06)
    s = saw_add(f, t, 650, nh=12) * 0.7 + np.sin(2 * np.pi * f * t) * 0.9
    e = np.minimum(t / 0.004, 1) * np.where(t > d, np.exp(-(t - d) / 0.02), 1) * np.exp(-t / 0.4)
    return s * e


@normalized
def kick():
    t = tt(0.5)
    f = 46 + 125 * np.exp(-t / 0.032)
    s = np.sin(2 * np.pi * np.cumsum(f) / SR) * env(t, 0.0015, 0.2)
    c = fft_filter(rng.standard_normal(len(t)) * np.exp(-t / 0.0025), hp(1500)) * 0.35
    return np.tanh(1.6 * (s + c))


@normalized
def clap():
    t = tt(0.4)
    e = sum((t >= o) * np.exp(-np.maximum(t - o, 0) / 0.007) for o in (0, 0.011, 0.022))
    e = e + (t >= 0.03) * np.exp(-np.maximum(t - 0.03, 0) / 0.11) * 0.75
    return fft_filter(rng.standard_normal(len(t)) * e, bp(900, 4200))


@normalized
def hat(decay=0.03):
    t = tt(0.14)
    return fft_filter(rng.standard_normal(len(t)) * np.exp(-t / decay), hp(7500, 3))


@normalized
def crash(d=2.4):
    t = tt(d)
    n = rng.standard_normal((len(t), 2)) * (np.exp(-t / 0.7) * np.minimum(t / 0.003, 1))[:, None]
    return fft_filter(n, hp(3200, 2))


@normalized
def boom():
    t = tt(1.6)
    f = 33 + 46 * np.exp(-t / 0.12)
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * env(t, 0.004, 0.55)


@normalized
def snare(pitch=1.0):
    t = tt(0.2)
    tone = np.sin(2 * np.pi * np.cumsum(190 * pitch + 80 * np.exp(-t / 0.02)) / SR) * np.exp(-t / 0.05)
    n = fft_filter(rng.standard_normal(len(t)) * np.exp(-t / 0.06), bp(1200, 8000))
    return tone * 0.6 + n


# ------------------------------------------------------------------ sound effects
@normalized
def click():
    t = tt(0.07)
    s = np.sin(2 * np.pi * 3100 * t) * np.exp(-t / 0.005) + 0.6 * np.sin(2 * np.pi * 1650 * t) * np.exp(-t / 0.011)
    n = fft_filter(rng.standard_normal(len(t)) * np.exp(-t / 0.0025), hp(2500))
    return s * 0.7 + n * 0.6


@normalized
def tap(pitch=1.0):
    t = tt(0.09)
    s = np.sin(2 * np.pi * 820 * pitch * t) * np.exp(-t / 0.016) + 0.35 * np.sin(2 * np.pi * 2300 * pitch * t) * np.exp(-t / 0.007)
    n = fft_filter(rng.standard_normal(len(t)) * np.exp(-t / 0.0035), bp(1500, 7000)) * 0.45
    return s + n


@normalized
def keytap():
    p = rng.uniform(0.88, 1.15)
    out = tap(p * 1.25) * 0.7
    t = tt(len(out) / SR)
    return out + np.sin(2 * np.pi * 420 * p * t) * np.exp(-t / 0.012) * 0.5


@normalized
def tick():
    t = tt(0.05)
    return np.sin(2 * np.pi * 2100 * t) * np.exp(-t / 0.008) + 0.4 * np.sin(2 * np.pi * 4300 * t) * np.exp(-t / 0.004)


@normalized
def pop(f0=380, f1=1150, d=0.2):
    t = tt(d)
    f = f0 + (f1 - f0) * (1 - np.exp(-t / 0.025))
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * env(t, 0.002, 0.055)


@normalized
def whoosh(d=0.6, f0=250, f1=3500, q=0.9, peak=0.55, pan0=0.0, pan1=0.0):
    t = tt(d)
    x = t / d
    fc = f0 * (f1 / f0) ** x
    n = rng.standard_normal(len(t))
    s = svf(n, fc, q=q) * 0.6 + svf(n, fc * 0.5, q=0.6, mode="lp") * 0.4
    shape = np.where(x < peak, (x / peak) ** 2, ((1 - x) / (1 - peak)) ** 1.6)
    s = s * shape
    pan = pan0 + (pan1 - pan0) * x
    a = (pan + 1) * np.pi / 4
    return np.stack([s * np.cos(a), s * np.sin(a)], 1) * np.sqrt(2)


@normalized
def riser(d=1.5):
    t = tt(d)
    x = t / d
    n = rng.standard_normal(len(t))
    s = svf(n, 200 * (40 ** x), q=1.6) * (x ** 2.2)
    tone = np.sin(2 * np.pi * np.cumsum(220 * (4 ** x)) / SR) * (x ** 3) * 0.25
    return s + tone


def reverse_crash(d=0.9):
    return crash(d)[::-1] * np.linspace(0, 1, int(round(d * SR)))[:, None] ** 2


@normalized
def swell(d=0.8, f0=70, f1=170):
    t = tt(d)
    x = t / d
    f = f0 * (f1 / f0) ** x
    s = np.sin(2 * np.pi * np.cumsum(f) / SR) + 0.3 * np.sin(4 * np.pi * np.cumsum(f) / SR)
    n = svf(rng.standard_normal(len(t)), 300 + 2500 * x, q=0.7, mode="lp") * 0.5
    return (s + n) * np.sin(np.pi * x) ** 1.5


@normalized
def power_on():
    t = tt(0.5)
    f = 180 + 360 * (1 - np.exp(-t / 0.08))
    ph = 2 * np.pi * np.cumsum(f) / SR
    return (np.sin(ph) + 0.35 * np.sin(2 * ph) + 0.15 * np.sin(3 * ph)) * env(t, 0.01, 0.16)


@normalized
def chime():
    a = bell(mtof(84), 2.0, 0.6)
    b = bell(mtof(89), 2.0, 0.7)
    out = np.zeros(len(a) + int(0.13 * SR))
    out[:len(a)] += a * 0.8
    out[int(0.13 * SR):int(0.13 * SR) + len(b)] += b
    return out


@normalized
def blip(f):
    t = tt(0.12)
    return (np.sin(2 * np.pi * f * t) + 0.25 * np.sin(4 * np.pi * f * t)) * env(t, 0.002, 0.035)


@normalized
def ding():
    parts = [bell(mtof(m), 3.2, 1.1) for m in (89, 93, 96)]
    shimmer = bell(mtof(101), 3.2, 0.6) * 0.25
    return parts[0] + parts[1] * 0.7 + parts[2] * 0.55 + shimmer


@normalized
def glide(d, a, b):
    """Soft finger-on-glass rub that swells with the finger's speed (ease-in-out cubic)."""
    t = tt(d)
    x = P(t, 0, d)
    speed = np.where(x < 0.5, 12 * x * x, 12 * (1 - x) ** 2) / 3
    n = svf(rng.standard_normal(len(t)), 900 + 1800 * speed, q=0.7)
    s = n * (0.15 + speed) * np.minimum(t / 0.05, 1) * np.minimum((d - t) / 0.08, 1)
    pan = a + (b - a) * x
    ang = (pan + 1) * np.pi / 4
    return np.stack([s * np.cos(ang), s * np.sin(ang)], 1) * np.sqrt(2)


# ------------------------------------------------------------------ reverb
def make_ir(d=1.6, decay=0.42, tone=5000):
    t = tt(d)
    n = rng.standard_normal((len(t), 2)) * np.exp(-t / decay)[:, None]
    n[: int(0.012 * SR)] = 0
    ir = fft_filter(n, lp(tone, 1))
    return ir / np.sqrt(np.sum(ir ** 2, axis=0))  # unit energy: the wet return sits at the send level


def convolve(x, ir):
    n = len(x) + len(ir)
    nf = 1 << int(np.ceil(np.log2(n)))
    X = np.fft.rfft(x, nf, axis=0)
    Hh = np.fft.rfft(ir, nf, axis=0)
    return np.fft.irfft(X * Hh, nf, axis=0)[: len(x)]


# ------------------------------------------------------------------ arrangement
CH = {
    "Dm": [50, 57, 60, 65], "Bb": [46, 53, 57, 62], "F": [53, 57, 60, 64], "C": [48, 55, 62, 64],
}
ROOT = {"Dm": 38, "Bb": 34, "F": 41, "C": 36}
SEQ = [(0, 2, "Dm"), (2, 4, "Bb"), (4, 6, "F"), (6, 8, "C"), (8, 10, "Dm"), (10, 12, "Bb"), (12, 14, "F"),
       (14, 16, "C"), (16, 18, "Dm"), (18, 20, "Bb"), (20, 22, "F"), (22, 23, "C"), (23, 24, "Bb"), (24, 25, "C"),
       (25, 28, "F")]
DROP, BREAK, END = TL["logoLand"], TL["trustIn"], TL["endStart"]


def chord_at(t):
    for a, b, c in SEQ:
        if a <= t < b:
            return c
    return "F"


def build_music():
    pad = np.zeros((N, 2))
    pl = np.zeros((N, 2))
    bass = np.zeros((N, 2))
    drums = np.zeros((N, 2))
    bells = np.zeros((N, 2))

    for a, b, c in SEQ:
        fc, g = (650, 0.9) if a < DROP else (1300, 0.75) if BREAK <= a < END else (2900, 0.85) if a >= END else (2100, 0.7)
        rel = 2.2 if a >= END else 0.6
        place(pad, pad_chord(CH[c], b - a, fc, rel=rel, att=0.3 if a > 0 else 0.9), a, g)

    # hook: sparse plucks + quiet ticking hats
    for i, bt in enumerate(np.arange(0.5, DROP - 0.01, 0.5)):
        notes = [m + 12 for m in CH[chord_at(bt)]]
        place(pl, pluck(mtof(notes[[3, 1, 2, 0][i % 4]]), 0.6, 0.7), bt, 0.35, -0.3 if i % 2 else 0.3)
    for bt in np.arange(1.0, DROP - 0.01, 0.25):
        place(drums, hat(0.018), bt, 0.05)

    # main: 16th arpeggio from the zoom onwards
    pat = [0, 1, 2, 3, 2, 3, 1, 2]
    k = 0
    for st in np.arange(TL["zoomStart"] + 0.5, BREAK - 0.01, 0.125):
        notes = [m + 12 for m in CH[chord_at(st)]]
        lvl = 0.55 if st < TL["touchpadCaption"] else 1.0
        place(pl, pluck(mtof(notes[pat[k % 8]]), 0.4, 1.1), st, 0.2 * lvl, -0.35 if k % 2 else 0.35)
        k += 1

    # title bell motif on the drop (Fmaj7 arpeggio)
    for dt, m in ((0.0, 77), (0.25, 81), (0.5, 84), (0.75, 88)):
        place(bells, bell(mtof(m), 2.4, 0.7), DROP + dt, 0.35 if dt < 0.75 else 0.45, (dt - 0.4))

    # drums + bass
    kicks = np.arange(DROP, BREAK - 0.01, 0.5)
    for bt in kicks:
        place(drums, kick(), bt, 0.95)
        place(bass, bass_note(mtof(ROOT[chord_at(bt)])), bt + 0.25, 0.55)
    for bar in np.arange(8.0, BREAK - 0.01, 2.0):
        for off in (0.5, 1.5):
            place(drums, clap(), bar + off, 0.42)
    for bt in np.arange(TL["zoomStart"], BREAK - 0.01, 0.5):
        place(drums, hat(), bt + 0.25, 0.16)
        if bt >= TL["touchpadCaption"]:
            place(drums, hat(0.015), bt + 0.125, 0.06, -0.4)
            place(drums, hat(0.015), bt + 0.375, 0.06, 0.4)
    # break: snare roll + riser into the end card
    rolls = list(np.arange(END - 1.0, END - 0.5, 0.125)) + list(np.arange(END - 0.5, END - 0.01, 0.0625))
    for i, st in enumerate(rolls):
        place(drums, snare(1 + i * 0.03), st, 0.12 + 0.5 * (i / len(rolls)) ** 1.5)
    place(drums, kick(), END, 1.0)
    for hit in (DROP, END):
        place(drums, crash(), hit, 0.35)
        place(drums, boom(), hit, 0.9)
        place(drums, reverse_crash(0.9), hit - 0.9, 0.28)
    place(drums, stereo(riser(1.6)), DROP - 1.6, 0.30)
    place(drums, stereo(riser(1.0)), END - 1.0, 0.24)

    # sidechain pump while the kick plays
    tline = np.arange(N) / SR
    duck = np.ones(N)
    for bt in kicks:
        i0 = int(bt * SR)
        seg = tline[i0:i0 + int(0.45 * SR)] - bt
        duck[i0:i0 + len(seg)] = np.minimum(duck[i0:i0 + len(seg)], 1 - 0.55 * np.exp(-seg / 0.11))
    pad *= duck[:, None]
    pl *= (0.4 + 0.6 * duck)[:, None]
    bass *= duck[:, None]

    return {"pad": pad, "pluck": pl, "bass": bass, "drums": drums, "bells": bells}


# Target RMS (dBFS) per stem, measured where the stem plays: the groove for most, the title for bells.
MUSIC_TARGET = {"pad": (-21, 8.5, 23), "pluck": (-22, 8.5, 23), "bass": (-19, 8.5, 23), "drums": (-15.5, 8.5, 23), "bells": (-19, 4, 6)}
MUSIC_SEND = {"pad": 0.25, "pluck": 0.45, "bass": 0.0, "drums": 0.06, "bells": 0.6}


def mix_music(stems):
    for k, (db, a, b) in MUSIC_TARGET.items():
        seg = stems[k][int(a * SR):int(b * SR)]
        stems[k] = stems[k] * (10 ** (db / 20) / np.sqrt(np.mean(seg ** 2)))
    dry = sum(stems.values())
    wet = convolve(sum(stems[k] * MUSIC_SEND[k] for k in stems), make_ir())
    return dry + wet


def build_sfx():
    sfx = np.zeros((N, 2))
    T = TL
    place(sfx, whoosh(0.55, 300, 2500, pan0=0.8, pan1=0.4), T["hookMonitorIn"] - 0.05, 0.22)
    place(sfx, whoosh(0.5, 300, 2800, pan0=-0.8, pan1=-0.4), T["hookPhoneIn"] - 0.08, 0.22)
    place(sfx, pop(300, 900), T["hookPhoneIn"] + 0.32, 0.2, -0.45)
    place(sfx, whoosh(0.75, 600, 4500, q=1.4, peak=0.8, pan0=-0.5, pan1=0.5), T["hookLine"], 0.1)
    place(sfx, tick(), T["hookSnap"], 0.2)
    place(sfx, whoosh(0.7, 200, 3000, peak=0.85), T["hookSnap"] + 0.05, 0.3)
    place(sfx, whoosh(0.7, 2500, 250, peak=0.35), T["zoomStart"] - 0.05, 0.28)
    place(sfx, power_on(), T["screenOn"], 0.18)
    place(sfx, pop(420, 1000), T["screenOn"] + 0.12, 0.14)
    place(sfx, click(), T["approveClick"], 0.38, -0.1)
    place(sfx, chime(), T["connected"], 0.3, 0.25)
    place(sfx, swell(0.6, 90, 200), T["appIn"] - 0.05, 0.12)
    place(sfx, glide(T["fingerUp"] - T["fingerDown"], 0.5, 0.0), T["fingerDown"], 0.12)
    place(sfx, tap(), T["searchClick"] - 0.01, 0.3, 0.4)
    place(sfx, click(), T["searchClick"] + 0.01, 0.32, -0.2)
    place(sfx, tick(), T["kbTab"], 0.2, 0.45)
    place(sfx, tap(), T["textareaTap"], 0.28, 0.45)
    place(sfx, whoosh(0.3, 400, 2500, peak=0.6, pan0=0.45, pan1=0.45), T["textareaTap"] + 0.04, 0.08)
    for i, _ in enumerate(T["typed"]):
        place(sfx, keytap(), T["typeStart"] + i * T["typeStep"], 0.3, 0.45)
    place(sfx, tap(1.1), T["sendText"], 0.3, 0.45)
    place(sfx, whoosh(0.42, 500, 5000, q=1.2, peak=0.7, pan0=0.45, pan1=-0.3), T["sendText"] + 0.02, 0.26)
    place(sfx, pop(520, 1400), T["searchFilled"], 0.2, -0.2)
    place(sfx, tick(), T["mediaTab"], 0.2, 0.45)
    place(sfx, tap(), T["playTap"], 0.3, 0.45)
    place(sfx, swell(0.7, 70, 190), T["playTap"] + 0.02, 0.25, -0.1)
    for i, at in enumerate(T["volTaps"]):
        place(sfx, tap(1.05), at, 0.22, 0.45)
        place(sfx, blip(660 * 2 ** (i * 4 / 12)), at + 0.02, 0.22, -0.1)
    place(sfx, tick(), T["airTab"], 0.2, 0.45)
    place(sfx, tap(0.95), T["airSeg"], 0.25, 0.45)
    place(sfx, blip(990), T["airSeg"] + 0.04, 0.12, 0.45)
    place(sfx, tap(0.9), T["airHold"], 0.22, 0.45)
    # pointing: a soft airy sweep that follows the cursor across the screen
    place(sfx, whoosh(T["airSweepEnd"] - T["airSweepStart"] + 0.2, 300, 1600, q=0.9, peak=0.5, pan0=-0.2, pan1=0.6), T["airSweepStart"], 0.16)
    hum_t = tt(T["airClick"] - T["airHold"])
    hum = (np.sin(2 * np.pi * 174 * hum_t) + 0.5 * np.sin(2 * np.pi * 261 * hum_t)) * (0.7 + 0.3 * np.sin(2 * np.pi * 5 * hum_t))
    hum *= np.minimum(hum_t / 0.2, 1) * np.minimum((hum_t[-1] - hum_t) / 0.15, 1)
    place(sfx, norm(hum), T["airHold"], 0.035, 0.2)
    place(sfx, tap(), T["airClick"], 0.28, 0.45)
    place(sfx, click(), T["airClick"] + 0.01, 0.34, 0.2)
    place(sfx, swell(0.75, 60, 220), T["fullscreen"], 0.28)
    place(sfx, whoosh(0.6, 3000, 200, peak=0.3), T["trustIn"] - 0.05, 0.18)
    for i, at in enumerate(T["chips"]):
        place(sfx, pop(340 * 2 ** (i * 3 / 12), 1000 * 2 ** (i * 3 / 12)), at, 0.22, (i - 1) * 0.5)
    place(sfx, whoosh(0.6, 200, 3500, peak=0.9), T["endStart"] - 0.55, 0.24)
    place(sfx, ding(), T["endLogo"], 0.36)
    place(sfx, pop(500, 1300), T["endTitle"], 0.1)
    place(sfx, pop(600, 1500), T["endUrl"], 0.13, 0.1)
    ir = make_ir(1.2, 0.3)
    return sfx + convolve(sfx, ir) * 0.3


def main():
    out = HERE / (sys.argv[1] if len(sys.argv) > 1 else "soundtrack.wav")
    music = mix_music(build_music())
    sfx = build_sfx()
    mix = music + sfx * 1.6  # UI sounds sit clearly on top of the bed
    tline = np.arange(N) / SR
    mix *= np.minimum(tline / 0.03, 1)[:, None]
    fade = np.clip((DUR - tline) / 1.1, 0, 1)
    mix *= (0.5 - 0.5 * np.cos(np.pi * fade))[:, None]
    # gentle soft clip, then -1 dBFS peak
    # peak-normalise, then a gentle soft clip that only rounds the loudest transients, then -1 dBFS
    mix = np.tanh(mix / np.max(np.abs(mix)) * 1.3)
    mix *= 0.891 / np.max(np.abs(mix))
    dither = (rng.random((N, 2)) - rng.random((N, 2))) / 32768
    pcm = np.clip(np.round((mix + dither) * 32767), -32768, 32767).astype("<i2")
    with wave.open(str(out), "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    peak_db = 20 * np.log10(np.max(np.abs(pcm)) / 32768)
    rms_db = 20 * np.log10(np.sqrt(np.mean((pcm / 32768.0) ** 2)))
    print(f"wrote {out.name}: {len(pcm) / SR:.3f} s, peak {peak_db:.2f} dBFS, rms {rms_db:.1f} dBFS")


if __name__ == "__main__":
    main()
