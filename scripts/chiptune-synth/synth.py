#!/usr/bin/env python3
"""
chiptune-synth — stdlib-only sin/cos additive synthesizer for retro SFX.

Generates 16-bit PCM WAV from sin/cos oscillators with harmonic mixing,
ADSR envelope, vibrato, detune layering, and optional bit-crush/
downsample for NES/Famicom-era grit.

Presets: piano, pad, lead, bass.

Usage:
    python synth.py piano C4 --duration 1.5 --out piano.wav
    python synth.py lead --sequence "C4 E4 G4 C5" --tempo 140 --out riff.wav

No external dependencies.
"""

import argparse
import copy
import math
import struct
import wave
from dataclasses import dataclass, field
from typing import List, Tuple

SAMPLE_RATE = 44100
TWO_PI = 2.0 * math.pi

# ---------- note -> frequency --------------------------------------------

_NOTE_OFFSETS = {
    "C": 0, "C#": 1, "DB": 1,
    "D": 2, "D#": 3, "EB": 3,
    "E": 4,
    "F": 5, "F#": 6, "GB": 6,
    "G": 7, "G#": 8, "AB": 8,
    "A": 9, "A#": 10, "BB": 10,
    "B": 11,
}


def note_to_freq(note: str) -> float:
    """Parse scientific pitch notation ('C4', 'A#3', 'Bb5') to Hz. A4=440."""
    n = note.strip().upper()
    if len(n) < 2 or not n[0].isalpha():
        raise ValueError(f"bad note: {note!r}")
    i = 1
    if i < len(n) and n[i] in ("#", "B"):
        i += 1
    letter = n[:i]
    if letter not in _NOTE_OFFSETS:
        raise ValueError(f"bad note letter: {letter!r}")
    try:
        octave = int(n[i:])
    except ValueError:
        raise ValueError(f"bad octave in {note!r}")
    midi = (octave + 1) * 12 + _NOTE_OFFSETS[letter]
    return 440.0 * 2.0 ** ((midi - 69) / 12.0)


# ---------- preset --------------------------------------------------------

@dataclass
class Preset:
    name: str
    harmonics: List[Tuple[int, float]]          # (n-th harmonic, relative amp)
    attack: float
    decay: float
    sustain: float                               # 0..1
    release: float
    detune_cents: List[float] = field(default_factory=lambda: [0.0])
    vibrato_rate: float = 0.0                    # Hz (0 = off)
    vibrato_depth: float = 0.0                   # cents peak
    vibrato_onset: float = 0.0                   # s before vibrato starts
    pitch_env_cents: float = 0.0                 # initial pitch offset
    pitch_env_time: float = 0.0                  # s to decay pitch offset to 0
    gain: float = 0.8


PRESETS = {
    # bright pluck: harmonics decay ~1/n^1.5, tiny pitch dip on attack like a hammered key
    "piano": Preset(
        name="piano",
        harmonics=[(1, 1.0), (2, 0.5), (3, 0.3), (4, 0.2), (5, 0.13), (6, 0.08), (7, 0.05)],
        attack=0.005, decay=0.35, sustain=0.15, release=0.30,
        pitch_env_cents=18.0, pitch_env_time=0.02,
        gain=0.85,
    ),
    # soft wash: few harmonics, 3-layer chorus via detune, gentle vibrato after onset
    "pad": Preset(
        name="pad",
        harmonics=[(1, 1.0), (2, 0.25), (3, 0.08)],
        attack=0.35, decay=0.25, sustain=0.75, release=1.20,
        detune_cents=[0.0, -7.0, 7.0],
        vibrato_rate=5.0, vibrato_depth=8.0, vibrato_onset=0.25,
        gain=0.55,
    ),
    # square/pulse-wave approximation via odd harmonics (1/n), NES pulse channel vibe
    "lead": Preset(
        name="lead",
        harmonics=[(1, 1.0), (3, 1 / 3), (5, 1 / 5), (7, 1 / 7), (9, 1 / 9), (11, 1 / 11)],
        attack=0.01, decay=0.08, sustain=0.65, release=0.18,
        vibrato_rate=6.5, vibrato_depth=12.0, vibrato_onset=0.15,
        gain=0.65,
    ),
    # thick low: strong fundamental + octave + weak 3rd, punchy envelope
    "bass": Preset(
        name="bass",
        harmonics=[(1, 1.0), (2, 0.55), (3, 0.22), (4, 0.10)],
        attack=0.005, decay=0.12, sustain=0.50, release=0.12,
        gain=0.95,
    ),
    # warm ambient: bright airy top from 4th harmonic, 5-layer wide chorus
    # with ±20¢ spread for lush drift, slower breathing than `pad`
    "ambient": Preset(
        name="ambient",
        harmonics=[(1, 1.0), (2, 0.22), (3, 0.10), (4, 0.07)],
        attack=1.5, decay=0.6, sustain=0.85, release=3.0,
        detune_cents=[-20.0, -10.0, 0.0, 10.0, 20.0],
        vibrato_rate=0.25, vibrato_depth=7.0, vibrato_onset=1.0,
        gain=0.55,
    ),
}


