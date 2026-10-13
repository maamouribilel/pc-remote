# Pairing and control transport — version 1

Current stage: authenticated mouse, keyboard/navigation and media control (`mobile`). `/control/ws` accepts the allowlisted messages below; `/diagnostics/ws` remains ping-only.

| Type | Exact payload | Validation |
| --- | --- | --- |
| `pointer.move` | `{dx,dy}` | Finite numbers, each ±500; PC sensitivity 0.25–4 and fractional accumulation. |
| `pointer.scroll` | `{dx,dy}` | Finite numbers, each ±1200 Windows wheel units; PC natural-scroll setting. |
| `pointer.button` | `{button,action}` | `left`/`right`; `down`/`up`/`click`/`double`. |
| `input.releaseAll` | `{}` | Release remote-owned keys/buttons and reset residual motion. |
| `ping` | `{}` | Refresh lease and return pong. |

Same version/sequence/exact-field/1024-byte/depth validation applies. Authenticated sockets accept 180 messages/second; diagnostics remain four/second. Only text has an individual acknowledgement; unknown commands fail. Input failures close with `input_failed` (1008); invalid messages with 1007 and rate excess with 1008. `inputEnabled` is true; authentication and pause still apply.

Real SendInput checks native INPUT size (40 bytes x64, 28 x86) and inserted-event count. Failed release pauses control and prevents takeover; cleanup retries. Ownership loss, pause, revoke, timeout, disconnect, malformed messages, rate excess, replacement and exit release input.

During drag the phone heartbeats approximately every 250 ms. A 1200 ms held-input lease is inspected every 100 ms; expiry releases and ends ownership. Scheduling can add delay. Normal heartbeat remains five seconds. Page hiding sends releaseAll and closes. Reconnect resets drag without replay.

Motion is aggregated per animation frame with one bounded aggregate under backpressure. Button transitions flush motion; unsendable transitions close for cleanup. One finger moves, short low-distance taps click, two fingers scroll without taps, three fingers cancel. Right/double click and explicit drag have buttons. Touch-action suppression applies only to the pad.

Native references: [SendInput/integrity restrictions](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput), [INPUT layout](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-input), [relative movement/wheel units](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mouseinput).

## Keyboard extension (Milestone 3)

| Type | Exact payload | Allowed values |
| --- | --- | --- |
| `text.insert` | `{text}` | 1–256 UTF-16 units, valid surrogate pairs; controls only newline/CR/tab. Also bounded by the 1024-byte frame limit. |
| `key.down`, `key.up`, `key.press` | `{key}` | Enter, Backspace, Escape, Tab, Control, Alt, Shift, Windows, ArrowLeft/Right/Up/Down, Home, End, PageUp, PageDown. |
| `shortcut` | `{action}` | copy, paste, selectAll, undo, switchApp. Declarative Ctrl+C/V/A/Z and Alt+Tab. |

Unicode insertion uses KEYEVENTF_UNICODE down/up for each UTF-16 unit (including surrogate pairs), with CRLF normalized and newlines inserted using an Enter pair. Physical keys use allowlisted virtual keys and extended flags where required. Text and shortcut batches reject held keyboard keys; the phone explicitly releases held input and visibly resets modifier state first. Ctrl+V uses the PC's clipboard; no clipboard transfer/read endpoint exists.

Text rate is at most four insertions/second. Replayed sequences fail before injection. Accepted text returns `ack` with the version, sequence, session and empty payload; it contains no typed text. The phone disables Send during composition and while waiting for ack, clears its field on acknowledgement, and never automatically resends. An ack timeout or connection loss leaves text available with a delivery-uncertain message so the user can check the PC before another send.

Repeated modifier downs are idempotent. Arrow/Backspace/Page holds begin repeat after 400 ms and repeat every 125 ms (8/sec). The server bounds repeated downs to ten per second across the session and releases on excess. Held keys use the same 1200 ms watchdog and 250 ms heartbeat as drag; releaseAll, visibility loss, cancellation, ownership replacement, pause, revoke and exit clear keys and buttons together. Partial native insertion tracks possible held physical/Unicode keys for cleanup.

