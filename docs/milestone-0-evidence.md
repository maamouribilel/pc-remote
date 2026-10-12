# Milestone 0 verification evidence

> Publication note: `192.0.2.10` is a documentation-only replacement for the original LAN address. Use the live address shown by your companion.
Date: 2026-10-11. Final status: **actual trusted iPhone Safari/Home Screen acceptance passed**. User confirms full trust/no-warning transport. The observations below preserve the initial investigation and its pending stages; see release-evidence.md for current release status.

Latest physical result: user reports **Connected securely, 11 ms** from Safari on the actual iPhone after the Private-LAN startup fix. This establishes a user-observed application ping/pong over HTTPS/WSS. Explicit certificate full-trust/no-warning confirmation and actual Home Screen validation remain pending; this is not input latency evidence.

## Inspection and choices

- Initial repository contained only `IMPLEMENTATION_PLAN.md`; no Git repository and no applicable `AGENTS.md` in the project or ancestor paths. Existing plan preserved and updated in place.
- Windows 11 Pro, x64, version `10.0.26300`. Initially only .NET runtimes 6.0.16 / 8.0.21 were installed; there was no .NET SDK. Node 22.13.1, npm 10.9.2, Python 3.13.1 and Git were present. No `cl` or `cmake` found on PATH. This session is unelevated.
- Selected official supported stable .NET 10 LTS / Node 24 LTS. Portable SDK 10.0.401 and Node 24.21.0 archives were checked against official SHA-512/SHA-256 hashes before extraction to ignored `.tools`. Versions are pinned, with npm 11.19.0, Vite 8.3.4, TypeScript 7.0.2 and Playwright 1.64.0. No .NET NuGet dependencies added.
- Sources: [Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [.NET 10 release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json), [Node releases](https://nodejs.org/en/about/previous-releases), [Node 24 archive checksums](https://nodejs.org/dist/v24.21.0/SHASUMS256.txt).
- Observed Wi-Fi 3 / `192.0.2.10`, network name `Ooredoo-ALHN-126D-5 2`, Public profile. Firewall enabled for all profiles. User confirmed this is the trusted home network and that an actual iPhone is available. User-side Private-profile change and administrator firewall installation are still pending; no rule or router exposure was silently added.

## Commands and outcomes

| Check | Evidence | Result |
| --- | --- | --- |
| Tool bootstrap | `./scripts/install-tools.ps1` | SDK 10.0.401, Node 24.21.0, npm 11.19.0; portable paths used |
| Dependency install | `npm ci` using pinned HTTPS registry and lockfile | 20 packages; npm audit reported 0 vulnerabilities |
| Mobile compilation | `tsc --noEmit && vite build` | Pass; HTML, local CSS/JS and favicon bundled, no CDN |
| Debug solution build | `.tools/dotnet/dotnet.exe build PcController.slnx` | Pass, 0 warnings / 0 errors |
| Release build | `./scripts/build.ps1 -Configuration Release` | Pass, 0 warnings / 0 errors |
| WPF launch | `./scripts/run.ps1 -Configuration Release` | Real `PcController.App` window titled “PC Remote — connection setup”; ready status inspected |
| Bound address | `Get-NetTCPConnection -LocalPort 8443 -State Listen` | Only `127.0.0.1:8443` while network is Public |
| Protocol and crypto | `./scripts/test.ps1` | 20 checks: version/shape/ranges, unsupported input, malformed JSON, replay, size, DPAPI, public export, chain/EKU, persistence and SAN regeneration |
| Real transport | `./scripts/test.ps1 -Integration` | 35 total checks including HTTPS health/assets, exact Origin/Host rejection, WSS ping/pong, malformed/oversized/input frame rejection, rate limit and host survival; custom root trust and hostname validation, TLS 1.2/1.3 enforced |
| Mobile rendering | `./scripts/test.ps1 -Integration -Browser` | Earlier run: 4 pass with normal certificate verification and temporary CurrentUser root. Final UI run: explicit loopback-only browser TLS bypass because re-imported Windows trust did not persist; installed Edge used. This is UI-only evidence |
| Trust cleanup | `./scripts/trust-windows.ps1 -Action Remove` | Only this installation's temporary CurrentUser root removed after browser QA |
| Actual iPhone | Not run yet | Pending — Safari, root trust, Home Screen, LAN and firewall acceptance unverified |

Browser flow: `https://localhost:8443/` → real server pong → Connected securely / HTTPS + WSS / measured RTT → Test connection → fresh pong. Offline mode disables the test button and shows connection loss; restoring networking reconnects to the real server. This is desktop emulation, not an iPhone or input test.

| Browser QA criterion | Result |
| --- | --- |
| URL/title identity and meaningful content | Pass |
| Blank page / framework error overlay | None |
| Console errors and warnings | None in the app on final run |
| Viewports | 390×700 portrait, 844×390 landscape, 1280×800 desktop |
| Target size / horizontal overflow | Button ≥44 CSS px; no horizontal overflow |
| Interaction and reconnect | Pass against real WSS |
| Screenshot inspection | Portrait and landscape inspected with `view_image`, compared against generated concept; final previews saved outside the repository in the chat's visualization folder, temporary QA artifacts removed |

The in-app browser was attempted first: initially `ERR_CONNECTION_CLOSED`, then `ERR_CERT_AUTHORITY_INVALID` after fixing TLS. Even after temporary Windows CurrentUser trust, IAB rejected the private CA. Edge/Playwright was used as the fallback. Earlier successful runs used normal certificate verification. After removing/re-adding trust, native imports reported success but the root was absent on reopening the store, causing the browser rerun to fail. Final UI QA uses the explicit `PC_CONTROLLER_BROWSER_TLS_BYPASS=1` flag in its isolated localhost context; it is off by default and skips browser verification only, not encryption or .NET integration verification. No warning interstitial was clicked. This cannot establish iPhone or desktop root onboarding reliability. Test artifacts live outside the repository in the Windows temp directory. Representative loopback RTT screenshots read 1 ms; this is not LAN latency or Windows input latency.

## Failures fixed during this milestone

1. No .NET SDK: installed official checksum-verified portable SDK; did not replace global runtimes.
2. Initial npm invocation used the old global Node after a path-resolution mistake: corrected the portable path. npm engine checks rejected the wrong toolchain. The inherited npm registry used HTTP; project config now pins HTTPS and the lockfile was regenerated, followed by a clean install and successful audit.
3. TypeScript flagged a missing CSS import declaration and possibly undefined pending ping; added Vite types and an explicit pending guard.
4. C# flagged a nullable Host value; the allowlist rejects a missing Host safely.
5. Ephemeral imported server keys broke Schannel TLS: temporary Windows user key containers fixed real transport. Persistent certificate files remain DPAPI-protected.
6. A Host rejection test changed TLS SNI along with the HTTP header and failed name validation: corrected the test to independently vary HTTP Host over hostname-validated TLS.
7. Desktop browser WebSocket connection failed: accepted HTTP/2 CONNECT as well as GET, and listed the exact WSS origin in CSP. Integration tests had previously exercised HTTP/1.1 only.
8. Browser console reported missing favicon: added a small local SVG favicon. Final console checks pass.
9. Visual review showed inconsistent desktop font rendering: chose an explicit Arial fallback while preserving native Apple font selection.
10. Optional Windows trust imports did not reliably survive store reopening after removal/reinstallation: the helper now verifies retention and fails with an actionable message. The environmental cause is unresolved. Final UI QA uses the documented explicit test-only loopback bypass; trusted .NET transport validation remains separate and passes.

## Remaining limits and next action

Follow `docs/iphone-milestone-0.md` on the real phone. Record the model/iOS version, trusted Safari/WSS success, Home Screen success, five RTT values and lock/unlock recovery. Only then complete the Milestone 0 acceptance gate and proceed to Milestone 1 pairing. No Windows input, input cleanup, device authentication, QR pairing, media actions, installer or clean-machine installation has been tested or claimed.

This prototype requires restart for address changes and leaf renewal. It supports Private IPv4 addresses only; domain/public networks are not implicitly exposed. Optional Windows root-store retention is unresolved on this host. It has a diagnostic concurrency/rate ceiling, and no HTTP control listener. Final onboarding is still provisional until the actual iPhone validates the private-CA trust flow.

## Reported iPhone reachability failure

On 2026-10-11 the user reported Safari could not connect to the server. Live inspection found the app running, a loopback-only `127.0.0.1:8443` listener, Wi-Fi 3 still Public at `192.0.2.10`, and no named private firewall rule. This explains why the PC is not available over its LAN address yet; Wi-Fi isolation and certificate trust cannot be evaluated until that is corrected. The user must perform the administrator network/firewall steps, restart the normal-user Release app, and use its LAN address on the iPhone. The phone test remains failed/pending, not passed.

## LAN startup correction and phone success

Subsequent inspection found Wi-Fi 3 now Private and the expected rule installed/enabled for TCP 8443, Private profiles, LocalSubnet. The companion was closed normally and restarted. Private interface selection exposed an actual defect: `dynamic` COM automation threw `ArgumentException` on `GetAdapterId`'s GUID result. The Windows SDK [INetworkConnection interface](https://learn.microsoft.com/en-us/windows/win32/api/netlistmgr/nn-netlistmgr-inetworkconnection) uses a native GUID output; replacing that call with a typed dual-interface declaration fixed the error without opening Public adapters.

`--network` now reports Private IPv4 `192.0.2.10`. Release rebuild passes with zero warnings/errors. A fresh app process listens on that address and loopback. `./scripts/test.ps1 -Lan` passes all 35 checks against `https://192.0.2.10:8443` / its WSS endpoint, including real root/chain/IP-SAN verification and rejection/rate tests. The original temporary Windows root trust is not required for these custom-root checks.

Afterward the user reported “i see connected securly 11ms” on the actual iPhone. This supersedes the initial connection failure for Safari reachability and diagnostic ping/pong. Full trust without warning bypass and Home Screen results still need confirmation. The app remains running for those tests.
