#!/usr/bin/env python3
"""Sign an installed PC Remote.app with a local, per-Mac code-signing identity.

Release archives carry only an ad-hoc seal, whose designated requirement is the build's cdhash, so
macOS treats every update as a different app: the Accessibility entry stops matching ("Failed to
match existing code requirement"). Signing the installed bundle with one self-signed identity that
this Mac keeps gives every build the same designated requirement (identifier + certificate hash),
so the Accessibility entry survives updates. The login keychain's partition list still names each
build's cdhash (apps without an Apple Team ID get no teamid: partition), so "Allow Keychain access
after update…" remains one dialog per build.

This is a local convenience for the Mac the identity was created on. It is not a Developer ID,
notarization, Gatekeeper or distribution signature, and a locally signed bundle must never be
uploaded as a release asset. The identity's private key lives in a dedicated file-based keychain
whose random password is stored in the login keychain; any process running as this user can use
it, which is the same boundary as the installation data and the ad-hoc seal (see
docs/macos-setup.md, "Storage and recovery").
"""
import argparse
import getpass
import os
from pathlib import Path
import plistlib
import secrets
import socket
import subprocess
import sys
import tempfile

IDENTITY = 'PC Remote local signing'
BUNDLE_ID = 'local.pc-remote.desktop.dev'
KEYCHAIN = Path.home() / 'Library/Keychains/pc-remote-local-signing.keychain-db'
LOGIN_KEYCHAIN = Path.home() / 'Library/Keychains/login.keychain-db'
PASSWORD_SERVICE = 'local.pc-remote.signing.keychain'
SECURITY = '/usr/bin/security'
CODESIGN = '/usr/bin/codesign'
OPENSSL = '/usr/bin/openssl'
DEFAULT_APP = Path('/Applications/PC Remote.app')


def run(argv, capture=False, check=True):
    result = subprocess.run(argv, check=check, text=True, capture_output=capture)
    return result.stdout if capture else None


def fail(message):
    raise SystemExit('sign-macos-local: ' + message)


def account():
    return getpass.getuser()


def stored_password():
    result = subprocess.run([SECURITY, 'find-generic-password', '-s', PASSWORD_SERVICE, '-a', account(), '-w', str(LOGIN_KEYCHAIN)],
                            text=True, capture_output=True)
    return result.stdout.strip() if result.returncode == 0 and result.stdout.strip() else None


def create_identity():
    if KEYCHAIN.exists():
        fail(f'{KEYCHAIN} exists but its password is not in the login keychain. Run with --remove, then sign again to create a fresh identity.')
    password = secrets.token_urlsafe(32)
    # The password only ever reaches the login keychain item and the security tool.
    run([SECURITY, 'add-generic-password', '-s', PASSWORD_SERVICE, '-a', account(), '-w', password, '-U',
         '-j', 'Password of the PC Remote local code-signing keychain', str(LOGIN_KEYCHAIN)])
    run([SECURITY, 'create-keychain', '-p', password, str(KEYCHAIN)])
    run([SECURITY, 'set-keychain-settings', str(KEYCHAIN)])  # no automatic lock; unlocked explicitly before each signing
    with tempfile.TemporaryDirectory(prefix='pc-remote-signing-') as directory:
        os.chmod(directory, 0o700)
        key, certificate, bundle = (Path(directory) / name for name in ('key.pem', 'certificate.pem', 'identity.p12'))
        subject = f'/CN={IDENTITY}/O=PC Remote local identity/OU={socket.gethostname()}'
        run([OPENSSL, 'req', '-x509', '-newkey', 'rsa:3072', '-sha256', '-days', '3650', '-nodes', '-keyout', str(key), '-out', str(certificate),
             '-subj', subject, '-addext', 'keyUsage=critical,digitalSignature', '-addext', 'extendedKeyUsage=critical,codeSigning',
             '-addext', 'basicConstraints=critical,CA:false'], capture=True)
        # The PKCS#12 transport passphrase only protects this temporary file for the import below.
        transport = secrets.token_urlsafe(24)
        subprocess.run([OPENSSL, 'pkcs12', '-export', '-out', str(bundle), '-inkey', str(key), '-in', str(certificate), '-name', IDENTITY,
                        '-passout', 'env:PC_REMOTE_P12'], check=True, text=True, capture_output=True, env=dict(os.environ, PC_REMOTE_P12=transport))
        run([SECURITY, 'import', str(bundle), '-k', str(KEYCHAIN), '-P', transport, '-T', CODESIGN], capture=True)
        allow_codesign(password)
        for path in (key, bundle):  # Best-effort overwrite before the temporary directory is removed.
            with path.open('r+b') as handle:
                handle.write(os.urandom(path.stat().st_size))
    if IDENTITY not in run([SECURITY, 'find-identity', '-p', 'codesigning', str(KEYCHAIN)], capture=True):
        fail('The identity was not created.')
    print(f'Created the local identity "{IDENTITY}" in {KEYCHAIN}.')
    return password


