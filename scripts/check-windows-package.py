#!/usr/bin/env python3
"""Validate a provisional Windows package without installing or executing the companion."""
import hashlib
from pathlib import Path
import zipfile
from portable import ROOT
from package_support import inspect, version


def main():
    directory = Path((ROOT / 'artifacts/latest-release.txt').read_text().strip())
    tree = directory / 'PCRemote-win-x64'
    product = version()
    for name in ['PcController.App.exe', 'PcController.App.dll', 'coreclr.dll', 'wwwroot/index.html', 'licenses/DOTNET-LICENSE.txt', 'licenses/DOTNET-ThirdPartyNotices.txt', 'licenses/QRCODER-LICENSE.txt']:
        if not (tree / name).is_file():
            raise RuntimeError('Required Windows package content missing: ' + name)
    inspect(tree)
    archive = directory / ('PCRemote-' + product + '-win-x64.zip')
    with zipfile.ZipFile(archive) as file:
        if file.testzip():
            raise RuntimeError('Windows ZIP integrity failed.')
        for name in file.namelist():
            if name.endswith('/'):
                continue
            source = directory / name.replace('\\', '/')
            if file.read(name) != source.read_bytes():
                raise RuntimeError('Windows ZIP differs from publish inventory: ' + name)
    for line in (directory / 'SHA256SUMS.txt').read_text(encoding='utf-8-sig').splitlines():
        digest, name = line.split('  ', 1)
        if hashlib.sha256((directory / name).read_bytes()).hexdigest() != digest.lower():
            raise RuntimeError('Windows asset checksum mismatch.')
    print('Windows provisional package contents, licenses, byte-pattern secret scan, ZIP inventory and checksums passed. No install/uninstall/power action executed.')


if __name__ == '__main__':
    main()
