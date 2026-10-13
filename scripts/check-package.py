#!/usr/bin/env python3
"""Validate the latest local candidate or final package inventory/checksum and read-only runtime startup."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import subprocess
import sys
from portable import ROOT
from package_support import inspect, version, archive_contents


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--rid', choices=['osx-arm64', 'osx-x64', 'linux-x64'], required=True)
    parser.add_argument('--final', action='store_true', help='Check the latest clean-commit final package.')
    parser.add_argument('--output', type=Path, help='Package directory to check instead of the latest local one.')
    args = parser.parse_args()
    candidates = sorted((ROOT / 'artifacts').glob(('release-' if args.final else 'candidate-') + version() + '-' + args.rid + '-*'))
    if not args.output and not candidates:
        raise RuntimeError('No package for this RID.')
    output = args.output or candidates[-1]
    records = list(output.glob('*manifest.json'))
    if len(records) != 1:
        raise RuntimeError('Expected exactly one package manifest.')
    manifest = json.loads(records[0].read_text())
    if manifest['productVersion'] != version() or manifest['rid'] != args.rid or manifest['candidate'] == args.final:
        raise RuntimeError('Package metadata differs from source version/RID/release mode.')
    if args.final and (manifest['sourceDirty'] or manifest['sourceRevision'] != subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()):
        raise RuntimeError('Final package is not from the exact clean checked-out commit.')
    tree = output / ('PC Remote.app' if args.rid.startswith('osx') else 'PCRemote-linux-x64')
    for name, digest in manifest['files'].items():
        if hashlib.sha256((tree / name).read_bytes()).hexdigest() != digest:
            raise RuntimeError('Package inventory mismatch: ' + name)
    if {str(p.relative_to(tree)) for p in tree.rglob('*') if p.is_file()} != set(manifest['files']):
        raise RuntimeError('Uninventoried package file.')
    inspect(tree)
    for line in (output / 'SHA256SUMS.txt').read_text().splitlines():
        digest, name = line.split('  ', 1)
        if hashlib.sha256((output / name).read_bytes()).hexdigest() != digest:
            raise RuntimeError('Archive checksum mismatch.')
        if name.endswith(('.zip', '.tar.gz')):
            archive_contents(tree, output / name, manifest.get('archiveSignatureMetadata'))
    native_rid = ('osx' if sys.platform == 'darwin' else 'linux') + ('-arm64' if platform.machine().lower() in ('arm64', 'aarch64') else '-x64')
    if args.rid != native_rid:
        print('Inventory/checksum passed. Native startup unavailable for ' + args.rid + ' on ' + native_rid + '; cross-build is not hardware acceptance.')
        return
    binary = tree / ('Contents/MacOS/PcController.Desktop' if args.rid.startswith('osx') else 'PcController.Desktop')
    env = os.environ.copy()
    for name in list(env):
        if name.startswith('DOTNET_ROOT') or name in ('DOTNET_MULTILEVEL_LOOKUP',):
            env.pop(name)
    env['PATH'] = '/usr/bin:/bin'
    info = json.loads(subprocess.check_output([str(binary), '--package-info'], env=env, text=True, timeout=15))
    if info['version'] != version() + '.0' or not info['bundledRemote'] or not info['informationalVersion'].endswith('+' + manifest['sourceRevision']):
        raise RuntimeError('Bundled product/source revision/web metadata differs: ' + json.dumps(info))
    if info['runtime'] != '.NET 10.0.12':
        raise RuntimeError('Unexpected bundled runtime: ' + info['runtime'])
    print('Candidate inventory, checksum and self-contained read-only startup passed: ' + json.dumps(info))
    print('GUI/installed/native/phone/LAN/power acceptance remains separate.')


if __name__ == '__main__':
    main()
