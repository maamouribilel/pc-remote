#!/usr/bin/env python3
"""Build an Ubuntu x64 self-contained candidate or clean-commit final archive. No root/install/publish."""
import argparse
import platform
import sys
import tarfile
from portable import run
from package_support import prepare, publish, notices, guides, inspect, manifest, checksum, archive_contents, archive_name, manifest_path


def main():
    if sys.platform != 'linux' or platform.machine().lower() not in ('x86_64', 'amd64'):
        raise RuntimeError('Ubuntu candidate packaging requires a Linux x64 build host. Cross-publishing must not copy an ARM64 native bridge.')
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--rid', choices=['linux-x64'], default='linux-x64')
    parser.add_argument('--final', action='store_true', help='Release naming; requires a clean committed source tree.')
    args = parser.parse_args()
    version, output = prepare(args.rid, args.final)
    tree = output / 'PCRemote-linux-x64'
    publish(args.rid, tree)
    notices(tree / 'licenses'); guides(tree / 'docs', args.final, args.rid)
    if not (tree / 'wwwroot/index.html').is_file() or not (tree / 'libPcControllerLinux.so').is_file() or not (tree / 'libcoreclr.so').is_file():
        raise RuntimeError('Bundled browser/native bridge/runtime missing.')
    for file in [tree / 'PcController.Desktop', *tree.glob('*.so')]:
        if 'x86-64' not in run(['file', str(file)], capture=True):
            raise RuntimeError('Architecture mismatch: ' + file.name)
    dependencies = run(['ldd', str(tree / 'libPcControllerLinux.so')], capture=True)
    if 'not found' in dependencies:
        raise RuntimeError('Native dependency missing: ' + dependencies)
    (tree / 'native-dependencies.txt').write_text('\n'.join(line.strip().split(' => ')[0] for line in dependencies.splitlines() if ' => ' in line) + '\n')
    count = inspect(tree)
    archive = output / archive_name(version, args.rid, args.final, '.tar.gz')
    record = manifest_path(output, archive, args.final)
    manifest(tree, args.rid, version, record, final=args.final)
    def public_metadata(member):
        member.uid = member.gid = 0
        member.uname = member.gname = ''
        return member
    with tarfile.open(archive, 'w:gz') as tar:
        tar.add(tree, arcname=tree.name, filter=public_metadata)
    archive_contents(tree, archive)
    checksum(output, archive, record)
    print('Verified package files: ' + str(count))
    print('No release published. Extract the whole archive and run PCRemote-linux-x64/PcController.Desktop as the desktop user.')


if __name__ == '__main__':
    main()
