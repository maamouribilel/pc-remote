/* Authored Linux bridge. Public XCB/XTEST, GDBus portal/logind and libsecret APIs. */
#include <gio/gio.h>
#include <libsecret/secret.h>
#include <xcb/xcb.h>
#include <xcb/xtest.h>
#include <xkbcommon/xkbcommon-x11.h>
#include <stdatomic.h>
#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>

#define PORTAL "org.freedesktop.portal.Desktop"
#define RD "org.freedesktop.portal.RemoteDesktop"
#define ROOT "/org/freedesktop/portal/desktop"
static GDBusConnection *bus, *system_bus;
static GMainContext *context;
static GMainLoop *loop;
static GThread *thread;
static xcb_connection_t *display;
static struct xkb_context *keyboard;
static int device, backend;
static char *session_path, *portal_path;
static GMutex portal_mutex;
static GRecMutex feature_gate; // Serialize native feature calls with backend teardown.
static atomic_int portal_live, interactive, initialized, stopped, sleeping, cancel_consent;
static atomic_uint_fast64_t revision = 1, armed;
static void (*unsafe_callback)(void);
static guint subscriptions[10];
#ifdef PC_REMOTE_TESTING
static xcb_window_t guard_window; /* Test-only X11 disposable target, never set by production. */
static atomic_int test_mode;
#endif
static unsigned wheel_held;