# ---------- envelope & rendering -----------------------------------------

def adsr(n_samples: int, sr: int, a: float, d: float, s: float, r: float) -> List[float]:
    env = [0.0] * n_samples
    a_n = max(1, int(a * sr))
    d_n = max(1, int(d * sr))
    r_n = max(1, int(r * sr))
    sustain_end = max(a_n + d_n, n_samples - r_n)
    for i in range(n_samples):
        if i < a_n:
            env[i] = i / a_n
        elif i < a_n + d_n:
            t = (i - a_n) / d_n
            env[i] = 1.0 + (s - 1.0) * t
        elif i < sustain_end:
            env[i] = s
        else:
            t = (i - sustain_end) / r_n
            env[i] = s * max(0.0, 1.0 - t)
    return env


def render_note(preset: Preset, freq: float, duration: float, sr: int = SAMPLE_RATE) -> List[float]:
    n = int(duration * sr)
    if n <= 0:
        return []
    env = adsr(n, sr, preset.attack, preset.decay, preset.sustain, preset.release)

    total_amp = sum(a for _, a in preset.harmonics) or 1.0
    harmonics = [(h, a / total_amp) for h, a in preset.harmonics]

    samples = [0.0] * n
    layers = max(1, len(preset.detune_cents))

    for detune in preset.detune_cents:
        det_mult = 2.0 ** (detune / 1200.0)
        for i in range(n):
            t = i / sr
            vib = 0.0
            if preset.vibrato_rate > 0 and t >= preset.vibrato_onset:
                vib = preset.vibrato_depth * math.sin(
                    TWO_PI * preset.vibrato_rate * (t - preset.vibrato_onset)
                )
            if preset.pitch_env_time > 0 and t < preset.pitch_env_time:
                pe = preset.pitch_env_cents * (1.0 - t / preset.pitch_env_time)
            else:
                pe = 0.0
            f = freq * det_mult * 2.0 ** ((vib + pe) / 1200.0)
            phase_base = TWO_PI * f * t
            s = 0.0
            for h, amp in harmonics:
                s += amp * math.sin(h * phase_base)
            samples[i] += s

    for i in range(n):
        samples[i] = samples[i] / layers * env[i] * preset.gain
    return samples


def render_sequence(preset: Preset, notes: List[str], tempo_bpm: float,
                    sr: int = SAMPLE_RATE) -> List[float]:
    """Play one note per beat. Release tail of each note bleeds into the next."""
    beat_sec = 60.0 / tempo_bpm
    n_per_beat = int(beat_sec * sr)
    tail_sec = preset.release + 0.05
    n_tail = int(tail_sec * sr)
    total = n_per_beat * len(notes) + n_tail
    out = [0.0] * total
    for idx, note in enumerate(notes):
        freq = note_to_freq(note)
        wav = render_note(preset, freq, beat_sec + preset.release, sr)
        start = idx * n_per_beat
        for i, v in enumerate(wav):
            if start + i < total:
                out[start + i] += v
    return out


# ---------- retro colour --------------------------------------------------

def bitcrush(samples: List[float], bits: int) -> List[float]:
    steps = 2 ** (bits - 1)
    return [round(s * steps) / steps for s in samples]


def lofi_downsample(samples: List[float], sr_in: int, sr_out: int) -> List[float]:
    """Sample-and-hold to emulate low-rate DAC aliasing."""
    if sr_out >= sr_in or sr_out <= 0:
        return samples
    stride = max(1, int(sr_in / sr_out))
    out = [0.0] * len(samples)
    held = 0.0
    for i in range(len(samples)):
        if i % stride == 0:
            held = samples[i]
        out[i] = held
    return out


# ---------- WAV I/O -------------------------------------------------------

