"""Shared candidate/final packaging checks. Never tags, publishes, or changes trust."""
import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import stat
import tarfile
import zipfile
import xml.etree.ElementTree as ET
from portable import ROOT, DOTNET, run, frontend, NPM


def version():
    value = ET.parse(ROOT / 'Directory.Build.props').findtext('.//PcRemoteVersion')
    if not value or not re.fullmatch(r'\d+\.\d+\.\d+', value):
        raise RuntimeError('Shared product version must have three components.')
    if ET.parse(ROOT / 'src/PcController.App/PcController.App.csproj').findtext('.//Version') != value:
        raise RuntimeError('Windows and shared versions differ.')
    if ET.parse(ROOT / 'src/PcController.Desktop/PcController.Desktop.csproj').findtext('.//Version') != '$(PcRemoteVersion)':
        raise RuntimeError('Desktop must use the shared product version.')
    return value


def require_clean():
    # Final assets must come from an exact committed revision so the embedded source revision is true.
    status = run(['git', 'status', '--porcelain', '--untracked-files=all'], capture=True)
    if status:
        raise RuntimeError('Final packaging requires a clean committed source tree:\n' + '\n'.join(status.splitlines()[:20]))


def prepare(rid, final=False):
    from portable import frontend, executable
    if final:
        require_clean()
    frontend()  # Exact SDK, Node and npm, including in packaging-only CI.
    npm = executable('npm.cmd' if os.name == 'nt' else 'npm')
    run([npm, 'ci'], ROOT / 'src/remote')
    run([npm, 'run', 'build'], ROOT / 'src/remote')
    product = version()
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%d-%H%M%S-%f')
    output = ROOT / 'artifacts' / (('release-' if final else 'candidate-') + product + '-' + rid + '-' + stamp)
    output.mkdir(parents=True, exist_ok=False)
    return product, output


def publish(rid, target):
    # Preserve existing user lockfiles even though RID-specific NuGet graphs differ.
    locks = {p: p.read_bytes() for p in ROOT.glob('**/packages.lock.json') if 'node_modules' not in p.parts and '.tools' not in p.parts}
    try:
        run([DOTNET, 'publish', 'src/PcController.Desktop', '-c', 'Release', '-r', rid, '--self-contained', 'true',
             '-p:PublishTrimmed=false', '-p:PublishSingleFile=false', '-p:DebugType=None', '-p:DebugSymbols=false',
             '-p:CopyOutputSymbolsToPublishDirectory=false', '-p:ContinuousIntegrationBuild=true', '-p:SourceRevisionId=' + run(['git', 'rev-parse', 'HEAD'], capture=True), '-o', str(target)])
    finally:
        for path, content in locks.items():
            if path.read_bytes() != content:
                path.write_bytes(content)
    for file in target.rglob('*.pdb'):
        file.unlink()


def notices(target):
    target.mkdir(parents=True)
    shutil.copyfile(ROOT / 'LICENSE', target / 'PCRemote-LICENSE.txt')
    sdk = Path(DOTNET).resolve().parent
    if not (sdk / 'LICENSE.txt').is_file():
        sdk = Path(run([DOTNET, '--list-sdks'], capture=True).split('[')[-1].split(']')[0]).parent
    for name, dest in [('LICENSE.txt', 'DOTNET-LICENSE.txt'), ('ThirdPartyNotices.txt', 'DOTNET-ThirdPartyNotices.txt')]:
        shutil.copyfile(sdk / name, target / dest)
    package_root = Path(os.environ.get('NUGET_PACKAGES', str(Path.home() / '.nuget/packages')))
    lock = json.loads((ROOT / 'src/PcController.Desktop/packages.lock.json').read_text())
    inventory = set()
    for packages in lock['dependencies'].values():
        for name, detail in packages.items():
            if 'resolved' not in detail:
                continue
            inventory.add(name + ' ' + detail['resolved'])
            package = package_root / name.lower() / detail['resolved']
            if not package.is_dir():
                raise RuntimeError('Missing locked license package: ' + name)
            for source in package.rglob('*'):
                if source.is_file() and any(t in source.name.lower() for t in ('license', 'licence', 'notice')) and source.suffix.lower() in ('.txt', '.md', ''):
                    # Include subdirectory in the name to avoid overwriting native notices.
                    label = name + '-' + str(source.relative_to(package)).replace('/', '-')
                    shutil.copyfile(source, target / label)
    shutil.copyfile(ROOT / 'docs/licenses/Avalonia-12.1.4.md', target / 'Avalonia-LICENSE.md')
    (target / 'dependency-inventory.txt').write_text('\n'.join(sorted(inventory)) + '\n')
    if not any(p.name.lower().startswith('qrcoder-') for p in target.iterdir()) or not any('third-party' in p.name.lower() for p in target.iterdir()):
        raise RuntimeError('Required dependency/native notices missing.')


