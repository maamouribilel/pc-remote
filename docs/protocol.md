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

The profile is explicit in each command and matches the UI shown to the user, avoiding races with another PC profile selection. `POST /api/settings/media` accepts exactly `{profile}`, requires authentication/current ownership/not-paused plus exact Origin, and persists the setting. Status and authenticated pong payload expose `mediaProfile` for UI synchronization. Failed persistence returns settings_failed (503). No typed data or tokens are included.

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

All four client fields must appear exactly once. Reject unknown/extra fields, unsupported versions, invalid JSON, non-text messages and replayed sequences. Sequence is an increasing positive JavaScript-safe integer. The UTF-8 message limit is 1024 bytes, including fragments; the JSON depth limit is four. Diagnostics accept four pings/second; authenticated control accepts 180 messages/second with additional action-specific limits. Invalid messages close with `invalid_message` (1007), excessive rate with `rate_limit` (1008). Authentication is rechecked on each authenticated message. Session cancellation interrupts a blocked receive immediately.

`/diagnostics/ws` remains unauthenticated but only replies to ping; it never reserves a control session or injects input. At most four diagnostic sockets and 120 HTTP requests per minute are allowed, without queuing. Approval polling is every two seconds. `/health` exposes readiness, protocol version, stage and inputEnabled. Authenticated pong payload additionally reports pause/profile/settings needed by the UI, without credentials or typed data.

No message for 15 seconds aborts the socket. Normal pings are every five seconds with a five-second deadline; held input uses the shorter lease above. Reconnect uses bounded exponential backoff/jitter, checks authentication and obtains a fresh ticket. Page hiding releases input and closes; resuming reconnects. Sequence/pending state resets without an offline queue or replay. Tab switches also release held input. No executable/shell message is supported; same-user local tray IPC is separate from the LAN protocol.