static GVariant *empty(void) { return g_variant_new_array(G_VARIANT_TYPE("{sv}"), NULL, 0); }
static GVariant *call(GDBusConnection *connection, const char *name, const char *path,
                      const char *interface, const char *method, GVariant *args, const GVariantType *type) {
    if (!connection || g_dbus_connection_is_closed(connection)) return NULL;
    GError *error = NULL;
    GVariant *reply = g_dbus_connection_call_sync(connection, name, path, interface, method, args,
                                                  type, G_DBUS_CALL_FLAGS_NONE, 1500, NULL, &error);
    g_clear_error(&error);
    return reply;
}
static void changed(void) {
    atomic_store(&armed, 0);
    atomic_fetch_add(&revision, 1);
    if (unsafe_callback) unsafe_callback();
}
static int session_ready(void) {
#ifdef PC_REMOTE_TESTING
    if (atomic_load(&test_mode)) return 1;
#endif
    if (!session_path || !system_bus || atomic_load(&sleeping)) return 0;
    GVariant *r = call(system_bus, "org.freedesktop.login1", session_path, "org.freedesktop.DBus.Properties", "GetAll",
                        g_variant_new("(s)", "org.freedesktop.login1.Session"), G_VARIANT_TYPE("(a{sv})"));
    if (!r) return 0;
    GVariant *props; gboolean active = FALSE, locked = TRUE, remote = TRUE;
    const char *type = NULL, *desktop = NULL;
    g_variant_get(r, "(@a{sv})", &props);
    g_variant_lookup(props, "Active", "b", &active);
    g_variant_lookup(props, "LockedHint", "b", &locked);
    g_variant_lookup(props, "Remote", "b", &remote);
    g_variant_lookup(props, "Type", "&s", &type);
    g_variant_lookup(props, "Desktop", "&s", &desktop);
    int gnome = desktop && (g_strrstr(desktop, "GNOME") || !g_ascii_strcasecmp(desktop, "gnome") || !g_ascii_strcasecmp(desktop, "ubuntu"));
    int correct_backend = type && !strcmp(type, backend == 1 ? "x11" : "wayland");
    g_variant_unref(props); g_variant_unref(r);
    if (!(active && !locked && !remote && correct_backend && gnome)) return 0;
    GVariant *screen = call(bus, "org.gnome.ScreenSaver", "/org/gnome/ScreenSaver", "org.gnome.ScreenSaver", "GetActive", NULL, G_VARIANT_TYPE("(b)"));
    gboolean screen_locked = TRUE;
    if (screen) { g_variant_get(screen, "(b)", &screen_locked); g_variant_unref(screen); }
    return !screen_locked;
}
// NetworkManager reports access-point scans, signal strength, link bitrate and traffic
// counters as PropertiesChanged every few seconds. They are not connectivity changes, so
// they must not pause control or revoke LAN approval. Address, gateway, SSID and BSSID
// changes are still detected by the managed interface identity check.
static int network_noise(const char *path, const char *signal, GVariant *params) {
    if (strcmp(signal, "PropertiesChanged") || !g_str_has_prefix(path, "/org/freedesktop/NetworkManager") ||
        !g_variant_is_of_type(params, G_VARIANT_TYPE("(sa{sv}as)"))) return 0;
    const char *name; GVariant *properties, *invalidated;
    g_variant_get(params, "(&s@a{sv}@as)", &name, &properties, &invalidated);
    int noise = !strcmp(name, "org.freedesktop.NetworkManager.AccessPoint") || !strcmp(name, "org.freedesktop.NetworkManager.Device.Statistics");
    if (!noise && !strcmp(name, "org.freedesktop.NetworkManager.Device.Wireless") && g_variant_n_children(invalidated) == 0) {
        noise = 1;
        GVariantIter iter; const char *key; GVariant *value;
        g_variant_iter_init(&iter, properties);
        while (g_variant_iter_next(&iter, "{&sv}", &key, &value)) {
            if (strcmp(key, "Bitrate") && strcmp(key, "LastScan") && strcmp(key, "AccessPoints")) noise = 0;
            g_variant_unref(value);
        }
    }
    g_variant_unref(properties); g_variant_unref(invalidated);
    return noise;
}
static void signal_changed(GDBusConnection *c, const char *sender, const char *path,
                           const char *iface, const char *signal, GVariant *params, void *data) {
    (void)c; (void)sender; (void)iface; (void)data;
    if (network_noise(path, signal, params)) return;
    if (!strcmp(signal, "Closed")) {
        g_mutex_lock(&portal_mutex);
        int current = portal_path && !strcmp(path, portal_path);
        if (current) atomic_store(&portal_live, 0);
        g_mutex_unlock(&portal_mutex);
        if (!current) return; // A late old-session signal cannot invalidate a replacement.
    }
    if (!strcmp(signal, "NameOwnerChanged")) {
        const char *name, *old, *new_owner;
        g_variant_get(params, "(&s&s&s)", &name, &old, &new_owner);
        (void)old; (void)new_owner;
        if (strcmp(name, PORTAL) && strcmp(name, "org.freedesktop.secrets") && strcmp(name, "org.freedesktop.NetworkManager") && strcmp(name, "org.freedesktop.login1")) return;
        if (!strcmp(name, PORTAL)) atomic_store(&portal_live, 0);
    }
    if (!strcmp(signal, "PrepareForSleep")) {
        gboolean preparing; g_variant_get(params, "(b)", &preparing); atomic_store(&sleeping, preparing);
    }
    // Any lock/session/suspend/network signal disarms before managed ownership cleanup.
    atomic_store(&interactive, session_ready());
    changed();
}
static guint subscribe(GDBusConnection *c, const char *name, const char *iface, const char *signal, const char *path) {
    return g_dbus_connection_signal_subscribe(c, name, iface, signal, path, NULL,
                                               G_DBUS_SIGNAL_FLAGS_NONE, signal_changed, NULL, NULL);
}
static gboolean poll_ready(void *data) {
    (void)data;
    int ready = session_ready();
    if (display && xcb_connection_has_error(display)) ready = 0;
    if (bus && g_dbus_connection_is_closed(bus)) { ready = 0; atomic_store(&portal_live, 0); }
    if (atomic_exchange(&interactive, ready) != ready) changed();
    return G_SOURCE_CONTINUE;
}
static void *observe(void *unused) {
    (void)unused;
    g_main_context_push_thread_default(context);
    GError *error = NULL;
    bus = g_bus_get_sync(G_BUS_TYPE_SESSION, NULL, &error); g_clear_error(&error);
    system_bus = g_bus_get_sync(G_BUS_TYPE_SYSTEM, NULL, &error); g_clear_error(&error);
    if (bus) {
        subscriptions[0] = subscribe(bus, PORTAL, "org.freedesktop.portal.Session", "Closed", NULL);
        subscriptions[9] = subscribe(bus, "org.freedesktop.secrets", "org.freedesktop.DBus.Properties", "PropertiesChanged", NULL);
        subscriptions[8] = subscribe(bus, "org.gnome.ScreenSaver", "org.gnome.ScreenSaver", "ActiveChanged", NULL);
        subscriptions[1] = subscribe(bus, "org.freedesktop.DBus", "org.freedesktop.DBus", "NameOwnerChanged", NULL);
    }
    if (system_bus) {
        GVariant *r = call(system_bus, "org.freedesktop.login1", "/org/freedesktop/login1", "org.freedesktop.login1.Manager", "GetSessionByPID",
                            g_variant_new("(u)", (guint)getpid()), G_VARIANT_TYPE("(o)"));
        // Apps started from GNOME Shell, a terminal or autostart run in systemd user scopes
        // outside any logind session. "auto" then resolves to the user's display session,
        // which session_ready() still requires to be local, active, unlocked and GNOME.
        if (!r) r = call(system_bus, "org.freedesktop.login1", "/org/freedesktop/login1", "org.freedesktop.login1.Manager", "GetSession",
                         g_variant_new("(s)", "auto"), G_VARIANT_TYPE("(o)"));
        if (r) { g_variant_get(r, "(o)", &session_path); g_variant_unref(r); }
        subscriptions[2] = subscribe(system_bus, "org.freedesktop.login1", "org.freedesktop.DBus.Properties", "PropertiesChanged", session_path);
        subscriptions[3] = subscribe(system_bus, "org.freedesktop.login1", "org.freedesktop.login1.Manager", "PrepareForSleep", NULL);
        subscriptions[4] = subscribe(system_bus, "org.freedesktop.login1", "org.freedesktop.login1.Session", NULL, session_path);
        subscriptions[5] = subscribe(system_bus, "org.freedesktop.NetworkManager", "org.freedesktop.DBus.Properties", "PropertiesChanged", NULL);
        subscriptions[6] = subscribe(system_bus, "org.freedesktop.NetworkManager", "org.freedesktop.NetworkManager", NULL, NULL);
        subscriptions[7] = subscribe(system_bus, "org.freedesktop.DBus", "org.freedesktop.DBus", "NameOwnerChanged", NULL);
    }
    GSource *timer = g_timeout_source_new(250);
    g_source_set_callback(timer, poll_ready, NULL, NULL); g_source_attach(timer, context); g_source_unref(timer);
    atomic_store(&interactive, session_ready()); atomic_store(&initialized, 1);
    g_main_loop_run(loop);
    for (int i = 0; i < 10; i++) if (subscriptions[i]) g_dbus_connection_signal_unsubscribe(i < 2 || i >= 8 ? bus : system_bus, subscriptions[i]);
    g_main_context_pop_thread_default(context);
    return NULL;
}
int pc_linux_open(int selected, void (*callback)(void)) {
    if (geteuid() == 0 || thread || (selected != 1 && selected != 2)) return 0;
    backend = selected; unsafe_callback = callback;
    memset(subscriptions, 0, sizeof subscriptions); atomic_store(&cancel_consent, 0);
#ifdef PC_REMOTE_TESTING
    atomic_store(&test_mode, 0); guard_window = 0;
#endif
    if (backend == 1) {
        int screen;
        display = xcb_connect(NULL, &screen);
        if (xcb_connection_has_error(display)) return 0;
        const xcb_query_extension_reply_t *ext = xcb_get_extension_data(display, &xcb_test_id);
        if (!ext || !ext->present) return 0;
        uint16_t major = XKB_X11_MIN_MAJOR_XKB_VERSION, minor = XKB_X11_MIN_MINOR_XKB_VERSION;
        if (!xkb_x11_setup_xkb_extension(display, major, minor, XKB_X11_SETUP_XKB_EXTENSION_NO_FLAGS, NULL, NULL, NULL, NULL)) return 0;
        keyboard = xkb_context_new(XKB_CONTEXT_NO_FLAGS);
        device = xkb_x11_get_core_keyboard_device_id(display);
        if (!keyboard || device < 0) return 0;
    }
    context = g_main_context_new(); loop = g_main_loop_new(context, FALSE);
    atomic_store(&initialized, 0); atomic_store(&stopped, 0);
    thread = g_thread_new("pc-linux-observer", observe, NULL);
    while (!atomic_load(&initialized)) g_usleep(1000);
    GVariant *network = call(system_bus, "org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "NameHasOwner", g_variant_new("(s)", "org.freedesktop.NetworkManager"), G_VARIANT_TYPE("(b)"));
    gboolean network_observed = FALSE;
    if (network) { g_variant_get(network, "(b)", &network_observed); g_variant_unref(network); }
    return bus && system_bus && session_path && network_observed;
}
uint64_t pc_linux_generation(void) { return atomic_load(&revision); }
int pc_linux_observed(void) {
    if (!bus || !system_bus || !session_path || g_dbus_connection_is_closed(bus) || g_dbus_connection_is_closed(system_bus)) return 0;
    const char *names[] = {"org.freedesktop.NetworkManager", "org.freedesktop.login1"};
    for (int i = 0; i < 2; i++) {
        GVariant *r = call(system_bus, "org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "NameHasOwner", g_variant_new("(s)", names[i]), G_VARIANT_TYPE("(b)"));
        gboolean available = FALSE;
        if (r) { g_variant_get(r, "(b)", &available); g_variant_unref(r); }
        if (!available) return 0;
    }
    return 1;
}
int pc_linux_ready(void) {
#ifdef PC_REMOTE_TESTING
    if (!atomic_load(&test_mode) && !pc_linux_observed()) return 0;
#else
    if (!pc_linux_observed()) return 0;
#endif
    if (!session_ready() || !atomic_load(&interactive) || atomic_load(&stopped)) return 0;
    return backend == 1 ? display && !xcb_connection_has_error(display) : atomic_load(&portal_live);
}
int pc_linux_arm(uint64_t current) {
    if (!pc_linux_ready() || current != atomic_load(&revision)) return 0;
    atomic_store(&armed, current);
    if (current != atomic_load(&revision)) { atomic_store(&armed, 0); return 0; }
    return 1;
}

/* Subscribe before sending portal requests using the specified handle_token path. */
typedef struct { GMutex mutex; GCond condition; GVariant *results; int done; guint response; } Request;
static void response(GDBusConnection *c, const char *sender, const char *path, const char *iface,
                     const char *signal, GVariant *params, void *data) {
    (void)c; (void)sender; (void)path; (void)iface; (void)signal;
    Request *r = data;
    g_mutex_lock(&r->mutex);
    g_variant_get(params, "(u@a{sv})", &r->response, &r->results);
    r->done = 1; g_cond_signal(&r->condition); g_mutex_unlock(&r->mutex);
}
static GVariant *request(const char *method, const char *session) {
    Request *r = g_new0(Request, 1); g_mutex_init(&r->mutex); g_cond_init(&r->condition);
    char *uuid = g_uuid_string_random(); for (char *p = uuid; *p; p++) if (*p == '-') *p = '_';
    char *token = g_strconcat("pc_", uuid, NULL); g_free(uuid);
    char *sender = g_strdup(g_dbus_connection_get_unique_name(bus) + 1);
    for (char *p = sender; *p; p++) if (*p == '.') *p = '_';
    char *path = g_strdup_printf("/org/freedesktop/portal/desktop/request/%s/%s", sender, token); g_free(sender);
    // This call must run on a thread with its own thread-default context; iterate it below.
    GMainContext *request_context = g_main_context_new(); g_main_context_push_thread_default(request_context);
    guint sub = g_dbus_connection_signal_subscribe(bus, PORTAL, "org.freedesktop.portal.Request", "Response", path, NULL, G_DBUS_SIGNAL_FLAGS_NONE, response, r, NULL);
    GVariantBuilder options; g_variant_builder_init(&options, G_VARIANT_TYPE_VARDICT);
    g_variant_builder_add(&options, "{sv}", "handle_token", g_variant_new_string(token));
    if (!strcmp(method, "CreateSession")) g_variant_builder_add(&options, "{sv}", "session_handle_token", g_variant_new_string(token));
    if (!strcmp(method, "SelectDevices")) g_variant_builder_add(&options, "{sv}", "types", g_variant_new_uint32(3));
    GVariant *args = !strcmp(method, "CreateSession") ? g_variant_new("(a{sv})", &options) :
                    !strcmp(method, "Start") ? g_variant_new("(osa{sv})", session, "", &options) : g_variant_new("(oa{sv})", session, &options);
    GVariant *reply = call(bus, PORTAL, ROOT, RD, method, args, G_VARIANT_TYPE("(o)"));
    gint64 deadline = g_get_monotonic_time() + 60000000;
    while (reply && !r->done && !atomic_load(&cancel_consent) && g_get_monotonic_time() < deadline) {
        while (g_main_context_iteration(request_context, FALSE)) { }
        g_usleep(1000);
    }
    if (!r->done) {
        GVariant *closed = call(bus, PORTAL, path, "org.freedesktop.portal.Request", "Close", NULL, NULL);
        if (closed) g_variant_unref(closed);
    }
    if (reply) g_variant_unref(reply);
    g_dbus_connection_signal_unsubscribe(bus, sub);
    // Drain queued callbacks before releasing request userdata.
    while (g_main_context_iteration(request_context, FALSE)) { }
    g_main_context_pop_thread_default(request_context); g_main_context_unref(request_context);
    GVariant *results = r->done && r->response == 0 ? g_steal_pointer(&r->results) : NULL;
    if (r->results) g_variant_unref(r->results);
    g_mutex_clear(&r->mutex); g_cond_clear(&r->condition); g_free(r); g_free(path); g_free(token);
    return results;
}
static void close_portal(void) {
    atomic_store(&portal_live, 0);
    g_mutex_lock(&portal_mutex);
    char *old_path = g_steal_pointer(&portal_path);
    g_mutex_unlock(&portal_mutex);
    if (old_path && bus) {
        GVariant *r = call(bus, PORTAL, old_path, "org.freedesktop.portal.Session", "Close", NULL, NULL);
        if (r) g_variant_unref(r);
    }
    g_free(old_path);
}
void pc_linux_cancel_consent(void) { atomic_store(&cancel_consent, 1); atomic_store(&armed, 0); }
int pc_linux_consent(void) {
    if (atomic_load(&cancel_consent) || backend != 2 || !bus || !session_ready()) return 0;
    close_portal(); changed();
    GVariant *r = call(bus, PORTAL, ROOT, "org.freedesktop.DBus.Properties", "GetAll", g_variant_new("(s)", RD), G_VARIANT_TYPE("(a{sv})"));
    if (!r) return 0;
    GVariant *props; guint types = 0, version = 0;
    g_variant_get(r, "(@a{sv})", &props); g_variant_lookup(props, "AvailableDeviceTypes", "u", &types); g_variant_lookup(props, "version", "u", &version);
    g_variant_unref(props); g_variant_unref(r);
    GVariant *provider = call(bus, "org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "NameHasOwner", g_variant_new("(s)", "org.freedesktop.impl.portal.desktop.gnome"), G_VARIANT_TYPE("(b)"));
    gboolean supported = FALSE;
    if (provider) { g_variant_get(provider, "(b)", &supported); g_variant_unref(provider); }
    if (!supported) return 0;
    if (version < 1 || (types & 3) != 3) return 0;
    r = request("CreateSession", NULL);
    if (!r) return 0;
    const char *handle = NULL;
    if (g_variant_lookup(r, "session_handle", "&s", &handle) && g_variant_is_object_path(handle)) {
        g_mutex_lock(&portal_mutex); portal_path = g_strdup(handle); g_mutex_unlock(&portal_mutex);
    }
    g_variant_unref(r);
    if (!portal_path) return 0;
    r = request("SelectDevices", portal_path);
    if (!r) { close_portal(); return 0; } g_variant_unref(r);
    r = request("Start", portal_path);
    guint granted = 0;
    if (r) { g_variant_lookup(r, "devices", "u", &granted); g_variant_unref(r); }
    if ((granted & 3) != 3 || atomic_load(&cancel_consent) || !session_ready()) { close_portal(); return 0; }
    atomic_store(&portal_live, 1); changed(); return 1;
}
int pc_linux_resolve(uint32_t symbol) {
    if (backend == 2) return symbol <= INT32_MAX ? (int)symbol : 0; /* NotifyKeyboardKeysym transport. */
    if (!display || xcb_connection_has_error(display)) return 0;
    struct xkb_keymap *map = xkb_x11_keymap_new_from_device(keyboard, display, device, XKB_KEYMAP_COMPILE_NO_FLAGS);
    if (!map) return 0;
    struct xkb_state *state = xkb_x11_state_new_from_device(map, display, device);
    int result = 0;
    if (state) {
        xkb_layout_index_t layout = xkb_state_serialize_layout(state, XKB_STATE_LAYOUT_EFFECTIVE);
        for (xkb_keycode_t key = xkb_keymap_min_keycode(map); key <= xkb_keymap_max_keycode(map); key++) {
            const xkb_keysym_t *syms;
            // Logical key location in the active layout; no keymap mutation or group switch.
            int count = xkb_keymap_key_get_syms_by_level(map, key, layout, 0, &syms);
            if (count == 1 && syms[0] == symbol) { result = (int)key; break; }
            count = xkb_keymap_key_get_syms_by_level(map, key, layout, 1, &syms);
            if (count == 1 && syms[0] == symbol && !result) result = (int)key | 0x10000;
        }
        xkb_state_unref(state);
    }
    xkb_keymap_unref(map); return result;
}
static int check_cookie(xcb_void_cookie_t cookie) {
    xcb_generic_error_t *error = xcb_request_check(display, cookie);
    int ok = !error && !xcb_connection_has_error(display); free(error); return ok;
}
static int focus_guard(int type, int down, double dx, double dy) {
#ifdef PC_REMOTE_TESTING
    if (!guard_window || ((type == 2 || type == 3) && !down)) return 1;
    xcb_get_input_focus_reply_t *focus = xcb_get_input_focus_reply(display, xcb_get_input_focus(display), NULL);
    int ok = focus && focus->focus == guard_window; free(focus);
    if (!ok) return 0;
    if (type != 3) {
        xcb_query_pointer_reply_t *p = xcb_query_pointer_reply(display, xcb_query_pointer(display, guard_window), NULL);
        xcb_get_geometry_reply_t *g = xcb_get_geometry_reply(display, xcb_get_geometry(display, guard_window), NULL);
        ok = p && g && p->win_x >= 0 && p->win_y >= 0 && p->win_x < g->width && p->win_y < g->height;
        if (ok && type == 1) ok = p->win_x + dx >= 0 && p->win_y + dy >= 0 && p->win_x + dx < g->width && p->win_y + dy < g->height;
        free(p); free(g);
    }
    return ok;
#else
    (void)type; (void)down; (void)dx; (void)dy;
    return 1;
#endif
}
int pc_linux_event(int type, int code, double x, double y, int down) {
    if (type == 5) {
        if (!wheel_held) return 1;
        if (!display || xcb_connection_has_error(display)) return 0;
        int success = 1;
        for (uint8_t b = 4; b <= 7; b++) if (wheel_held & (1u << b)) {
            if (check_cookie(xcb_test_fake_input_checked(display, XCB_BUTTON_RELEASE, b, 0, XCB_NONE, 0, 0, 0))) wheel_held &= ~(1u << b);
            else success = 0;
        }
        return success;
    }
    int release = (type == 2 || type == 3) && !down;
    if (atomic_load(&stopped) || (!release && (!pc_linux_ready() || atomic_load(&armed) != atomic_load(&revision)))) return 0;
    if (backend == 1) {
        if (!display || xcb_connection_has_error(display) || !focus_guard(type, down, x, y)) return 0;
        if (type == 1) return check_cookie(xcb_test_fake_input_checked(display, XCB_MOTION_NOTIFY, 1, 0, XCB_NONE, (int16_t)x, (int16_t)y, 0));
        if (type == 2 || type == 3) return check_cookie(xcb_test_fake_input_checked(display, type == 2 ? (down ? XCB_BUTTON_PRESS : XCB_BUTTON_RELEASE) : (down ? XCB_KEY_PRESS : XCB_KEY_RELEASE), (uint8_t)code, 0, XCB_NONE, 0, 0, 0));
        if (type == 4) {
            int axes[] = {(int)x, (int)y};
            for (int a = 0; a < 2; a++) for (int i = 0; i < abs(axes[a]); i++) {
                uint8_t button = a == 0 ? (axes[a] > 0 ? 7 : 6) : (axes[a] > 0 ? 5 : 4);
                wheel_held |= 1u << button;
                int pressed = check_cookie(xcb_test_fake_input_checked(display, XCB_BUTTON_PRESS, button, 0, XCB_NONE, 0, 0, 0));
                int released = check_cookie(xcb_test_fake_input_checked(display, XCB_BUTTON_RELEASE, button, 0, XCB_NONE, 0, 0, 0));
                if (released) wheel_held &= ~(1u << button);
                if (!pressed || !released) return 0;
            }
            return 1;
        }
        return 0;
    }
    // Releases can run on other threads while consent/close replaces the portal session;
    // use a private copy so a concurrent close_portal() cannot free the path mid-call.
    g_mutex_lock(&portal_mutex);
    char *session = atomic_load(&portal_live) && portal_path ? g_strdup(portal_path) : NULL;
    g_mutex_unlock(&portal_mutex);
    if (!session) return 0;
    const char *method; GVariant *args;
    if (type == 1) { method = "NotifyPointerMotion"; args = g_variant_new("(o@a{sv}dd)", session, empty(), x, y); }
    else if (type == 2) { method = "NotifyPointerButton"; args = g_variant_new("(o@a{sv}iu)", session, empty(), code == 1 ? 272 : 273, (guint)down); }
    else if (type == 3) { method = "NotifyKeyboardKeysym"; args = g_variant_new("(o@a{sv}iu)", session, empty(), code, (guint)down); }
    else if (type == 4) {
        int ok = 1;
        if (x) { GVariant *r = call(bus, PORTAL, ROOT, RD, "NotifyPointerAxisDiscrete", g_variant_new("(o@a{sv}ui)", session, empty(), 1u, (int)x), NULL); ok &= r != NULL; if (r) g_variant_unref(r); }
        if (y) { GVariant *r = call(bus, PORTAL, ROOT, RD, "NotifyPointerAxisDiscrete", g_variant_new("(o@a{sv}ui)", session, empty(), 0u, (int)y), NULL); ok &= r != NULL; if (r) g_variant_unref(r); }
        g_free(session); return ok;
    } else { g_free(session); return 0; }
    GVariant *r = call(bus, PORTAL, ROOT, RD, method, args, NULL);
    g_free(session);
    if (!r) return 0;
    g_variant_unref(r); return 1;
}
int pc_linux_forget_ended_backend(void) {
    // Only a local UI confirmation may use this after verifying physical cleanup.
    // Never clear tracking while the original input transport is still live.
    int ended = backend == 1 ? display && xcb_connection_has_error(display) : !atomic_load(&portal_live);
    if (!ended) return 0;
    atomic_store(&armed, 0); wheel_held = 0; return 1;
}
void pc_linux_close(void) {
    g_rec_mutex_lock(&feature_gate);
    atomic_store(&armed, 0); close_portal(); atomic_store(&stopped, 1);
    if (loop) g_main_loop_quit(loop);
    if (thread) g_thread_join(thread);
    thread = NULL; unsafe_callback = NULL;
    if (display) xcb_disconnect(display);
    display = NULL;
    if (keyboard) xkb_context_unref(keyboard);
    keyboard = NULL;
    g_clear_object(&bus); g_clear_object(&system_bus);
    g_clear_pointer(&session_path, g_free);
    if (loop) g_main_loop_unref(loop);
    loop = NULL;
    if (context) g_main_context_unref(context);
    context = NULL;
    g_rec_mutex_unlock(&feature_gate);
}
#ifdef PC_REMOTE_TESTING
int pc_linux_test_wayland(void) { if (backend != 2 || !bus) return 0; atomic_store(&test_mode, 1); atomic_store(&interactive, 1); return 1; }
int pc_linux_test_unfocus(void) {
    if (!guard_window || !atomic_load(&test_mode)) return 0;
    return check_cookie(xcb_set_input_focus_checked(display, XCB_INPUT_FOCUS_NONE, XCB_NONE, XCB_CURRENT_TIME));
}
/* Only the isolated Xvfb native runner calls this; production has no managed entry point. */
int pc_linux_test_x11(uint32_t window) {
    if (!window || !display || backend != 1) return 0;
    guard_window = window; atomic_store(&test_mode, 1); atomic_store(&interactive, 1); return 1;
}

#endif

static const SecretSchema schema = { .name = "local.pc-remote.storage.v1", .flags = SECRET_SCHEMA_NONE,
                                     .attributes = {{"installation", SECRET_SCHEMA_ATTRIBUTE_STRING}, {NULL, 0}} };
/* Deliberately require the reviewed GNOME provider and its encrypted persistent login file.
 * Secret Service alone says nothing about at-rest encryption or authorization isolation. */
static int provider_ready(SecretService *service) {
    g_autofree char *owner = g_dbus_proxy_get_name_owner(G_DBUS_PROXY(service));
    if (!owner) return 0;
    GDBusConnection *connection = g_dbus_proxy_get_connection(G_DBUS_PROXY(service));
    GVariant *r = call(connection, "org.freedesktop.DBus", "/org/freedesktop/DBus", "org.freedesktop.DBus", "GetConnectionUnixProcessID", g_variant_new("(s)", owner), G_VARIANT_TYPE("(u)"));
    if (!r) return 0;
    guint pid; g_variant_get(r, "(u)", &pid); g_variant_unref(r);
    char path[128], executable[4096]; snprintf(path, sizeof path, "/proc/%u/exe", pid);
    ssize_t n = readlink(path, executable, sizeof executable - 1);
    struct stat st;
    snprintf(path, sizeof path, "/proc/%u", pid);
    if (n < 0 || stat(path, &st) || st.st_uid != geteuid()) return 0;
    executable[n] = 0;
    if (strcmp(executable, "/usr/bin/gnome-keyring-daemon")) return 0;
    char *file = g_build_filename(g_get_user_data_dir(), "keyrings", "login.keyring", NULL);
    int fd = open(file, O_RDONLY | O_CLOEXEC | O_NOFOLLOW); g_free(file);
    if (fd < 0) return 0;
    unsigned char header[20];
    int valid = !fstat(fd, &st) && S_ISREG(st.st_mode) && st.st_uid == geteuid() && !(st.st_mode & 077) &&
                read(fd, header, sizeof header) == sizeof header && !memcmp(header, "GnomeKeyring\n\r\0\n", 16) &&
                header[16] == 0 && header[17] == 0 && header[18] == 0 && header[19] == 0;
    close(fd); return valid;
}
int pc_linux_owned_path(const char *path, int directory) {
    struct stat st;
    return !lstat(path, &st) && st.st_uid == geteuid() && (directory ? S_ISDIR(st.st_mode) : S_ISREG(st.st_mode));
}
int pc_linux_secret(const char *account, unsigned char *key, int create) {
    if (geteuid() == 0) return 1;
    GError *error = NULL;
    SecretService *service = secret_service_get_sync(SECRET_SERVICE_OPEN_SESSION, NULL, &error);
    if (!service) { g_clear_error(&error); return 2; }
    int status = 3;
    SecretCollection *collection = NULL; GList *items = NULL; GHashTable *attributes = NULL;
    if (!provider_ready(service)) goto done;
    collection = secret_collection_for_alias_sync(service, "login", SECRET_COLLECTION_NONE, NULL, &error);
    if (!collection || secret_collection_get_locked(collection)) goto done;
    // Require the actual persistent login collection, never default/session aliases or prompts.
    if (strcmp(g_dbus_proxy_get_object_path(G_DBUS_PROXY(collection)), "/org/freedesktop/secrets/collection/login")) goto done;
    attributes = secret_attributes_build(&schema, "installation", account, NULL);
    items = secret_collection_search_sync(collection, &schema, attributes, SECRET_SEARCH_ALL, NULL, &error);
    if (error || g_list_length(items) > 1) goto done;
    if (!items && create) {
        SecretValue *value = secret_value_new((const char *)key, 32, "application/octet-stream");
        SecretItem *item = secret_item_create_sync(collection, &schema, attributes, "PC Remote installation encryption key", value, SECRET_ITEM_CREATE_NONE, NULL, &error);
        secret_value_unref(value);
        if (item) items = g_list_append(NULL, item);
    }
    if (!items) { status = 4; goto done; }
    SecretItem *item = items->data;
    if (secret_item_get_locked(item) || !secret_item_load_secret_sync(item, NULL, &error)) goto done;
    SecretValue *value = secret_item_get_secret(item);
    if (!value) goto done;
    gsize length; const char *bytes = secret_value_get(value, &length);
    if (length == 32) { memcpy(key, bytes, 32); status = 0; }
    secret_value_unref(value);
    if (secret_collection_get_locked(collection) || !provider_ready(service)) status = 3;
done:
    if (attributes) g_hash_table_unref(attributes);
    g_list_free_full(items, g_object_unref);
    if (collection) g_object_unref(collection);
    g_object_unref(service); g_clear_error(&error);
    return status;
}

#include "features.inc"
