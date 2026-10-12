# Initial public repository validation

Validated on Windows on 2026-10-12 for PC Remote 0.2.0.

| Check | Final result |
| --- | --- |
| `scripts/build.ps1 -Configuration Release` | Pass; TypeScript/Vite and .NET build, zero .NET warnings/errors |
| npm dependency audit during the build | Zero reported vulnerabilities |
| `scripts/test.ps1` | 141 checks passed |
| `scripts/test.ps1 -Pairing` | 176 checks passed |
| `scripts/test.ps1 -Lifecycle` | 148 checks passed |
| `scripts/test.ps1 -PcFeatures` | 170 checks passed |
| `scripts/test-browser-mouse.ps1` | All 6 Playwright tests passed |
| `scripts/test.ps1 -Integration` | 156 checks passed with certificate verification |
| `scripts/test.ps1 -Lan` | 156 checks passed with certificate and IP-name verification |
| `scripts/test-desktop.ps1` against the installed companion | 155 checks passed |
| `scripts/package.ps1 -Version 0.2.0` | Installer/archive generation passed; packaged MIT license matches source |
| README media capture | Six browser screenshots and the walkthrough captured; zero page/console errors; journey commands verified against recording adapters |
| GitHub Markdown rendering | README renders with its hero, badges, screenshot table, feature/stack tables, and collapsible sections |
| Documentation links | No missing local Markdown/image targets; all README external URLs returned HTTP 200 |
| Gitleaks 8.30.1 | No leaks found in the staged publication snapshot |
| Additional publication audit | No tracked dependencies, SDKs, builds, release artifacts, certificate exports, protected credentials, environment files, private user paths, or original LAN addresses |
| `git diff --cached --check` | Pass |

The numeric .NET counts include core checks repeated by each mode; they are not a total of distinct test cases. Browser media and power checks use the project's isolated recording fixture. See [media provenance](assets/README.md) for screenshot dimensions and the fixture's displayed test data.

Historical evidence documents retain their original test conclusions. Personal paths were generalized, original LAN addresses were replaced with the documentation-only address `192.0.2.10`, and referenced design images were preserved under `assets/design/`. These historical design concepts are separate from the real screenshots in the README.

No new physical iPhone, actual sleep/restart/shutdown, clean-account/VM, reboot, or manual uninstall acceptance is claimed by this publication work. Those remain in the [release checklist](release-checklist.md) and [limitations](limitations.md). There is no public cloud demo: the live application is served by the Windows companion on the user's trusted Private LAN.
