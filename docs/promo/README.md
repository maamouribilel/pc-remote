# PC Remote promo video

A 28-second animated ad in two cuts, with the same soundtrack and timing. Both are 30 fps, H.264 video with AAC stereo audio.

| Cut | For | Size | SHA-256 |
| :--- | :--- | :--- | :--- |
| [**16:9**, 1920 × 1080](pc-remote-ad-28s.mp4) | YouTube, X, LinkedIn, GitHub | 30.8 MB | `e01744571c19ab63af29f87121c598df5fac23d27c5278e4e197426157f77f1e` |
| [**9:16**, 1080 × 1920](pc-remote-ad-28s-vertical.mp4) | Reels, TikTok, Shorts, Stories | 34.5 MB | `a2a5fe89ebc13c474a2ff57c470c6053dec86f6b576d716014f71bab24e708f5` |

Made on 2026-10-15 from source baseline `d96c61e` (version 0.4.2).

## Story

| Time | Scene |
| :--- | :--- |
| 0–3.5 s | "Your computer is over there. You're over here." |
| 3.5–6 s | The two icons snap into the PC Remote logo. |
| 6.5 s | Pair: the phone is approved on the computer. |
| 8.5 s | Touchpad: glide and tap to the search box. |
| 11.5 s | Keyboard: types "Étoiles 🚀", then Send text. |
| 15 s | Media: play, then volume up. |
| 19 s | Air pointer: hold the pad, turn the wrist, click fullscreen. |
| 23 s | Privacy: stays on your network, approved on your computer, encrypted, no account. |
| 25 s | End card with the repository URL. |

## An illustration, not a capture

- The monitor, the phone and everything on their screens are **redrawn** with the remote's color tokens: the remote's tabs, the computer's approval prompt, the phone's on-screen keyboard and the desktop app. Layouts and labels follow the 0.4.2 remote but are simplified. For example, the keyboard tab shows only Ctrl/Alt/Shift, the media tab has no player profile, and the approval prompt is not the companion's actual dialog.
- The "Cinema" app, the film "Étoiles", the address 192.168.1.42 and the volume levels are fictional.
- It demonstrates no device, browser or platform behavior and is not release evidence. README demo media remains the real captures in [`docs/assets/`](../assets/README.md).
- All music and sound effects are synthesized by [`source/audio.py`](source/audio.py); no samples, stock footage or third-party logos are used.
- The on-screen claims follow the [support matrix](../../README.md#supported-environments): "Windows, plus macOS and Ubuntu previews", "Runs in your phone's browser" (no phone or browser named), "Free & open source". No version number appears, so the video does not go stale with the next release.

## Rebuild

`source/` generates both files. Every frame is a pure function of time in `ad.html`, and `timeline.json` holds every cue, so the sound effects land on the frames they belong to.

| File | Role |
| :--- | :--- |
| `timeline.json` | Cue times read by both the animation and the soundtrack |
| `ad.html` | Canvas animation, `renderAt(t)`; a portrait timeline, or `?vertical` in a live preview, switches to the 9:16 layout |
| `capture.mjs` | Renders frames in headless Chromium with the Playwright installed by `src/remote` (`npm ci` there first); `--vertical` renders 1080 × 1920 |
| `audio.py` | 120 BPM music bed and UI sound effects, built with NumPy into a 48 kHz WAV |
| `encode.swift` | AVFoundation muxer: PNG frames + WAV → MP4, no ffmpeg needed |
| `build.sh` | Runs everything: `docs/promo/source/build.sh [all\|landscape\|vertical] [workDir]` |

Rebuilding requires macOS with the Xcode Command Line Tools, Node, Python 3 and NumPy. Each cut takes about 5 minutes. To check a few moments without a full render, run `node docs/promo/source/capture.mjs /tmp/review 4.9,12.9,20.8 [--vertical]`. Frames, the WAV and the encoder binary stay in the work directory. Update the claims here, and in the video, whenever the support matrix changes.
