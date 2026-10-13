"""Private-bus protocol fixture. No compositor, permission dialog, keyring or input injection."""
import json
from pathlib import Path
import sys
import os
import gi
gi.require_version('Gio', '2.0')
from gi.repository import Gio, GLib

scratch = Path(sys.argv[1])
root = '/org/freedesktop/portal/desktop'
name = 'org.freedesktop.portal.Desktop'
interface = 'org.freedesktop.portal.RemoteDesktop'
xml = '''<node><interface name="org.freedesktop.portal.RemoteDesktop">
<property name="version" type="u" access="read"/><property name="AvailableDeviceTypes" type="u" access="read"/>
<method name="CreateSession"><arg type="a{sv}" direction="in"/><arg type="o" direction="out"/></method>
<method name="SelectDevices"><arg type="o" direction="in"/><arg type="a{sv}" direction="in"/><arg type="o" direction="out"/></method>
<method name="Start"><arg type="o" direction="in"/><arg type="s" direction="in"/><arg type="a{sv}" direction="in"/><arg type="o" direction="out"/></method>
<method name="NotifyPointerMotion"><arg type="o" direction="in"/><arg type="a{sv}" direction="in"/><arg type="d" direction="in"/><arg type="d" direction="in"/></method>
<method name="NotifyPointerButton"><arg type="o" direction="in"/><arg type="a{sv}" direction="in"/><arg type="i" direction="in"/><arg type="u" direction="in"/></method>
<method name="NotifyPointerAxisDiscrete"><arg type="o" direction="in"/><arg type="a{sv}" direction="in"/><arg type="u" direction="in"/><arg type="i" direction="in"/></method>
<method name="NotifyKeyboardKeysym"><arg type="o" direction="in"/><arg type="a{sv}" direction="in"/><arg type="i" direction="in"/><arg type="u" direction="in"/></method>
</interface></node>'''
session_xml = Gio.DBusNodeInfo.new_for_xml('<node><interface name="org.freedesktop.portal.Session"><method name="Close"/><signal name="Closed"><arg type="a{sv}"/></signal></interface></node>')
bus = Gio.bus_get_sync(Gio.BusType.SESSION, None)
registrations = []
sessions = []

def mode():
    return (scratch / 'portal-mode').read_text().strip()

def callback(connection, sender, path, iface, method, parameters, invocation):
    values = parameters.unpack()
    if method == 'Close':
        invocation.return_value(GLib.Variant('()', ()))
        connection.emit_signal(None, path, 'org.freedesktop.portal.Session', 'Closed', GLib.Variant('(a{sv})', ({},)))
        return
    if method.startswith('Notify'):
        with (scratch / 'portal-events').open('a') as file:
            file.write(json.dumps([method, *values[2:]]) + '\n')
        invocation.return_value(GLib.Variant('()', ()))
        return
    options = values[-1]
    token = options['handle_token']
    sender_path = sender[1:].replace('.', '_')
    request = '/org/freedesktop/portal/desktop/request/' + sender_path + '/' + token
    result = {}
    if method == 'CreateSession':
        session = '/org/freedesktop/portal/desktop/session/' + sender_path + '/' + options['session_handle_token']
        registrations.append(connection.register_object(session, session_xml.interfaces[0], callback, None, None))
        sessions.append(session)
        result['session_handle'] = GLib.Variant('s', session)
    if method == 'SelectDevices' and options['types'] != 3:
        raise RuntimeError('Expected pointer and keyboard only')
    if method == 'Start':
        result['devices'] = GLib.Variant('u', 1 if mode() == 'partial' else 3)
    response = 1 if method == 'Start' and mode() == 'denied' else 0
    invocation.return_value(GLib.Variant('(o)', (request,)))
    def respond():
        connection.emit_signal(sender, request, 'org.freedesktop.portal.Request', 'Response', GLib.Variant('(ua{sv})', (response, result)))
        return False
    if method == "Start" and mode() == "pending":
        (scratch / "portal-pending").write_text("pending")
    else:
        GLib.idle_add(respond)

info = Gio.DBusNodeInfo.new_for_xml(xml)
registrations.append(bus.register_object(root, info.interfaces[0], callback, lambda *args: GLib.Variant('u', 3 if args[-1] == 'AvailableDeviceTypes' else 2), None))
for bus_name in [name, 'org.freedesktop.impl.portal.desktop.gnome']:
    bus.call_sync('org.freedesktop.DBus', '/org/freedesktop/DBus', 'org.freedesktop.DBus', 'RequestName', GLib.Variant('(su)', (bus_name, 0)), None, Gio.DBusCallFlags.NONE, 1000, None)

