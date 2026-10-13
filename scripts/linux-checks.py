#!/usr/bin/env python3
"""Isolated Xvfb/GTK or encrypted disposable GNOME keyring checks; never the user's bus/display."""
import argparse
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import time

ROOT = Path(__file__).resolve().parent.parent

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=['linux-x11', 'linux-portal', 'linux-storage'])
    parser.add_argument('--configuration', default='Release')
    parser.add_argument('--inside', action='store_true')
    parser.add_argument('--scratch')
    args = parser.parse_args()
    if sys.platform != 'linux' or os.geteuid() == 0:
        raise RuntimeError('Run as an unprivileged Linux user. No input or keyring changes made.')
    dotnet = str(ROOT / '.tools/dotnet/dotnet') if (ROOT / '.tools/dotnet/dotnet').is_file() else shutil.which('dotnet')
    if not args.inside:
        subprocess.run([dotnet, 'build', 'tests/PcController.LinuxTests', '-c', args.configuration], cwd=ROOT, check=True)
        with tempfile.TemporaryDirectory(prefix='pc-remote-linux-check-') as scratch:
            env = os.environ.copy()
            for name in ['DBUS_SESSION_BUS_ADDRESS', 'DISPLAY', 'WAYLAND_DISPLAY', 'XDG_SESSION_ID', 'XDG_SESSION_TYPE']:
                env.pop(name, None)
            for name, child in [('HOME', 'home'), ('XDG_DATA_HOME', 'home/.local/share'), ('XDG_CONFIG_HOME', 'home/.config'), ('XDG_RUNTIME_DIR', 'run')]:
                directory = Path(scratch) / child; directory.mkdir(parents=True, exist_ok=True, mode=0o700); env[name] = str(directory)
            subprocess.run(['dbus-run-session', '--', sys.executable, __file__, args.mode, '--inside', '--scratch', scratch, '--configuration', args.configuration], env=env, cwd=ROOT, check=True)
        return
    scratch = Path(args.scratch)
    processes = []
    env = os.environ.copy()
    env['PC_REMOTE_LINUX_CHECK_DIRECTORY'] = str(scratch)
    if sys.version_info >= (3, 13):
        env['PC_REMOTE_LINUX_STRICT_PYTHON'] = sys.executable
        env['PC_REMOTE_LINUX_TLS_SCRIPT'] = str(ROOT / 'tests/certificate-trust.py')
    try:
        if args.mode == 'linux-x11':
            flags = subprocess.check_output(['pkg-config', '--cflags', '--libs', 'gtk+-3.0', 'x11'], text=True).split()
            subprocess.run(['cc', '-Wall', '-Wextra', '-Werror', str(ROOT / 'tests/PcController.LinuxTests/target.c'), '-o', str(scratch / 'target'), *flags], check=True)
            read_fd, write_fd = os.pipe()
            xvfb = subprocess.Popen(['Xvfb', '-displayfd', str(write_fd), '-screen', '0', '1024x768x24', '-nolisten', 'tcp'], pass_fds=[write_fd])
            processes.append(xvfb); os.close(write_fd)
            with os.fdopen(read_fd) as pipe: display = pipe.readline().strip()
            if not display: raise RuntimeError('Private Xvfb failed')
            env['DISPLAY'] = ':' + display; env['PC_REMOTE_ISOLATED_XVFB'] = '1'
            pulse_socket = scratch / 'pulse.sock'
            pulse = subprocess.Popen(['pulseaudio', '-n', '--daemonize=no', '--use-pid-file=no', '--exit-idle-time=-1', '--log-target=stderr',
                '--load=module-null-sink sink_name=pc_remote_disposable', '--load=module-native-protocol-unix socket=' + str(pulse_socket) + ' auth-anonymous=1'], env=env)
            processes.append(pulse)
            deadline = time.monotonic() + 10
            while not pulse_socket.exists():
                if pulse.poll() is not None or time.monotonic() > deadline: raise RuntimeError('Private null-sink audio server unavailable')
                time.sleep(.02)
            env['PULSE_SERVER'] = 'unix:' + str(pulse_socket)
            flag = '--x11' 
        elif args.mode == 'linux-portal':
            (scratch / 'portal-mode').write_text('success')
            env['DBUS_SYSTEM_BUS_ADDRESS'] = env['DBUS_SESSION_BUS_ADDRESS']  # Fake logind shares ONLY this private bus.
            process = subprocess.Popen(['/usr/bin/python3', str(ROOT / 'tests/PcController.LinuxTests/portal-fixture.py'), str(scratch)], env=env)
            processes.append(process)
            deadline = time.monotonic() + 10
            while not (scratch / 'portal-ready').is_file():
                if process.poll() is not None or time.monotonic() > deadline: raise RuntimeError('Private portal protocol fixture unavailable')
                time.sleep(.02)
            flag = '--portal'
        else:
            # Disposable password sent by stdin, never a user credential or command argument.
            keyring = subprocess.Popen(['gnome-keyring-daemon', '--foreground', '--components=secrets', '--unlock'], stdin=subprocess.PIPE, env=env)
            processes.append(keyring); keyring.stdin.write(b'disposable-check-password\n'); keyring.stdin.close()
            deadline = time.monotonic() + 10
            file = Path(env['XDG_DATA_HOME']) / 'keyrings/login.keyring'
            while not file.is_file():
                if keyring.poll() is not None or time.monotonic() > deadline: raise RuntimeError('Disposable encrypted login keyring unavailable')
                time.sleep(.05)
            flag = '--storage'
        dll = ROOT / 'tests/PcController.LinuxTests/bin' / args.configuration / 'net10.0/PcController.LinuxTests.dll'
        layouts = ['us', 'fr'] if args.mode == 'linux-x11' else [None]
        for layout in layouts:
            if layout:
                subprocess.run(['setxkbmap', '-layout', layout], env=env, check=True)
                print('Isolated X11 keyboard layout: ' + layout, flush=True)
            subprocess.run([dotnet, str(dll), flag], env=env, cwd=ROOT, check=True)
        if args.mode == 'linux-storage':
            subprocess.run([dotnet, str(dll), '--storage-restart'], env=env, cwd=ROOT, check=True)
            subprocess.run(['gdbus', 'call', '--session', '--dest', 'org.freedesktop.secrets', '--object-path', '/org/freedesktop/secrets', '--method', 'org.freedesktop.Secret.Service.Lock', "['/org/freedesktop/secrets/collection/login']"], env=env, stdout=subprocess.DEVNULL, check=True)
            subprocess.run([dotnet, str(dll), '--probe-fails'], env=env, cwd=ROOT, check=True)
            keyring.terminate(); keyring.wait(timeout=5)
            keyring = subprocess.Popen(['gnome-keyring-daemon', '--foreground', '--components=secrets', '--unlock'], stdin=subprocess.PIPE, env=env)
            processes.append(keyring); keyring.stdin.write(b'disposable-check-password\n'); keyring.stdin.close()
            deadline = time.monotonic() + 10
            while True:
                owner = subprocess.check_output(['gdbus', 'call', '--session', '--dest', 'org.freedesktop.DBus', '--object-path', '/org/freedesktop/DBus', '--method', 'org.freedesktop.DBus.NameHasOwner', 'org.freedesktop.secrets'], env=env, text=True)
                if 'true' in owner: break
                if time.monotonic() > deadline or keyring.poll() is not None: raise RuntimeError('Disposable provider restart unavailable')
                time.sleep(.02)
            subprocess.run([dotnet, str(dll), '--storage-unlocked'], env=env, cwd=ROOT, check=True)
            import hashlib
            import re
            installation = hashlib.sha256(str(Path(env['HOME']) / '.local/share/pc-remote-test').encode()).hexdigest().upper()
            result = subprocess.check_output(['gdbus', 'call', '--session', '--dest', 'org.freedesktop.secrets', '--object-path', '/org/freedesktop/secrets', '--method', 'org.freedesktop.Secret.Service.SearchItems', "{'installation': '" + installation + "'}"], env=env, text=True)
            items = re.findall(r"/org/freedesktop/secrets/collection/login/[A-Za-z0-9_]+", result)
            if len(items) != 1: raise RuntimeError('Expected exactly one disposable installation key')
            subprocess.run(['gdbus', 'call', '--session', '--dest', 'org.freedesktop.secrets', '--object-path', items[0], '--method', 'org.freedesktop.Secret.Item.Delete'], env=env, stdout=subprocess.DEVNULL, check=True)
            subprocess.run([dotnet, str(dll), '--probe-fails'], env=env, cwd=ROOT, check=True)
            # Replace only the disposable provider file to check plaintext-format refusal.
            file.write_text('[keyring]\ndisplay-name=Disposable plaintext fixture\n')
            subprocess.run([dotnet, str(dll), '--probe-fails'], env=env, cwd=ROOT, check=True)
    finally:
        for process in reversed(processes):
            if process.poll() is None:
                process.terminate()
                try: process.wait(timeout=5)
                except subprocess.TimeoutExpired: process.kill(); process.wait()

if __name__ == '__main__':
    main()
