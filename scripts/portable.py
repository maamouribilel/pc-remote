#!/usr/bin/env python3
"""Portable build/check runner. Fixtures are loopback-only and test-project-owned."""
import argparse
import contextlib
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tempfile
import time

ROOT = Path(__file__).resolve().parent.parent
TOOLS = ROOT / '.tools'
WINDOWS = os.name == 'nt'
RID = ('win' if WINDOWS else 'darwin' if sys.platform == 'darwin' else 'linux') + ('-arm64' if platform.machine().lower() in ('arm64', 'aarch64') else '-x64')
NODE_VERSION = (ROOT / 'src/remote/.nvmrc').read_text().strip()
NODE_BIN = TOOLS / ('node-v' + NODE_VERSION + '-' + RID)
if not WINDOWS:
    NODE_BIN /= 'bin'
os.environ['PATH'] = str(NODE_BIN) + os.pathsep + os.environ.get('PATH', '')
os.environ['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
os.environ['AVALONIA_TELEMETRY_OPTOUT'] = '1'
os.environ['DOTNET_NOLOGO'] = '1'
os.environ['DOTNET_GENERATE_ASPNET_CERTIFICATE'] = 'false'


def executable(name, local=None):
    if local and local.is_file():
        return str(local)
    found = shutil.which(name)
    if not found:
        raise RuntimeError('Missing ' + name + '; install the pinned toolchain documented in README.md.')
    return found


DOTNET = executable('dotnet', TOOLS / 'dotnet' / ('dotnet.exe' if WINDOWS else 'dotnet'))
# Windows command scripts require cmd.exe; POSIX invokes npm directly.
NPM = None


def run(argv, cwd=ROOT, capture=False):
    if WINDOWS and str(argv[0]).lower().endswith('.cmd'):
        argv = [os.environ.get('COMSPEC', 'cmd.exe'), '/d', '/c', *argv]
    result = subprocess.run(argv, cwd=cwd, check=True, text=True, capture_output=capture)
    return result.stdout.strip() if capture else None


def frontend():
    global NPM
    NPM = executable('npm.cmd' if WINDOWS else 'npm')
    node = executable('node')
    expected_sdk = json.loads((ROOT / 'global.json').read_text())['sdk']['version']
    if run([DOTNET, '--version'], capture=True) != expected_sdk:
        raise RuntimeError('The exact global.json SDK is required.')
    engines = json.loads((ROOT / 'src/remote/package.json').read_text())['engines']
    if run([node, '--version'], capture=True) != 'v' + engines['node'] or run([NPM, '--version'], capture=True) != engines['npm']:
        raise RuntimeError('Use the exact Node/npm versions in src/remote/package.json.')


PROJECT = 'tests/PcController.PortableTests'


@contextlib.contextmanager
def fixture(configuration, platform_name="recording", mac=False):
    with tempfile.TemporaryDirectory(prefix='PcControllerPortable-') as directory:
        project = 'tests/PcController.MacOSTests' if mac else PROJECT
        run([DOTNET, 'build', project, '-c', configuration])
        dll = ROOT / project / 'bin' / configuration / ('net10.0/PcController.MacOSTests.dll' if mac else 'net10.0/PcController.PortableTests.dll')
        directory = str(Path(directory).resolve())
        arguments = ['--serve-tls', directory] if mac else ['--serve-mouse-fixture', directory, platform_name]
        process = subprocess.Popen([DOTNET, str(dll), *arguments], cwd=ROOT)
        try:
            metadata = Path(directory) / 'certificates/fixture.json'
            deadline = time.monotonic() + 30
            while not metadata.is_file():
                if process.poll() is not None or time.monotonic() > deadline:
                    raise RuntimeError('Loopback fixture failed to start; close any companion using port 8443.')
                time.sleep(.1)
            yield Path(directory)
        finally:
            (Path(directory) / 'stop').write_text('stop')
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=['build', 'test', 'tls', 'browser', 'browser-capabilities', 'remote-unit', 'desktop-test', 'desktop', 'desktop-fixture', 'linux-test', 'linux-x11', 'linux-portal', 'linux-storage', 'mac-test', 'mac-storage', 'mac-smoke', 'mac-onboard', 'mac-tls', 'audit'])
    parser.add_argument('--configuration', default='Release', choices=['Debug', 'Release'])
    args = parser.parse_args()
    if args.action == 'build':
        frontend()
        run([NPM, 'ci'], ROOT / 'src/remote')
        run([NPM, 'run', 'build'], ROOT / 'src/remote')
        run([DOTNET, 'build', 'PcController.Portable.slnx', '-c', args.configuration])
    elif args.action == 'test':
        run([DOTNET, 'run', '--project', PROJECT, '-c', args.configuration])
    elif args.action in ('mac-smoke', 'mac-onboard'):
        run([sys.executable, 'scripts/mac-smoke.py', *(['--onboard'] if args.action == 'mac-onboard' else [])])
    elif args.action == 'linux-test':
        run([DOTNET, 'run', '--project', 'tests/PcController.LinuxTests', '-c', args.configuration])
    elif args.action in ('linux-x11', 'linux-portal', 'linux-storage'):
        if sys.platform != 'linux':
            raise RuntimeError('This check requires Linux; native Wayland acceptance is a separate supervised GNOME check.')
        run([sys.executable, 'scripts/linux-checks.py', args.action, '--configuration', args.configuration])
    elif args.action in ('mac-test', 'mac-storage'):
        extra = [] if args.action == 'mac-test' else ['--', '--storage']
        run([DOTNET, 'run', '--project', 'tests/PcController.MacOSTests', '-c', args.configuration, *extra])
    elif args.action == 'desktop-test':
        run([DOTNET, 'run', '--project', 'tests/PcController.DesktopTests', '-c', args.configuration])
    elif args.action == 'desktop':
        run([DOTNET, 'run', '--project', 'src/PcController.Desktop', '-c', args.configuration])
    elif args.action == 'desktop-fixture':
        run([DOTNET, 'run', '--project', 'tests/PcController.DesktopTests', '-c', args.configuration, '--', '--show-fixture'])
    elif args.action == 'browser-capabilities':
        frontend()
        for platform_name in ['recording', 'macos', 'linux', 'unavailable']:
            with fixture(args.configuration, platform_name) as directory:
                os.environ['PC_CONTROLLER_MOUSE_FIXTURE'] = str(directory / 'certificates/fixture.json')
                os.environ['PC_CONTROLLER_TEST_PLATFORM'] = platform_name
                os.environ['PC_CONTROLLER_BROWSER_TLS_BYPASS'] = '1'
                run([NPM, 'test', '--', 'capability-compatibility.spec.ts' if platform_name == 'recording' else 'capabilities.spec.ts'], ROOT / 'src/remote')
    elif args.action in ('tls', 'mac-tls'):
        if sys.version_info < (3, 13):
            raise RuntimeError('Strict TLS acceptance requires Python 3.13+; older Python is not equivalent evidence.')
        with fixture(args.configuration, mac=args.action == 'mac-tls') as directory:
            run([sys.executable, 'tests/certificate-trust.py', str(directory / 'certificates/pc-remote-root.cer')])
    elif args.action == 'remote-unit':
        frontend()
        run([NPM, 'test', '--', 'air-math.spec.ts'], ROOT / 'src/remote')  # page-less, no fixture or browser
    elif args.action == 'browser':
        frontend()
        run([NPM, 'test', '--', 'air-math.spec.ts'], ROOT / 'src/remote')
        # Each fixture issues one single-use pairing challenge, so the Air pointer spec gets its own instance.
        for specs in (['mouse.spec.ts', 'connection.spec.ts'], ['air.spec.ts']):
            with fixture(args.configuration) as directory:
                os.environ['PC_CONTROLLER_MOUSE_FIXTURE'] = str(directory / 'certificates/fixture.json')
                os.environ['PC_CONTROLLER_BROWSER_TLS_BYPASS'] = '1'  # Explicit fixture UI mode only.
                run([NPM, 'test', '--', *specs], ROOT / 'src/remote')
    elif args.action == 'audit':
        frontend()
        run([NPM, 'audit', '--audit-level=low'], ROOT / 'src/remote')
        audit = json.loads(run([DOTNET, 'list', 'PcController.Portable.slnx', 'package', '--vulnerable', '--include-transitive', '--format', 'json'], capture=True))
        if not audit.get('projects') or audit.get('errors'):
            raise RuntimeError('NuGet audit did not return usable project results.')
        for project in audit['projects']:
            for framework in project.get('frameworks', []):
                for package in framework.get('topLevelPackages', []) + framework.get('transitivePackages', []):
                    if package.get('vulnerabilities'):
                        raise RuntimeError('NuGet advisory for ' + package['id'])
        print('Portable npm/NuGet audits: no advisories in current feeds.')


if __name__ == '__main__':
    main()
