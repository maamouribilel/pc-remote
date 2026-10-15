#!/bin/zsh
# Rebuilds the promo from source: frames (Chromium) + soundtrack (numpy) -> MP4 (AVFoundation).
# Usage: docs/promo/source/build.sh [all|landscape|vertical] [workDir]
#   landscape -> docs/promo/pc-remote-ad-28s.mp4 (1920×1080), vertical -> docs/promo/pc-remote-ad-28s-vertical.mp4 (1080×1920)
#   Frames, the soundtrack WAV and the encoder binary go to workDir (default: a temp dir). Each format takes about 5 minutes.
set -euo pipefail
here=${0:A:h}
out=${here:h}
mode=${1:-all}
work=${2:-$(mktemp -d)}
mkdir -p "$work"
fps=$(python3 -c "import json;print(json.load(open('$here/timeline.json'))['fps'])")

python3 "$here/audio.py" "$work/soundtrack.wav"
swiftc -O "$here/encode.swift" -o "$work/encode" 2>/dev/null

render() {  # render <frames subdir> <output mp4> [--vertical]
  rm -rf "$work/$1"
  node "$here/capture.mjs" "$work/$1" ${3:-}
  "$work/encode" "$work/$1" "$fps" "$work/soundtrack.wav" "$out/$2"
}
case $mode in
  all|landscape) render frames pc-remote-ad-28s.mp4 ;|
  all|vertical) render frames-vertical pc-remote-ad-28s-vertical.mp4 --vertical ;;
  landscape) ;;
  *) echo "usage: ./build.sh [all|landscape|vertical] [workDir]" >&2; exit 2 ;;
esac