def archive_name(product, rid, final, suffix):
    return 'PCRemote-' + product + '-' + rid + ('' if final else '-candidate') + suffix


def guides(target, final=False, rid=None):
    # Retain source-relative guide links locally; a dirty candidate cannot reference a published tag.
    target.mkdir(parents=True)
    for file in (ROOT / 'docs').glob('*.md'):
        shutil.copyfile(file, target / file.name)
    shutil.copytree(ROOT / 'docs/assets', target / 'assets', ignore=shutil.ignore_patterns('.DS_Store', '._*'))
    shutil.copytree(ROOT / 'docs/licenses', target / 'licenses')
    security = target.parent / '.github'
    security.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(ROOT / '.github/SECURITY.md', security / 'SECURITY.md')
    branding = target.parent / 'src/remote/public'
    branding.mkdir(parents=True)
    shutil.copyfile(ROOT / 'src/remote/public/pc-remote-v2-128.png', branding / 'pc-remote-v2-128.png')
    shutil.copyfile(ROOT / 'LICENSE', target.parent / 'LICENSE')
    for name in ['README.md', 'AGENTS.md', 'CLAUDE.md', 'MULTIPLATFORM_PLAN.md']:
        shutil.copyfile(ROOT / name, target.parent / name)
    if final:
        platform = 'macOS' if rid.startswith('osx') else 'Ubuntu 24.04 GNOME'
        (target.parent / 'PREVIEW.txt').write_text(
            'PC Remote ' + version() + ' experimental ' + platform + ' preview (' + rid + ').\n'
            'Built from source revision ' + run(['git', 'rev-parse', 'HEAD'], capture=True) + '. Windows remains the primary preview platform.\n'
            'No Developer ID signing/notarization or Linux distribution signature. The macOS bundle has a local ad-hoc seal only.\n'
            'Extract the whole package; .NET/ASP.NET Core and the browser are bundled. Native OS services remain required.\n'
            'Starts paused; LAN approval is session-only. No automatic trust installation.\n'
            'Native, installed, phone/LAN, player, login and power acceptance remain incomplete: read docs/release-readiness.md.\n')
        return
    (target.parent / 'CANDIDATE.txt').write_text(
        'Provisional development candidate. It is not a published release asset.\n'
        'No Developer ID signing/notarization or Linux distribution signature. SDK/native ad-hoc signatures are not distribution signing.\n'
        'Extract the whole package; .NET/ASP.NET Core and the browser are bundled. Native OS services remain required.\n'
        'Starts paused; LAN approval is session-only. No automatic trust installation.\n'
        'See the repository MULTIPLATFORM_PLAN.md for actual evidence and pending acceptance.\n')


FORBIDDEN = {'.pfx', '.p12', '.protected', '.mobileconfig', '.pdb', '.key', '.pem', '.log', '.cer', '.crt', '.der', '.keystore', '.jks', '.dmp'}
PRIVATE_NAMES = {'credentials.json', 'secrets.json', 'devices.json', 'pointer-settings.json', 'media-settings.json', 'launcher-settings.json', 'update-settings.json'}
SECRET_PATTERNS = [re.compile(rb'-----BEGIN (?:RSA |EC |OPENSSH |DSA |ENCRYPTED )?PRIVATE KEY-----'),
                   re.compile(rb'gh[pousr]_[A-Za-z0-9]{36,}'), re.compile(rb'github_pat_[A-Za-z0-9_]{60,}'),
                   re.compile(rb'AKIA[0-9A-Z]{16}'), re.compile(rb'(?<![A-Za-z0-9_-])sk-(?:proj-)?[A-Za-z0-9_-]{40,}')]


