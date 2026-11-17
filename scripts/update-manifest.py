#!/usr/bin/env python3
"""Create, sign and verify the signed release manifest that companions use for update notices.

Each release from the first one with update checks attaches `update.json` and `update.json.sig`.
The companion (src/PcController.Host/UpdateCheck.cs) trusts a release only when this signature
verifies against a public key embedded from src/PcController.Host/update-signing-keys.pub, so the
private key is the release trust anchor: it stays on the maintainer's machine, outside the
repository, and never in CI or GitHub secrets. See AUTO_UPDATE_PLAN.md.

  keygen    create the offline ECDSA P-256 signing key and print its public key (paste it into the .pub file)
  create    build update.json from a verified release directory and its SHA256SUMS.txt
  sign      write update.json.sig with the local key (checked against the committed public keys)
  verify    check a manifest and signature against the committed public keys (also for public downloads)
  protect   add a passphrase to the key file; the public key does not change
  selftest  round-trip with a throwaway key in a temporary directory (safe in CI)
"""
import argparse
import base64
import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
REPOSITORY = 'maamouribilel/pc-remote'
PUBLIC_KEYS = ROOT / 'src/PcController.Host/update-signing-keys.pub'
DEFAULT_KEY = Path.home() / '.config/pc-remote-release/update-signing-key.pem'
VERSION = re.compile(r'^(0|[1-9]\d{0,4})\.(0|[1-9]\d{0,4})\.(0|[1-9]\d{0,4})$')
# Release asset suffix -> (platform, kind). Must match UpdateCheck.cs.
ASSETS = {
    '-win-x64-setup.exe': ('win-x64', 'installer'),
    '-win-x64.zip': ('win-x64', 'zip'),
    '-osx-arm64.zip': ('osx-arm64', 'zip'),
    '-osx-x64.zip': ('osx-x64', 'zip'),
    '-linux-x64.tar.gz': ('linux-x64', 'archive'),
}
SUMMARY_LIMIT = 300
MANIFEST_LIMIT = 64 * 1024


def fail(message):
    raise SystemExit('update-manifest: ' + message)


def openssl():
    path = shutil.which('openssl')
    if not path:
        fail('openssl is required.')
    return path


def refuse_ci():
    if os.environ.get('CI', '').lower() == 'true':
        fail('the release signing key is never used in CI; sign locally after verifying the assets.')


def key_path(value):
    path = Path(value).expanduser().resolve()
    if path == ROOT or ROOT in path.parents:
        fail(f'{path} is inside the repository; keep the private key outside it.')
    return path


def check_summary(summary):
    if not summary or len(summary) > SUMMARY_LIMIT or any(ord(c) < 32 or 127 <= ord(c) < 160 for c in summary):
        fail(f'summary must be 1-{SUMMARY_LIMIT} characters of plain text without control characters.')


def validate(data):
    """Apply the same rules as the companion; returns the parsed manifest."""
    if len(data) > MANIFEST_LIMIT:
        fail('manifest is larger than 64 KiB.')
    try:
        manifest = json.loads(data.decode('utf-8'))
    except (UnicodeDecodeError, json.JSONDecodeError) as error:
        fail(f'manifest is not UTF-8 JSON: {error}')
    if not isinstance(manifest, dict) or manifest.get('schema') != 1 or manifest.get('product') != 'pc-remote':
        fail('manifest schema must be 1 and product "pc-remote".')
    version = manifest.get('version')
    if not isinstance(version, str) or not VERSION.match(version) or manifest.get('tag') != 'v' + version:
        fail('version must be X.Y.Z and tag vX.Y.Z.')
    if manifest.get('releaseUrl') != f'https://github.com/{REPOSITORY}/releases/tag/v{version}':
        fail('releaseUrl must be the exact release page for the tag.')
    if not isinstance(manifest.get('security'), bool):
        fail('security must be true or false.')
    try:
        datetime.datetime.fromisoformat(str(manifest.get('published')).replace('Z', '+00:00'))
    except ValueError:
        fail('published must be an ISO 8601 date-time.')
    check_summary(manifest.get('summary') if isinstance(manifest.get('summary'), str) else '')
    assets = manifest.get('assets')
    if not isinstance(assets, list) or not assets:
        fail('assets must list at least one release download.')
    for asset in assets:
        name = asset.get('name') if isinstance(asset, dict) else None
        expected = next((v for suffix, v in ASSETS.items() if isinstance(name, str) and name == f'PCRemote-{version}{suffix}'), None)
        if expected is None or (asset.get('platform'), asset.get('kind')) != expected:
            fail(f'unexpected asset entry {asset!r}.')
        if not isinstance(asset.get('size'), int) or not 0 < asset['size'] <= 2 ** 31 or not re.fullmatch(r'[0-9a-f]{64}', str(asset.get('sha256'))):
            fail(f'asset {name} needs a positive size and a lowercase SHA-256.')
    return manifest