Unicode input depends on target support for VK_PACKET/WM_CHAR. Some applications may handle it differently; no universal app compatibility is claimed. Native references: [KEYBDINPUT Unicode/extended flags](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput), [virtual-key values](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes).

## Media extension (Milestone 4)

`media.action` accepts exactly `{profile,action}`. Profile is General, YouTube, VLC or Stremio. Actions are playPause, next, previous, volumeUp, volumeDown and mute; YouTube/VLC/Stremio also support seekBack, seekForward and fullscreen. Stremio next means next episode, and previous is unsupported. Unsupported pairs fail validation. Ten media actions/second maximum; excess fails safely with cleanup. Real native key chords are allowlisted in MediaProfiles, with no executable strings.

The profile is explicit in each command and matches the UI shown to the user, avoiding races with another PC profile selection. `POST /api/settings/media` accepts exactly `{profile}`, requires authentication/a connected controlling session/not-paused plus exact Origin, and persists the setting. Version 0.2.1 captures the original live session before reading the body, then rechecks ownership and authentication under the ownership lock before saving. A reserved, paused, revoked, disconnected, or replaced session cannot complete a delayed mutation. Status and authenticated pong payload expose `mediaProfile` for UI synchronization. Failed persistence returns settings_failed (503). No typed data or tokens are included.

General maps system media/volume virtual keys; app profiles map documented player shortcuts. Foreground player focus/default hotkeys are required. YouTube previous is playlist-only. Seek labels have no fixed duration; General hides seek/fullscreen. See [media evidence and mappings](milestone-4-evidence.md).

HTTPS and WSS share origin on TCP 8443. Only the certificate SAN/allowlisted Host is accepted. All HTTP mutations and WebSocket upgrades require the exact `Origin: https://HOST:8443`; missing, `null`, foreign and slash-suffixed origins fail. No CORS policy or plaintext listener exists. GET and HTTP/2 CONNECT upgrades are supported. Logging does not contain requests, payloads or credentials.

## Pairing and authentication

Version 0.2.0 adds authenticated `GET /api/pc` (configured app IDs/names/profile/availability, current volume, public pending power status). PC executable paths are never returned. `POST /api/pc/launch` accepts exactly `{session,id}` and opens a locally configured executable without arguments, at most once every two seconds; it releases held input and selects the app's saved media profile. `POST /api/pc/volume` accepts exactly `{session,level}` with an integer level from 0 through 100. Authenticated pongs include current volume and public power status.

Power routes are `POST /api/pc/power/prepare` with `{session,action}` (sleep/restart/shutdown only), `/confirm` with `{session,token}`, and `/cancel` with `{session}`. Preparation issues a device/session-bound single-use token expiring in 60 seconds; confirmation starts a 30-second server countdown. Preparing is not execution. Pausing, disconnecting, revoking, replacing the phone session or shutting down the server cancels pending phone power. The device/session are checked again at execution, and held input is released before the native action. Local Windows-confirmed requests use the same queue and can be cancelled by the current phone controller.

All PC mutations require remembered authentication, exact Origin, a currently connected unpaused controlling session ID, and exact bounded JSON fields. Authorization and mutations run under the ownership lock; old session IDs cannot execute delayed actions after takeover. The native sleep call runs outside that lock so suspend/resume processing can finish. No remote paths, command lines or forced application termination are supported.

The PC generates a random 256-bit challenge, valid for two minutes. Its QR uses `/#pair=CHALLENGE`; the fragment never goes to the HTTP server or referrer. The phone removes it from the address bar and submits `POST /api/pair/request` with exactly `{challenge,name}`. Names are 1–64 characters with no control characters. Challenge consumption is atomic and single use; attempts are limited to five per minute. Creating a new PC link invalidates outstanding pairing requests.