def write_wav(path: str, samples: List[float], sr: int = SAMPLE_RATE) -> None:
    if not samples:
        raise ValueError("no samples to write")
    peak = max(abs(s) for s in samples) or 1.0
    if peak > 1.0:
        samples = [s / peak for s in samples]
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        frames = b"".join(
            struct.pack("<h", max(-32768, min(32767, int(s * 32767)))) for s in samples
        )
        w.writeframes(frames)


# ---------- CLI -----------------------------------------------------------

def main() -> None:
    ap = argparse.ArgumentParser(
        description="Chiptune-style sin/cos additive synthesizer.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    ap.add_argument("preset", choices=sorted(PRESETS.keys()))
    ap.add_argument("note", nargs="?", help="Pitch like C4 / A#3 / Bb5. Omit if --sequence.")
    ap.add_argument("--duration", type=float, default=1.0, help="Seconds (single-note mode).")
    ap.add_argument("--sequence", type=str, default=None,
                    help='Space-separated notes, e.g. "C4 E4 G4 C5".')
    ap.add_argument("--tempo", type=float, default=120.0, help="BPM for --sequence.")
    ap.add_argument("--out", "-o", required=True, help="Output WAV path.")
    ap.add_argument("--attack", type=float, help="Override ADSR attack (s).")
    ap.add_argument("--decay", type=float, help="Override ADSR decay (s).")
    ap.add_argument("--sustain", type=float, help="Override ADSR sustain (0..1).")
    ap.add_argument("--release", type=float, help="Override ADSR release (s).")
    ap.add_argument("--gain", type=float, help="Override master gain (0..1).")
    ap.add_argument("--vibrato-rate", type=float, help="Override vibrato rate (Hz).")
    ap.add_argument("--vibrato-depth", type=float, help="Override vibrato depth (cents).")
    ap.add_argument("--detune", type=str,
                    help='Override detune layers, comma cents, e.g. "-10,0,10".')
    ap.add_argument("--bitcrush", type=int, default=0,
                    help="Reduce to N-bit depth (e.g. 4 or 8). 0 = off.")
    ap.add_argument("--sr", type=int, default=SAMPLE_RATE, help="Output sample rate (Hz).")
    ap.add_argument("--lofi-sr", type=int, default=0,
                    help="Sample-and-hold downsample to this rate (0 = off).")
    ap.add_argument("--layer", action="append", default=[],
                    help='Layer another preset on top. Format '
                         '"preset:notes:tempo:gain", e.g. '
                         '"bell:C5 E5 G5 C6:80:0.22". Repeatable.')
    args = ap.parse_args()

    p = copy.deepcopy(PRESETS[args.preset])
    if args.attack is not None: p.attack = args.attack
    if args.decay is not None: p.decay = args.decay
    if args.sustain is not None: p.sustain = args.sustain
    if args.release is not None: p.release = args.release
    if args.gain is not None: p.gain = args.gain
    if args.vibrato_rate is not None: p.vibrato_rate = args.vibrato_rate
    if args.vibrato_depth is not None: p.vibrato_depth = args.vibrato_depth
    if args.detune is not None:
        p.detune_cents = [float(x) for x in args.detune.split(",")]

    if args.sequence:
        samples = render_sequence(p, args.sequence.split(), args.tempo, args.sr)
    else:
        if not args.note:
            ap.error("provide a note (e.g. C4) or use --sequence")
        samples = render_note(p, note_to_freq(args.note), args.duration, args.sr)

    for spec in args.layer:
        parts = spec.split(":")
        if len(parts) != 4:
            ap.error(f'--layer expects "preset:notes:tempo:gain", got {spec!r}')
        l_name, l_notes, l_tempo, l_gain = parts
        if l_name not in PRESETS:
            ap.error(f"--layer unknown preset {l_name!r}")
        layer_samples = render_sequence(
            PRESETS[l_name], l_notes.split(), float(l_tempo), args.sr
        )
        gain = float(l_gain)
        if len(layer_samples) > len(samples):
            samples.extend([0.0] * (len(layer_samples) - len(samples)))
        for i, v in enumerate(layer_samples):
            samples[i] += v * gain

    if args.lofi_sr > 0:
        samples = lofi_downsample(samples, args.sr, args.lofi_sr)
    if args.bitcrush > 0:
        samples = bitcrush(samples, args.bitcrush)

    write_wav(args.out, samples, args.sr)
    print(f"wrote {args.out} ({len(samples) / args.sr:.2f}s @ {args.sr}Hz, preset={p.name})")


if __name__ == "__main__":
    main()
