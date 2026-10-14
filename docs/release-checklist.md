# Release acceptance

## Source, documentation, and GitHub synchronization

For every new version, complete [AGENTS.md](https://github.com/maamouribilel/windows-pc-mobile-controller/blob/main/AGENTS.md)'s mandatory release synchronization and the [publishing procedure](https://github.com/maamouribilel/windows-pc-mobile-controller/blob/main/docs/releases.md#publishing-the-next-version):

- [ ] Shared version, Windows app project, Desktop inheritance, packaging default, installer fallback, and annotated tag use the same version.
- [ ] README current version/status/downloads/commands and affected features/media are updated.
- [ ] Both AGENTS.md and CLAUDE.md version links and instructions are updated.
- [ ] Download documentation and dated release evidence are updated; affected guides and limitations are current.
- [ ] `.github/` files are reviewed and affected workflows/templates/configuration are updated.
- [ ] `scripts/check-release-docs.ps1`, security/regression suites, dependency/runtime advisory checks, source/package secret scans, licenses, and hashes pass.
- [ ] Matching source/documentation is committed and pushed; that commit's `final` dispatch (every suite on Windows, macOS and Linux) passed; bundled runtime advisories were rechecked on release day.
- [ ] Final assets come from `--final`/the CI `final` dispatch at that exact commit; the release has installer, ZIP, any macOS/Ubuntu archives and manifests, one combined checksum file, accurate notes and preview/stable status.
- [ ] Public tag/commit and download URLs, sizes, and SHA-256 digests are verified before declaring completion.

## Manual acceptance

Use the installer in the directory named by `artifacts/latest-release.txt`. Save work before rebooting; use disposable input targets. Record actual results in release-evidence.md and IMPLEMENTATION_PLAN.md.

1. Install as a normal user. Record actual publisher/SmartScreen prompts. Choose Private LAN firewall and approve its administrator prompt, or use Configure Private LAN firewall in setup. Only the short helper elevates. Install location: `%LOCALAPPDATA%\Programs\PCRemote`.
2. Clean-account/VM check: with no SDK/Node/runtime, launch and repeat certificate/pairing onboarding. A new Windows user creates a different CA; export/install/fully trust that public profile, compare fingerprints and approve a fresh device. Remove that test profile afterwards. Current-account tests are not clean-environment evidence.
3. Actual iPhone: refresh Safari/reopen Home Screen at setup's address, confirm secure connection without warning. In a NEW unsaved Notepad tab, focus text and send `Bonjour été café 😀` once; try Enter/Backspace/arrows/Ctrl+A/Ctrl+Z. Repeat in a disposable browser field. Check mouse/right/double/scroll/cross-monitor drag and matching focused media players.
4. Start drag on a disposable target; revoke THIS browser's device in PC setup. Button must release, connection close, and refresh must fail to control until fresh PC approval. Pair again. Repeat with Shift lit, then pair again; preserve unrelated devices.
5. Lock/unlock and disable/re-enable Wi-Fi during a drag/modifier hold. Input must release after about two seconds, reconnect/reset without replay or reapproval. Check tray pause/resume, tab release, both phone browsers, landscape and phone keyboard usability.
6. Enable Start PC Remote at sign-in and Save sign-in preference. Save work, restart Windows yourself, sign into the same account. Confirm tray auto-start, secure remembered iPhone reconnect and real input. Use the new setup address if DHCP changed IP.
7. Optional live-network/sleep check: use a disposable target. Trusted Private IP changes must update setup/QR/SANs and preserve root/devices. Public profiles must lose LAN listening. Check sleep/wake reconnect/release. Controlled automated snapshots do not prove live DHCP/profile changes.
8. Windows Settings → Apps → PC Remote → Uninstall; approve its firewall removal prompt. Verify process, executable, Start shortcuts, uninstall entry and matching startup entry are gone, and the `PcRemote-Private-*` rule for THAT executable is removed. Other/developer rules survive. Protected `%LOCALAPPDATA%\PcController` data remains; untracked files must not be recursively deleted. Reinstall/launch and confirm retained trust/devices reconnect. Re-enable optional startup if wanted. Permanent retirement also requires phone/Windows trust removal per CERTIFICATES.md.

Automated recording tests cover bounds, expired/single-use approval, Origin/auth rejection, invalid/binary/oversized/replayed/rate-excess input, remembered reconnect and revoke over validated HTTPS/WSS. They do not establish physical input release. Earlier actual-device results remain separate from final packaged checks.


## Multi-platform readiness

- [ ] Distinguish implemented source, validated candidate, pending native/manual acceptance and unpublished public release per [release readiness](release-readiness.md).
- [ ] Each final package bundles the remote/runtime/native bridge, exact version/source revision, actual managed/native licenses and safe contents; archive integrity, artifact secret scan and all aggregate SHA-256 sums pass.
- [ ] Mac ARM64/real Intel and Ubuntu 24.04 x64/GNOME X11/Wayland are checked separately. Windows regression/DPAPI/Schannel/installer gates remain. On macOS, an update of an existing installation is approved through **Allow Keychain access after update…** and keeps the same root/devices.
- [ ] Native audio/read-back, actual matching player/default shortcuts/focus, real configured app launching and own login-start approval/sign-in/opt-out are accepted on authorized targets.
- [ ] Phase 4 real Wayland text/cleanup, lock/suspend/provider/network/tray and physical Safari/Home Screen HTTPS/WSS acceptance are retained until actually passed.
- [ ] Power recording/protocol tests remain separate from separately authorized disposable physical execution. Unsupported actions/backend/architectures stay UI/backend-denied.
- [ ] Signing/notarization/repository signature status and native OS dependencies are accurate; clean-account runtime-free startup is separately observed.
- [ ] CI jobs have actually been observed, source is clean/committed before final packaging, and every public URL/tag/size/digest is checked after authorized publication.

Follow the numbered [exact platform manual steps](release-readiness.md#exact-pending-acceptance); neither a build nor an isolated/headless test closes them.