def inspect(tree):
    count = 0
    for file in tree.rglob('*'):
        if file.is_symlink():
            raise RuntimeError('Unexpected package symlink: ' + str(file.relative_to(tree)))
        if not file.is_file():
            continue
        if file.suffix.lower() in FORBIDDEN or file.name in PRIVATE_NAMES or file.name.startswith('.env') or file.name.startswith('._') or file.name == '.DS_Store':
            raise RuntimeError('Unexpected private/debug/metadata file: ' + str(file.relative_to(tree)))
        data = file.read_bytes()
        if any(pattern.search(data) for pattern in SECRET_PATTERNS):
            raise RuntimeError('Potential secret in package: ' + str(file.relative_to(tree)))
        count += 1
    return count


def archive_contents(tree, archive, metadata=None):
    metadata = metadata or {}
    expected = {tree.name + '/' + str(p.relative_to(tree)): p for p in tree.rglob('*') if p.is_file()}
    directories = {tree.name, *[tree.name + '/' + str(p.relative_to(tree)) for p in tree.rglob('*') if p.is_dir()]}
    found = set()
    if archive.suffix == '.zip':
        with zipfile.ZipFile(archive) as container:
            if container.testzip():
                raise RuntimeError('Archive integrity failed.')
            for member in container.infolist():
                if member.is_dir():
                    if member.filename.rstrip('/') not in directories:
                        raise RuntimeError('Unexpected archive directory: ' + member.filename)
                    continue
                if member.filename in metadata:
                    data = container.read(member)
                    if member.filename in found or hashlib.sha256(data).hexdigest() != metadata[member.filename] or not data.startswith(b'\x00\x05\x16\x07') or b'com.apple.cs.' not in data or any(pattern.search(data) for pattern in SECRET_PATTERNS):
                        raise RuntimeError('Invalid inventoried signature metadata: ' + member.filename)
                    found.add(member.filename)
                    continue
                if stat.S_ISLNK(member.external_attr >> 16) or member.filename in found or member.filename not in expected or container.read(member) != expected[member.filename].read_bytes():
                    raise RuntimeError('Unexpected/different archive member: ' + member.filename)
                found.add(member.filename)
    else:
        with tarfile.open(archive, 'r:gz') as container:
            for member in container.getmembers():
                if member.uid or member.gid or member.uname or member.gname:
                    raise RuntimeError('Local ownership metadata in archive.')
                if member.isdir():
                    if member.name.rstrip('/') not in directories:
                        raise RuntimeError('Unexpected archive directory: ' + member.name)
                    continue
                if not member.isfile() or member.name in found or member.name not in expected or container.extractfile(member).read() != expected[member.name].read_bytes():
                    raise RuntimeError('Unexpected/different archive member: ' + member.name)
                found.add(member.name)
    if found != set(expected) | set(metadata):
        raise RuntimeError('Archive inventory differs from package tree.')


def manifest(tree, rid, product, path, metadata=None, final=False):
    revision = run(['git', 'rev-parse', 'HEAD'], capture=True)
    dirty = bool(run(['git', 'status', '--porcelain', '--untracked-files=all'], capture=True))
    if final and dirty:
        raise RuntimeError('Source changed during final packaging; rebuild from a clean commit.')
    files = run(['git', 'ls-files', '--cached', '--others', '--exclude-standard', '-z'], capture=True).split('\0')
    digest = hashlib.sha256()
    for name in sorted(set(files)):
        file = ROOT / name
        if name and file.is_file():
            digest.update(name.encode() + b'\0' + hashlib.sha256(file.read_bytes()).digest())
    record = {'candidate': not final, 'productVersion': product, 'rid': rid, 'sourceRevision': revision,
              'sourceDirty': dirty, 'sourceTreeSha256': digest.hexdigest(),
              'sdk': run([DOTNET, '--version'], capture=True), 'signing': 'No distribution signature/notarization',
              'files': {str(p.relative_to(tree)): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(tree.rglob('*')) if p.is_file()},
              'archiveSignatureMetadata': metadata or {}}
    path.write_text(json.dumps(record, indent=2) + '\n')


def manifest_path(output, archive, final):
    return output / (archive.name.removesuffix('.tar.gz').removesuffix('.zip') + '-manifest.json' if final else 'candidate-manifest.json')


def checksum(output, archive, metadata):
    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    (output / 'SHA256SUMS.txt').write_text(digest + '  ' + archive.name + '\n' + hashlib.sha256(metadata.read_bytes()).hexdigest() + '  ' + metadata.name + '\n')
    print('Package: ' + str(archive))
    print('SHA-256: ' + digest)
    return digest
