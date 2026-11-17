# Nuvio Desktop controls

**Unreleased source change after PC Remote 0.4.2 (2026-11-17).** Published 0.4.2 downloads do not include this profile. Windows remains primary; macOS and Ubuntu remain experimental.

Select **Media → Nuvio Desktop** on the phone, then click the video in official Nuvio Desktop on the computer. You can also add Nuvio's local executable/app in the companion's app launcher and choose its **Nuvio** media profile. Opening that configured app selects the profile. Existing launcher entries can be removed and re-added with that profile; Nuvio is not automatically discovered.

The controls use the official **0.1.37-alpha** built-in player's default shortcuts:

| Control | Shortcut | Behavior |
| --- | --- | --- |
| Play / pause | K | Toggle playback |
| Seek back / forward | Left / Right | Nuvio chooses the seek amount |
| Volume down / up; mute | Down / Up; M | Nuvio player volume; the separate system slider controls the OS output |
| Fullscreen | F | Toggle player fullscreen |
| Next episode | Shift+N | Requires an available next episode |
| Skip intro / recap | Enter | Skip Nuvio's current detected segment while its skip prompt is available |
| Subtitles | S | Open/close subtitles |
| Audio | A | Open/close audio tracks |
| Sources | Q | Open/close sources |
| Episodes | E | Open/close episodes |
| Picture in picture | P | Toggle picture in picture where Nuvio supports it |

Intro and recap share one contextual shortcut, so the remote has one **Skip intro / recap** button. Nuvio supplies segment detection and timing; PC Remote does not guess a duration, fetch metadata or inspect playback state. It cannot know which prompt is showing or disable Next/Skip according to the current video. Missing skip data means no detected segment to skip. Close player panels and focus the video first: Enter can activate a focused control instead of skipping. Use **Navigation** or **Touchpad** to choose items inside panels and **Esc** to close them. Previous episode has no mapped shortcut.

This profile targets the built-in player. If Nuvio opens VLC or another external player, select that player's matching profile. Older Nuvio versions, other forks, customized shortcuts and keyboard layouts may behave differently. macOS additionally checks the frontmost bundle is `com.nuvio.media.desktop`; the Nuvio main window must be focused. On Windows/Ubuntu, explicitly focus the intended player before sending shortcuts. Nuvio controls are available only when the companion advertises the corresponding actions; older companions keep their existing profiles.

## Source and acceptance

Shortcuts were inspected on 2026-11-17 in official [Nuvio Desktop 0.1.37-alpha controls.js](https://github.com/NuvioMedia/NuvioDesktop/blob/0.1.37-alpha/composeApp/src/desktopMain/resources/player-ui/controls.js) (release revision `3020193d8303c38ce88381f0a2a613b389219e62`). The [same release's packaging configuration](https://github.com/NuvioMedia/NuvioDesktop/blob/0.1.37-alpha/composeApp/build.gradle.kts) declares the macOS bundle ID. No Nuvio source, runtime or dependency is bundled into PC Remote.

Protocol allowlist, capability denial, settings persistence, shortcut mapping/cleanup and browser fixture checks establish PC Remote's dispatch behavior only. Actual installed Nuvio playback, intro/recap detection, episode transitions, panels, PiP, alternate layouts and physical-phone/LAN use require separate acceptance on each OS. Recording input cannot prove these player actions executed.
