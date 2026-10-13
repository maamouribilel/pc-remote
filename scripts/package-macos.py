#!/usr/bin/env python3
"""Build a version-aware candidate or clean-commit final .app/ZIP; never publishes or uses signing credentials."""
import argparse
from pathlib import Path
import plistlib
import sys
import tempfile
from portable import ROOT, run
from package_support import prepare, publish, notices, guides, inspect, manifest, checksum, archive_contents, archive_name, manifest_path


def seal_and_archive(app, archive):
    # A flat .NET bundle needs extended code-signature attributes for managed PE DLLs.
    # Clear local metadata first, then retain only newly generated public signatures.
    run(['/usr/bin/xattr', '-cr', str(app)])
    run(['/usr/bin/codesign', '--force', '--deep', '--sign', '-', str(app)])
    run(['/usr/bin/codesign', '--verify', '--deep', '--strict', str(app)])
    from mac_package_metadata import write_zip
    metadata = write_zip(app, archive)
    archive_contents(app, archive, metadata)
    with tempfile.TemporaryDirectory(prefix='pc-remote-package-') as directory:
        run(['/usr/bin/ditto', '-x', '-k', str(archive), directory])
        run(['/usr/bin/codesign', '--verify', '--deep', '--strict', str(Path(directory) / app.name)])
    return metadata


def main():
    if sys.platform != 'darwin':
        raise RuntimeError('Build on macOS with Xcode Command Line Tools.')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--rid', choices=['osx-arm64', 'osx-x64'], default='osx-arm64')
    parser.add_argument('--final', action='store_true', help='Release naming; requires a clean committed source tree.')
    args = parser.parse_args()
    version, output = prepare(args.rid, args.final)
    app = output / 'PC Remote.app'
    binary = app / 'Contents/MacOS'
    binary.mkdir(parents=True)
    publish(args.rid, binary)
    with (app / 'Contents/Info.plist').open('wb') as file:
        plistlib.dump({'CFBundleName': 'PC Remote', 'CFBundleDisplayName': 'PC Remote', 'CFBundleExecutable': 'PcController.Desktop',
                      'CFBundleIdentifier': 'local.pc-remote.desktop.dev', 'CFBundlePackageType': 'APPL',
                      'CFBundleShortVersionString': version, 'CFBundleVersion': version, 'LSMinimumSystemVersion': '14.0',
                      'NSHighResolutionCapable': True}, file)
    resources = app / 'Contents/Resources'
    notices(resources / 'licenses')
    guides(resources / 'docs', args.final, args.rid)
    if not (binary / 'wwwroot/index.html').is_file() or not (binary / 'libPcControllerMac.dylib').is_file() or not (binary / 'libcoreclr.dylib').is_file():
        raise RuntimeError('Bundled browser/native bridge/runtime missing.')
    expected_arch = 'arm64' if args.rid == 'osx-arm64' else 'x86_64'
    for name in ['PcController.Desktop', *[p.name for p in binary.glob('*.dylib')]]:
        if expected_arch not in run(['/usr/bin/lipo', '-archs', str(binary / name)], capture=True).split():
            raise RuntimeError('Architecture mismatch: ' + name)
    # Seal the completed bundle with a local ad-hoc signature. This needs no identity/credential
    # and is not Developer ID, notarization or Gatekeeper distribution acceptance.
    archive = output / archive_name(version, args.rid, args.final, '.zip')
    metadata = seal_and_archive(app, archive)
    count = inspect(app)
    record = manifest_path(output, archive, args.final)
    manifest(app, args.rid, version, record, metadata, args.final)
    checksum(output, archive, record)
    print('Verified package files: ' + str(count))
    print('No release published. Launch with: open ' + repr(str(app)))


if __name__ == '__main__':
    main()