The PC displays the device's claimed name and network address and requires explicit approval. A pending approval expires after two minutes. Closing the dialog, denying, timing out or an approval/storage failure cannot grant access. `/api/pair/poll` is POST because it consumes approval. Its temporary `__Host-pc-pair` cookie is Secure, HttpOnly, SameSite=Strict, Path=/, and expires with the request.

After approval, polling sets `__Host-pc-device` with the same cookie protections and a 30-day expiry. The reusable random credential is never returned in JSON or URLs and is inaccessible to page JavaScript. Only its SHA-256 hash and device metadata persist, inside a DPAPI-protected store in the installation's restricted user directory. Cookies remain browser-managed. Safari and the Home Screen may have separate cookie stores and require separate approval. Revoking a PC device record invalidates authentication and cancels its active session immediately; forgetting a browser also revokes its record.

## Session ownership

`GET /api/status` exposes this browser's authentication state, device name, pause/busy state, protocol version and input readiness. It sends no credentials. `POST /api/session/claim` accepts exactly `{takeover:false}`. It requires authentication and reserves a single-use, device-bound 30-second upgrade ticket in a Secure/HttpOnly/Strict `__Host-pc-lease` cookie. `/control/ws` requires both remembered authentication and that ticket. It does not authorize with URL parameters. A second controller receives `controller_busy` (409); replacing it requires the user's explicit Take over action (`takeover:true`). Pause returns 403. Unauthenticated or revoked credentials return 401.

Revocation, pause, session replacement, disconnect, timeout and shutdown cancel the owner. The synchronous release hook clears all tracked physical/Unicode keys and buttons before another owner can be assigned. Native partial failures pause control and preserve tracking for cleanup retries.

## Messages

Diagnostics accept only this message; authenticated sockets also accept the mouse, keyboard/navigation and media commands above:

```json
{"v":1,"seq":1,"type":"ping","payload":{}}
```

Response:

```json
{"v":1,"seq":1,"type":"pong","session":"opaque-session-id","payload":{}}
```

All four client fields must appear exactly once. Reject unknown/extra fields, unsupported versions, invalid JSON, non-text messages and replayed sequences. Sequence is an increasing positive JavaScript-safe integer. The client-command UTF-8 message limit is 1024 bytes, including fragments; the JSON depth limit is four. Diagnostics accept four pings/second; authenticated control accepts 180 messages/second with additional action-specific limits. Invalid messages close with `invalid_message` (1007), excessive rate with `rate_limit` (1008). Authentication is rechecked on each authenticated message. Session cancellation interrupts a blocked receive immediately.

`/diagnostics/ws` remains unauthenticated but only replies to ping; it never reserves a control session or injects input. At most four diagnostic sockets are allowed. HTTP requests are limited to 120 per minute per unpaired peer address or authenticated device, without queuing. Device identity is derived from a validated cookie, and peer addresses from the TCP connection, not forwarded headers. Authenticated power cancellation/logout share a separate 30-request/minute safety budget, so ordinary traffic cannot exhaust their capacity. Rejections return 429, a JSON error code, and `Retry-After: 60`. Kestrel caps ordinary connections at 64 and upgraded connections at eight. These limits bound application resources; they do not prevent all network-level denial of service. Approval polling is every two seconds. `/health` exposes readiness, protocol version, stage and inputEnabled. Authenticated pong payload additionally reports pause/profile/settings needed by the UI, without credentials or typed data.

No message for 15 seconds aborts the socket. Normal pings are every five seconds with a five-second deadline; held input uses the shorter lease above. Reconnect uses bounded exponential backoff/jitter, checks authentication and obtains a fresh ticket. Page hiding releases input and closes; resuming reconnects. Sequence/pending state resets without an offline queue or replay. Tab switches also release held input. No executable/shell message is supported; same-user local tray IPC is separate from the LAN protocol.

## Portable host capability extension (development)