def unlock(password):
    run([SECURITY, 'unlock-keychain', '-p', password, str(KEYCHAIN)])
    allow_codesign(password)


def allow_codesign(password):
    # Without the partition list, codesign cannot use the key outside an interactive approval
    # dialog and fails with errSecInternalComponent. Idempotent, so it also repairs older keychains.
    run([SECURITY, 'set-key-partition-list', '-S', 'apple-tool:,apple:,codesign:', '-s', '-k', password, str(KEYCHAIN)], capture=True)


def identity_hash():
    for line in run([SECURITY, 'find-identity', '-p', 'codesigning', str(KEYCHAIN)], capture=True).splitlines():
        parts = line.split()
        if len(parts) >= 2 and f'"{IDENTITY}"' in line:
            return parts[1]
    fail('The identity is missing from its keychain.')


def running(app):
    executable = str(app / 'Contents/MacOS/PcController.Desktop')
    result = subprocess.run(['/usr/bin/pgrep', '-f', executable], text=True, capture_output=True)
    return result.returncode == 0


def check_bundle(app):
    info = app / 'Contents/Info.plist'
    if not info.is_file():
        fail(f'{app} is not an application bundle.')
    with info.open('rb') as handle:
        plist = plistlib.load(handle)
    if plist.get('CFBundleIdentifier') != BUNDLE_ID:
        fail(f'{app} is not PC Remote ({plist.get("CFBundleIdentifier")}).')
    if not (app / 'Contents/MacOS/PcController.Desktop').is_file():
        fail('The bundle has no PcController.Desktop executable.')
    return plist.get('CFBundleShortVersionString', '?')


def sign(app):
    version = check_bundle(app)
    if running(app):
        fail('Quit PC Remote first; a running bundle cannot be re-signed safely.')
    password = stored_password()
    if password is None:
        password = create_identity()
    elif not KEYCHAIN.exists():
        fail(f'The login keychain holds a password but {KEYCHAIN} is missing. Run with --remove, then sign again.')
    unlock(password)
    # Same sealing flags as scripts/package-macos.py, with the local identity instead of the ad-hoc seal.
    # Quarantine and other extended attributes are left as they are; this is not a Gatekeeper bypass.
    run([CODESIGN, '--force', '--deep', '--sign', IDENTITY, '--keychain', str(KEYCHAIN), str(app)])
    run([CODESIGN, '--verify', '--deep', '--strict', str(app)])
    requirement = run([CODESIGN, '-d', '-r-', str(app)], capture=True).strip().splitlines()[-1]
    # codesign names the self-signed certificate "leaf" or "root" depending on chain resolution; both hash the same certificate.
    leaf = identity_hash().lower()
    if not any(f'certificate {part} = H"{leaf}"' in requirement for part in ('leaf', 'root')) or f'identifier "{BUNDLE_ID}"' not in requirement:
        fail('Unexpected designated requirement after signing: ' + requirement)
    print(f'Signed PC Remote {version} at {app}')
    print('Designated requirement (stays the same for every build signed here): ' + requirement.removeprefix('designated => '))
    print('Next: open PC Remote. The first build signed with this identity needs its Accessibility entry removed (−) and added again (+);')
    print('later builds signed here keep it. "Allow Keychain access after update…" is still needed once per build (partition list by cdhash).')
    print('Never upload a locally signed bundle; release assets stay ad-hoc sealed from scripts/package-macos.py.')


def remove():
    removed = False
    if KEYCHAIN.exists():
        run([SECURITY, 'delete-keychain', str(KEYCHAIN)])
        removed = True
    if stored_password() is not None:
        run([SECURITY, 'delete-generic-password', '-s', PASSWORD_SERVICE, '-a', account(), str(LOGIN_KEYCHAIN)], capture=True)
        removed = True
    print('Removed the local signing identity.' if removed else 'No local signing identity exists.')
    print('Bundles signed with it keep working; the next build signed by a new identity needs its Accessibility entry again.')


def main():
    if sys.platform != 'darwin':
        fail('macOS only.')
    if os.environ.get('CI') == 'true':
        fail('Local identities are never created on CI runners; release assets stay ad-hoc sealed.')
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('app', nargs='?', type=Path, default=DEFAULT_APP, help=f'Bundle to sign (default: {DEFAULT_APP}).')
    parser.add_argument('--remove', action='store_true', help='Delete the local identity, its keychain and the stored password.')
    args = parser.parse_args()
    if args.remove:
        remove()
        return
    sign(args.app.resolve())


if __name__ == '__main__':
    main()
