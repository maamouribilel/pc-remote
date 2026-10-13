#!/usr/bin/env python3
"""Build/run a dedicated Accessibility identity and disposable Cocoa input target."""
import argparse
from pathlib import Path
import platform
import plistlib
import sys
from portable import ROOT, DOTNET, run
from package_support import version


def bundle(path, name, executable, identifier):
    path.mkdir(parents=True, exist_ok=True)
    with (path / 'Contents/Info.plist').open('wb') as file:
        plistlib.dump({'CFBundleName': name, 'CFBundleDisplayName': name, 'CFBundleExecutable': executable,
                      'CFBundleIdentifier': identifier, 'CFBundlePackageType': 'APPL', 'CFBundleVersion': version(),
                      'LSMinimumSystemVersion': '14.0'}, file)


def main():
    if sys.platform != 'darwin':
        raise RuntimeError('Native smoke requires a real Mac.')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--onboard', action='store_true')
    args = parser.parse_args()
    architecture = 'arm64' if platform.machine() == 'arm64' else 'x86_64'
    rid = 'osx-arm64' if architecture == 'arm64' else 'osx-x64'
    app = ROOT / 'artifacts/macos-smoke/PC Remote Smoke.app'
    binary = app / 'Contents/MacOS'
    binary.mkdir(parents=True, exist_ok=True)
    try:
        run([DOTNET, 'publish', 'tests/PcController.MacOSTests', '-c', 'Release', '-r', rid, '--self-contained', 'true',
             '-p:MacBridgeArchitecture=' + architecture, '-p:DebugType=None', '-o', str(binary)])
    finally:
        run([DOTNET, 'restore', 'PcController.Portable.slnx', '--force-evaluate'])
    bundle(app, 'PC Remote Smoke', 'PcController.MacOSTests', 'local.pc-remote.native-smoke.dev')
    target = binary / 'Native Target.app'
    target_binary = target / 'Contents/MacOS'
    target_binary.mkdir(parents=True, exist_ok=True)
    run(['/usr/bin/clang', '-arch', architecture, '-fobjc-arc', '-Wall', '-Wextra', '-Werror', '-framework', 'AppKit',
         '-framework', 'ApplicationServices', 'tests/PcController.MacOSTests/target.m', '-o', str(target_binary / 'NativeTarget')])
    bundle(target, 'PC Remote disposable target', 'NativeTarget', 'local.pc-remote.disposable-target.dev')
    second = binary / 'Second Native Target.app'; second_binary = second / 'Contents/MacOS'; second_binary.mkdir(parents=True, exist_ok=True)
    import shutil
    shutil.copyfile(target_binary / 'NativeTarget', second_binary / 'NativeTarget')
    (second_binary / 'NativeTarget').chmod(0o755)
    bundle(second, 'PC Remote second disposable target', 'NativeTarget', 'local.pc-remote.disposable-target-b.dev')
    run(['/usr/bin/codesign', '--force', '--deep', '--sign', '-', str(app)])
    run(['/usr/bin/codesign', '--verify', '--deep', '--strict', str(app)])
    log = ROOT / '.tools/phase5-native-observed.log'
    errors = ROOT / '.tools/phase5-native-observed-errors.log'
    log.parent.mkdir(exist_ok=True)
    # Replace only this test run's output; never touch production data/trust.
    log.write_text(''); errors.write_text('')
    run(['/usr/bin/open', '-n', '-W', '--stdout', str(log), '--stderr', str(errors), str(app),
         '--args', '--onboard' if args.onboard else '--native-smoke'])
    print(log.read_text())
    if errors.read_text():
        print(errors.read_text(), file=sys.stderr)
    if not args.onboard and ('UNAVAILABLE:' in log.read_text() or 'Unhandled exception' in errors.read_text() or 'macOS adapter checks passed.' not in log.read_text()):
        raise RuntimeError('Guarded smoke did not complete; see the isolated logs under .tools/.')


if __name__ == '__main__':
    main()