Phase 1 keeps message protocol **v1** and all exact client-field validation. The existing Windows frontend remains compatible. Authenticated `GET /api/status` adds `capabilities` with `platform`, `pointer`, `keyboard`, `text`, `shortcuts`, `volume`, `launcher`, `powerActions`, `mediaActions` (profile to action names), and derived `inputEnabled`. Anonymous status returns no capability details. Existing consumers may ignore the additive response field; new consumers must treat missing capability data as the historical Windows v1 behavior only when connecting to an older Windows companion. Changes to client command fields, units or meaning require an explicit future protocol version rather than silent reinterpretation.

Logical shortcuts and media actions are translated by platform adapters. v1's `Windows` key token denotes logical Meta; Windows retains its existing left-Windows-key mapping. Scroll units remain 120 per detent with existing bounds/sign conversion, then native adapters translate the normalized units. Launcher paths/validation/discovery stay platform-local. Backend capability checks run inside the same ownership-authorized mutations, including the post-body-read checks; unsupported HTTP actions return `unsupported_action` (409), unsupported socket input follows existing `input_failed` closure/cleanup. Ping, release and authorized power cancellation remain available. `/health`/status derive input readiness from configured capabilities.

Phase 2 consumes this additive contract in the browser and shared shell. Authenticated status and pong add `readiness` (`storage`, `input`, `network`, `message`); capability fields use identical camel-case names in HTTP and WSS. Authenticated server replies are bounded to 4096 UTF-8 bytes to carry additive metadata; receivers must assemble fragments through EndOfMessage. Client-command size/depth/field validation remains unchanged. Existing browser WebSocket consumers already receive whole messages and ignore additive fields. `capabilities.labels` reports computer/modifier/shortcut names; the browser derives the same names from the platform ID for Phase 1 compatibility. macOS displays Command/Option and Command shortcuts; Linux displays Super and Ctrl/Alt shortcuts. Pointer, keyboard, text, shortcuts, profile-specific media, volume, launcher and power are gated independently. Missing v1 capability data retains older Windows behavior; present malformed data disables controls. A status protocol other than 1 is refused before reserving a control session; it is never silently interpreted as v1. Tests exercise macOS, Linux, unavailable, and existing Windows-style fixtures without changing v1 fields or semantics.

Session claims recheck cookie authentication under the ownership lock after the asynchronous body read. Host shutdown closes pairing immediately, denying pending decisions and refusing new challenges; a late dialog result cannot persist authorization. Failed revocation pauses control and cancels ownership. Certificate SANs must cover localhost and every exact bound IP; mismatched factory input is rejected before listening. Normal Mac/Linux Phase 2 startup has no listener because secure stores are still pending. The current production host factory remains Windows-only and enables the existing Windows features. [Portable architecture and test isolation](portable-host.md) describe adapter requirements.

## Phase 3 macOS adapter (development)

Production macOS source startup now composes real Quartz input and Keychain-backed certificates/devices; Linux has a Phase 4 source candidate with separate pending GNOME native acceptance. Protocol **v1**, exact allowlists/fields, Origin/Host checks, cookies, HTTP budgets, original-session reauthorization and bounds remain unchanged. Meta (`Windows`) maps Command, Alt maps Option; logical shortcuts resolve Command-layout key codes for C/V/A/Z and Command+Tab. Dedicated navigation keys keep macOS target-app semantics. Frozen 120-unit detents convert to fractional-accumulated 12 Quartz pixels per detent, with existing Host natural-scroll and native horizontal sign conversion. Unicode strings preserve scalar pairs; newline/tab use native Return/Tab pairs. No clipboard transfer or input-source change is added.

Capabilities independently expose real permission-dependent pointer/keyboard/text/shortcuts; missing Command layout resolution disables shortcuts. Media/volume/launcher/power stay unavailable and backend-denied. Startup and host replacement remain paused; Accessibility/session/storage loss cancels ownership. Native event submission independently checks permission/session and an armed environment generation, preventing a network/session transition racing Resume from enabling input. Failed submission now explicitly pauses the shared host even if immediate cleanup succeeds. Tracking includes possible held buttons/key codes/Unicode packets; failed cleanup survives listener replacement, blocks Resume and prevents graceful Exit until retried. AppKit lifecycle and SystemConfiguration notifications release synchronously and invalidate session network trust; storage failures stop all listeners.