def public_keys(path):
    text = Path(path).read_text(encoding='ascii')
    blocks = re.findall(r'-----BEGIN PUBLIC KEY-----.+?-----END PUBLIC KEY-----', text, re.S)
    if not blocks:
        fail(f'no PUBLIC KEY block in {path}.')
    return [block + '\n' for block in blocks]


def verify_signature(manifest_path, signature_path, keys_path):
    try:
        signature = base64.b64decode(Path(signature_path).read_text(encoding='ascii').strip(), validate=True)
    except (ValueError, UnicodeDecodeError):
        return False
    with tempfile.TemporaryDirectory() as directory:
        der = Path(directory) / 'signature.der'
        der.write_bytes(signature)
        for index, block in enumerate(public_keys(keys_path)):
            pem = Path(directory) / f'key{index}.pem'
            pem.write_text(block, encoding='ascii')
            result = subprocess.run([openssl(), 'dgst', '-sha256', '-verify', str(pem), '-signature', str(der), str(manifest_path)],
                                    capture_output=True, text=True)
            if result.returncode == 0 and 'Verified OK' in result.stdout:
                return True
    return False


def keygen(args):
    refuse_ci()
    key = key_path(args.key)
    if key.exists() and not args.force:
        fail(f'{key} already exists; a new key needs a key-rotation release (see AUTO_UPDATE_PLAN.md).')
    key.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
    os.chmod(key.parent, 0o700)
    previous = os.umask(0o077)
    try:
        generated = subprocess.run([openssl(), 'ecparam', '-name', 'prime256v1', '-genkey', '-noout'], check=True, capture_output=True).stdout
        temporary = key.with_name(key.name + '.tmp')
        subprocess.run([openssl(), 'pkcs8', '-topk8', '-nocrypt', '-out', str(temporary)], input=generated, check=True)
        os.chmod(temporary, 0o600)
        os.replace(temporary, key)
    finally:
        os.umask(previous)
    public = subprocess.run([openssl(), 'pkey', '-in', str(key), '-pubout'], check=True, capture_output=True, text=True).stdout
    print(f'Private key written to {key} (0600). Back it up offline; it is never committed.')
    print('Public key for src/PcController.Host/update-signing-keys.pub:')
    print(public, end='')


