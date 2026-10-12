# Milestone 4 — media profiles

Implemented General, YouTube/browser and VLC profiles, persistent PC/phone selection, play/pause, next/previous, volume and mute. App profiles add seek/fullscreen. General hides those unsupported actions. Profile commands carry their displayed profile explicitly, so a concurrent PC setting change cannot silently change their shortcut meaning. Phone learns PC selection through status/pong. Changing settings requires an authenticated current owner and exact Origin.

General uses Windows media virtual keys and system volume. YouTube maps K, Shift+N/P, arrows for volume, M, J/L and F. Previous requires a playlist; volume arrows require the player/seek bar focus. VLC maps Space, N/P, Ctrl+Up/Down, M, Shift+Left/Right and F. Focus the matching PC player and keep default hotkeys. Seek labels intentionally carry no duration because app preferences can change it.

Mappings verified against primary sources: [YouTube keyboard shortcuts](https://support.google.com/youtube/answer/7631406?hl=en), [VideoLAN desktop 3.0 hotkeys](https://docs.videolan.me/vlc-user/desktop/3.0/en/basic/hotkeys.html), [Windows virtual-key codes](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes). This verifies documented defaults, not actual playback. Installed VLC reports 3.0.24.

Release build passes with zero warnings/errors and strict TypeScript. `scripts/test.ps1 -KeyboardSmoke` passes 114 checks, including all declarative mappings, unsupported-action rejection, profile persistence, media rate bound, and actual SendInput K / Shift+Left sequences in a disposable field. The native test avoids global volume/playback changes and cannot claim VLC/YouTube app behavior.

Browser WSS suite passes phone profile change/persistence, profile-specific command dispatch, hiding General seek/fullscreen and conditional playlist labels, alongside keyboard/mouse/cleanup regressions. Its recording adapter and controlled approval are test-only; TLS bypass remains UI evidence only. A cleanup check initially counted a delayed pre-loss release. It now waits for the held modifier to reach the adapter and observes a disconnected state before restoring network; the complete flow passes.

Physical app acceptance pending: refresh the iPhone, scroll to Media, choose YouTube/browser and focus a PC YouTube player. Test play/pause, seek both directions, fullscreen toggle, mute/volume and next/previous in a playlist. Choose VLC and focus VLC with a local video/playlist; repeat the actions. Choose General with an active media session and test play/pause and system volume/mute; seek/fullscreen should be hidden. Record unsupported player behavior or customized hotkeys explicitly. The companion uses real SendInput throughout; no media simulation or arbitrary scripts.

Command rate is capped at ten media actions/sec in addition to the 180-message socket cap. Profiles/chords are declarative allowlists. Native batches check inserted-event counts and release partial held keys on failure. Monotonic time now drives held-input leases so clock adjustment does not extend a hold.

## Physical follow-up and Stremio addition — 2026-10-11

User confirms YouTube and VLC actions work from the actual iPhone. The General check received a Stremio feature request rather than a result; General remains unverified on the actual phone.

Installed Stremio is 5.0.26. Added a Stremio profile using Space (play/pause), Shift+N (next episode), Up/Down (player volume), M (mute), Left/Right (seek), F (fullscreen). Previous is hidden and rejected because no supported previous-episode shortcut is published. Seek labels remain duration-free. Focus the built-in player; external players require their matching profile. Existing Navigation, Keyboard and Touchpad remain available for browsing and search.

Primary references: [Stremio Help Center shortcuts](https://stremio.zendesk.com/hc/en-us/articles/360022892811-Shortcuts-in-stremio), [Stremio shortcut source at inspected commit](https://github.com/Stremio/stremio-web/blob/21c8a53b026a85745e60203d94786835782277f4/src/common/Shortcuts/shortcuts.json). The source documents M for mute and the next-episode chord. Documentation/source evidence does not establish actual installed-player behavior.

Updated Release build has zero warnings/errors. Pairing integration passes 136 checks; native keyboard smoke passes 126, including actual Shift+N and Left with modifier cleanup in the disposable field. The first native attempt stopped safely on focus loss and was rerun successfully with the guard unchanged. LAN transport passes 126 checks with certificate chain/IP validation. Browser WSS flow passes Stremio persistence after reload, mapped command dispatch and hiding Previous, alongside existing media/keyboard/mouse cleanup checks. Arrow-pad geometry was corrected, tested and visually inspected. Browser TLS bypass/recording input remain test-only limitations.

Final physical result: user explicitly confirms all Stremio actions and General actions work. Milestone 4 acceptance passes. The earlier pending notes above are historical evidence; Milestone 5 subsequently passes actual-device/tray checks.