`CGEventPost` returns no insertion count: success/`ack` establishes submission and permission checks, not target-app receipt. Unsupported/secure/custom text targets and IMEs need native acceptance. Guarded smoke tests abort input-producing actions on focus loss and permit only tracked up events for safety cleanup. Read-only layout-table checks, recording/headless fixtures and browser emulation are not physical-phone/native acceptance. See [macOS semantics and pending checks](macos-setup.md) and the dated roadmap evidence. Windows composition and DPAPI/root/device formats remain intact.

## Phase 4 Linux source candidate

v1 command fields, bounds, cookies, authentication/ownership rechecks, HTTP budgets and reserved safety capacity remain unchanged. Linux maps Meta to Super and logical copy/paste/selectAll/undo to Ctrl, switchApp to Alt+Tab. The actual logind session chooses X11 XCB/XTEST or explicit GNOME RemoteDesktop portal `Notify*` transport; XWayland never grants arbitrary Wayland input. Native generation/session checks, conservative stable-token tracking, key/button/wheel cleanup and Core repeat/watchdog apply to both. Permission/session/network/provider loss pauses ownership; failed ups retain tracking and block Resume/Exit. Closed-session local recovery requires explicit verification and never submits stale ups to a replacement backend.

`text` defaults false. Local GTK Unicode opt-in enables Ctrl+Shift+U scalar entry, CRLF/Return/Tab normalization and layout-resolved hex digits; it is not clipboard transfer, keymap mutation or universal text support. Interrupted compose requires local cancellation and confirmation before resuming; acknowledgements establish submission only. Portal protocol fixtures prove neither compositor receipt nor text/cleanup acceptance. Real GNOME Wayland text and revocation/cleanup remain mandatory pending gates. [Linux setup](linux-setup.md) defines target and backend limitations. Media/volume/launcher/power remain unavailable and backend-denied.


## Phase 5 source candidates (2026-10-13)

Supported native feature adapters and provisional packages are described in [the release-readiness handoff](release-readiness.md). Since version 0.3.0, releases publish experimental macOS/Ubuntu archives alongside the Windows companion; their native/phone/LAN acceptance remains pending. Mac Core Audio/native system media/player shortcuts/LaunchServices/login registration/sleep and Linux libpulse/MPRIS/player shortcuts/restricted GIO/XDG/logind use real platform services with backend denial and final native readiness/permission checks. Mac restart/shutdown stay unavailable; Linux power requires `Can* == yes`, `interactive=false`, no inhibitor override. Launcher wire input is still configured IDs only; no remote path/arguments/shell. Native dependencies/signing/login eligibility/actual player and physical power acceptance remain separate from recording/source-table/protocol/package checks.

Readiness transitions now force replacement of the capability snapshot even on unchanged loopback bindings; old ownership/listeners stop and replacement starts paused. v1 fields, budgets/reserved cancellation, original-session authentication checks, device-bound confirmation/countdown/cancellation and failed cleanup retention remain. Optional login start never preserves LAN approval/Wayland consent or resumes control. Provisional manifests record version/base revision/dirty source/file hashes; no tag/public release is created. Windows adapters, DPAPI and packaging remain intact. Real GNOME/phone/LAN/installed/Intel/CI gates stay visible.


Phase 5 PC endpoints additionally capture the original active controller before asynchronous JSON body reads. A delayed body cannot adopt a replacement session ID or its new power ticket. The supplied session must match that capture, and authentication/current connected ownership are checked under the existing lock before volume/launch/power mutation. Regression tests use chunked HTTPS bodies and recording adapters; no wire fields, HTTP budgets or physical actions change.