# Fake media/logind services: all names live on the private fixture bus, never the host bus.
feature_xml = Gio.DBusNodeInfo.new_for_xml('''<node>
<interface name="org.freedesktop.login1.Manager">
<method name="GetSessionByPID"><arg type="u" direction="in"/><arg type="o" direction="out"/></method>
<method name="CanSuspend"><arg type="s" direction="out"/></method><method name="CanReboot"><arg type="s" direction="out"/></method><method name="CanPowerOff"><arg type="s" direction="out"/></method>
<method name="Suspend"><arg type="b" direction="in"/></method><method name="Reboot"><arg type="b" direction="in"/></method><method name="PowerOff"><arg type="b" direction="in"/></method>
</interface>
<interface name="org.mpris.MediaPlayer2.Player">
<property name="CanControl" type="b" access="read"/><property name="CanPause" type="b" access="read"/><property name="CanPlay" type="b" access="read"/><property name="CanGoNext" type="b" access="read"/><property name="CanGoPrevious" type="b" access="read"/><property name="PlaybackStatus" type="s" access="read"/>
<method name="PlayPause"/><method name="Next"/><method name="Previous"/>
</interface></node>''')

def feature_method(connection, sender, path, iface, method, parameters, invocation):
    if method == 'GetSessionByPID': invocation.return_value(GLib.Variant('(o)', ('/org/freedesktop/login1/session/fixture',))); return
    if method.startswith('Can'):
        permission = (scratch / 'power-permission').read_text().strip() if (scratch / 'power-permission').exists() else 'yes'
        invocation.return_value(GLib.Variant('(s)', (permission,))); return
    if method == 'PlayPause' and (scratch / 'mpris-slow').exists():
        (scratch / 'feature-inflight').write_text('inflight')
        GLib.timeout_add(300, lambda: (invocation.return_value(GLib.Variant('()', ())), False)[1])
        return
    with (scratch / 'feature-events').open('a') as file: file.write(json.dumps([method, *parameters.unpack()]) + '\n')
    invocation.return_value(GLib.Variant('()', ()))

def feature_property(connection, sender, path, iface, prop):
    if prop == 'PlaybackStatus': return GLib.Variant('s', 'Playing')
    denied = (scratch / 'mpris-deny-next').exists() and prop == 'CanGoNext'
    return GLib.Variant('b', not denied)

registrations.append(bus.register_object('/org/freedesktop/login1', feature_xml.interfaces[0], feature_method, None, None))
registrations.append(bus.register_object('/org/mpris/MediaPlayer2', feature_xml.interfaces[1], feature_method, feature_property, None))
for bus_name in ['org.freedesktop.login1', 'org.freedesktop.NetworkManager', 'org.mpris.MediaPlayer2.pc_remote_disposable']:
    bus.call_sync('org.freedesktop.DBus', '/org/freedesktop/DBus', 'org.freedesktop.DBus', 'RequestName', GLib.Variant('(su)', (bus_name, 0)), None, Gio.DBusCallFlags.NONE, 1000, None)


secondary = Gio.DBusConnection.new_for_address_sync(os.environ['DBUS_SESSION_BUS_ADDRESS'], Gio.DBusConnectionFlags.AUTHENTICATION_CLIENT | Gio.DBusConnectionFlags.MESSAGE_BUS_CONNECTION, None, None)
secondary.register_object('/org/mpris/MediaPlayer2', feature_xml.interfaces[1], feature_method,
    lambda connection, sender, path, iface, prop: GLib.Variant('s', 'Playing' if (scratch / 'mpris-secondary-playing').exists() else 'Paused') if prop == 'PlaybackStatus' else GLib.Variant('b', True), None)
secondary_owned = False

(scratch / 'portal-ready').write_text('ready')

def closure():
    global secondary_owned
    requested = (scratch / 'mpris-secondary').exists()
    if requested != secondary_owned:
        secondary.call_sync('org.freedesktop.DBus', '/org/freedesktop/DBus', 'org.freedesktop.DBus', 'RequestName' if requested else 'ReleaseName', GLib.Variant('(su)', ('org.mpris.MediaPlayer2.pc_remote_secondary', 0)) if requested else GLib.Variant('(s)', ('org.mpris.MediaPlayer2.pc_remote_secondary',)), None, Gio.DBusCallFlags.NONE, 1000, None)
        secondary_owned = requested
        (scratch / 'mpris-secondary-ready').write_text(str(requested))
    marker = scratch / 'portal-close'
    if marker.exists():
        marker.unlink()
        for session in sessions:
            bus.emit_signal(None, session, 'org.freedesktop.portal.Session', 'Closed', GLib.Variant('(a{sv})', ({},)))
    return True
GLib.timeout_add(20, closure)
GLib.MainLoop().run()