def create(args):
    version = args.version
    if not VERSION.match(version):
        fail('version must be X.Y.Z.')
    check_summary(args.summary)
    directory = Path(args.assets).resolve()
    sums = {}
    for line in (directory / 'SHA256SUMS.txt').read_text(encoding='utf-8').splitlines():
        match = re.fullmatch(r'([0-9a-fA-F]{64}) [ *](.+)', line.strip())
        if match:
            sums[match.group(2)] = match.group(1).lower()
    assets = []
    for suffix, (platform, kind) in ASSETS.items():
        name = f'PCRemote-{version}{suffix}'
        if name not in sums:
            continue
        path = directory / name
        if not path.is_file():
            fail(f'{name} is listed in SHA256SUMS.txt but missing from {directory}.')
        digest = hashlib.sha256()
        with path.open('rb') as stream:
            for chunk in iter(lambda: stream.read(1 << 20), b''):
                digest.update(chunk)
        if digest.hexdigest() != sums[name]:
            fail(f'{name} does not match SHA256SUMS.txt.')
        assets.append({'platform': platform, 'kind': kind, 'name': name, 'size': path.stat().st_size, 'sha256': sums[name]})
    if not assets:
        fail(f'SHA256SUMS.txt lists no PCRemote-{version} downloads.')
    published = args.published or datetime.datetime.now(datetime.timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')
    manifest = {
        'schema': 1, 'product': 'pc-remote', 'version': version, 'tag': 'v' + version,
        'releaseUrl': f'https://github.com/{REPOSITORY}/releases/tag/v{version}',
        'published': published, 'security': args.security, 'summary': args.summary, 'assets': assets,
    }
    data = (json.dumps(manifest, indent=2, ensure_ascii=False) + '\n').encode('utf-8')
    validate(data)
    out = Path(args.out).resolve() if args.out else directory / 'update.json'
    if out.exists() and not args.force:
        fail(f'{out} exists; pass --force to replace an unpublished manifest.')
    out.write_bytes(data)
    print(f'Wrote {out} ({len(assets)} downloads). Sign it with: python3 scripts/update-manifest.py sign --manifest {out}')


def sign_with(key, manifest, keys):
    validate(manifest.read_bytes())
    with tempfile.TemporaryDirectory() as directory:
        der = Path(directory) / 'signature.der'
        subprocess.run([openssl(), 'dgst', '-sha256', '-sign', str(key), '-out', str(der), str(manifest)], check=True)
        signature = base64.b64encode(der.read_bytes()).decode('ascii')
    target = manifest.with_name(manifest.name + '.sig')
    target.write_text(signature + '\n', encoding='ascii')
    if not verify_signature(manifest, target, keys):
        target.unlink()
        fail(f'the signature does not match any key in {keys}; this is not a trusted release key.')
    print(f'Wrote and verified {target}.')


def sign(args):
    refuse_ci()
    key = key_path(args.key)
    if not key.is_file():
        fail(f'{key} not found; run keygen first.')
    if os.name == 'posix' and key.stat().st_mode & 0o077:
        fail(f'{key} is readable by other users; chmod 600 it.')
    sign_with(key, Path(args.manifest).resolve(), args.keys)


def verify(args):
    manifest = Path(args.manifest).resolve()
    parsed = validate(manifest.read_bytes())
    signature = Path(args.signature).resolve() if args.signature else manifest.with_name(manifest.name + '.sig')
    if not verify_signature(manifest, signature, args.keys):
        fail(f'signature {signature} does not verify against {args.keys}.')
    print(f'Verified PC Remote {parsed["version"]} manifest ({len(parsed["assets"])} downloads, security={str(parsed["security"]).lower()}).')


def protect(args):
    refuse_ci()
    key = key_path(args.key)
    if not key.is_file():
        fail(f'{key} not found.')
    temporary = key.with_name(key.name + '.tmp')
    previous = os.umask(0o077)
    try:
        # openssl asks for the current passphrase (if any) and the new one on the terminal.
        subprocess.run([openssl(), 'pkcs8', '-topk8', '-v2', 'aes-256-cbc', '-in', str(key), '-out', str(temporary)], check=True)
        os.chmod(temporary, 0o600)
        os.replace(temporary, key)
    finally:
        os.umask(previous)
        if temporary.exists():
            temporary.unlink()
    print(f'{key} is now passphrase-protected; sign will ask for the passphrase.')


def selftest(_):
    def expect_failure(action, name):
        try:
            action()
        except SystemExit:
            print('PASS: rejects ' + name)
            return
        raise SystemExit('FAIL: accepted ' + name)

    with tempfile.TemporaryDirectory() as directory:
        work = Path(directory)
        key, public = work / 'key.pem', work / 'keys.pub'
        generated = subprocess.run([openssl(), 'ecparam', '-name', 'prime256v1', '-genkey', '-noout'], check=True, capture_output=True).stdout
        subprocess.run([openssl(), 'pkcs8', '-topk8', '-nocrypt', '-out', str(key)], input=generated, check=True)
        os.chmod(key, 0o600)
        public.write_text(subprocess.run([openssl(), 'pkey', '-in', str(key), '-pubout'], check=True, capture_output=True, text=True).stdout)
        lines = []
        for name in ['PCRemote-9.8.7-win-x64-setup.exe', 'PCRemote-9.8.7-osx-arm64.zip', 'PCRemote-9.8.7-linux-x64.tar.gz']:
            (work / name).write_bytes(name.encode() * 10)
            lines.append(f'{hashlib.sha256(name.encode() * 10).hexdigest()}  {name}')
        (work / 'SHA256SUMS.txt').write_text('\n'.join(lines) + '\n')
        parser = build_parser()
        create(parser.parse_args(['create', '--version', '9.8.7', '--assets', str(work), '--summary', 'Self-test release.']))
        sign_with(key, work / 'update.json', public)
        verify(parser.parse_args(['verify', '--manifest', str(work / 'update.json'), '--keys', str(public)]))
        print('PASS: create, sign and verify round trip')
        original = (work / 'update.json').read_bytes()
        (work / 'update.json').write_bytes(original.replace(b'Self-test', b'Self-tesT'))
        expect_failure(lambda: verify(parser.parse_args(['verify', '--manifest', str(work / 'update.json'), '--keys', str(public)])), 'a modified manifest')
        (work / 'update.json').write_bytes(original)
        other = subprocess.run([openssl(), 'ecparam', '-name', 'prime256v1', '-genkey', '-noout'], check=True, capture_output=True).stdout
        other_pub = subprocess.run([openssl(), 'ec', '-pubout'], input=other, check=True, capture_output=True).stdout
        (work / 'other.pub').write_bytes(other_pub)
        expect_failure(lambda: verify(parser.parse_args(['verify', '--manifest', str(work / 'update.json'), '--keys', str(work / 'other.pub')])), 'a signature from an untrusted key')
        tampered = json.loads(original)
        tampered['releaseUrl'] = 'https://example.com/'
        expect_failure(lambda: validate(json.dumps(tampered).encode()), 'a foreign release URL')
        tampered = json.loads(original)
        tampered['tag'] = 'v9.8.8'
        expect_failure(lambda: validate(json.dumps(tampered).encode()), 'a version/tag mismatch')
        expect_failure(lambda: create(parser.parse_args(['create', '--version', '9.8.7', '--assets', str(work), '--summary', 'bad\x07text', '--force'])), 'a summary with control characters')
        (work / 'PCRemote-9.8.7-osx-arm64.zip').write_bytes(b'changed')
        expect_failure(lambda: create(parser.parse_args(['create', '--version', '9.8.7', '--assets', str(work), '--summary', 'x', '--force'])), 'a download that differs from SHA256SUMS.txt')
    public_keys(PUBLIC_KEYS)
    print('PASS: committed update-signing-keys.pub contains a public key')


def build_parser():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest='command', required=True)
    command = commands.add_parser('keygen'); command.add_argument('--key', default=str(DEFAULT_KEY)); command.add_argument('--force', action='store_true')
    command = commands.add_parser('create')
    command.add_argument('--version', required=True); command.add_argument('--assets', required=True)
    command.add_argument('--summary', required=True); command.add_argument('--security', action='store_true')
    command.add_argument('--published'); command.add_argument('--out'); command.add_argument('--force', action='store_true')
    command = commands.add_parser('sign')
    command.add_argument('--manifest', required=True); command.add_argument('--key', default=str(DEFAULT_KEY))
    command.add_argument('--keys', default=str(PUBLIC_KEYS))
    command = commands.add_parser('verify')
    command.add_argument('--manifest', required=True); command.add_argument('--signature'); command.add_argument('--keys', default=str(PUBLIC_KEYS))
    command = commands.add_parser('protect'); command.add_argument('--key', default=str(DEFAULT_KEY))
    commands.add_parser('selftest')
    return parser


def main():
    args = build_parser().parse_args()
    {'keygen': keygen, 'create': create, 'sign': sign, 'verify': verify, 'protect': protect, 'selftest': selftest}[args.command](args)


if __name__ == '__main__':
    main()
