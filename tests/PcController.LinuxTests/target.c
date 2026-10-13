/* Test-only disposable GTK target on the script's isolated Xvfb display. */
#include <gtk/gtk.h>
#include <gdk/gdkx.h>
#include <stdio.h>
#include <stdlib.h>
static GtkWidget *window, *editor;
static const char *report;
static int buttons, modifiers, clicks, motions, scrolls, doubles, rights;
static gboolean event(GtkWidget *widget, GdkEvent *e, gpointer unused) {
    (void)widget; (void)unused;
    if (e->type == GDK_BUTTON_PRESS || e->type == GDK_2BUTTON_PRESS) { clicks++; buttons |= 1 << e->button.button; }
    if (e->type == GDK_2BUTTON_PRESS) doubles++;
    if (e->type == GDK_BUTTON_PRESS && e->button.button == 3) rights++;
    if (e->type == GDK_BUTTON_RELEASE) buttons &= ~(1 << e->button.button);
    if (e->type == GDK_MOTION_NOTIFY) motions++;
    if (e->type == GDK_SCROLL) scrolls++;
    if (e->type == GDK_KEY_PRESS || e->type == GDK_KEY_RELEASE) {
        int flag = e->key.keyval == GDK_KEY_Shift_L ? 1 : e->key.keyval == GDK_KEY_Control_L ? 2 : e->key.keyval == GDK_KEY_Alt_L ? 4 : 0;
        if (e->type == GDK_KEY_PRESS) modifiers |= flag; else modifiers &= ~flag;
    }
    return FALSE;
}
static gboolean snapshot(gpointer unused) {
    (void)unused;
    GtkTextBuffer *buffer = gtk_text_view_get_buffer(GTK_TEXT_VIEW(editor));
    GtkTextIter start, end; gtk_text_buffer_get_bounds(buffer, &start, &end);
    char *text = gtk_text_buffer_get_text(buffer, &start, &end, TRUE);
    char *encoded = g_base64_encode((guchar *)text, strlen(text));
    GdkWindow *native = gtk_widget_get_window(window);
    Display *display = GDK_WINDOW_XDISPLAY(native);
    Window root, child; int rx, ry, wx, wy; unsigned mask = 0;
    XQueryPointer(display, GDK_WINDOW_XID(native), &root, &child, &rx, &ry, &wx, &wy, &mask);
    buttons = ((mask & Button1Mask) ? 2 : 0) | ((mask & Button3Mask) ? 8 : 0);
    modifiers = ((mask & ShiftMask) ? 1 : 0) | ((mask & ControlMask) ? 2 : 0) | ((mask & Mod1Mask) ? 4 : 0);
    char *data = g_strdup_printf("%s\n%d\n%d\n%d\n%d\n%d\n%d\n%d\n%d\n%d\n", encoded, buttons, modifiers, clicks, motions, scrolls, doubles, rights, wx, wy);
    char *temp = g_strconcat(report, ".tmp", NULL);
    g_file_set_contents(temp, data, -1, NULL); rename(temp, report);
    g_free(temp); g_free(data); g_free(encoded); g_free(text);
    return G_SOURCE_CONTINUE;
}
int main(int argc, char **argv) {
    if (argc != 2 || !getenv("PC_REMOTE_ISOLATED_XVFB")) return 2;
    report = argv[1]; gtk_init(&argc, &argv);
    window = gtk_window_new(GTK_WINDOW_TOPLEVEL); gtk_window_set_default_size(GTK_WINDOW(window), 600, 400);
    editor = gtk_text_view_new(); gtk_container_add(GTK_CONTAINER(window), editor);
    gtk_widget_add_events(editor, GDK_BUTTON_PRESS_MASK | GDK_BUTTON_RELEASE_MASK | GDK_POINTER_MOTION_MASK | GDK_SCROLL_MASK | GDK_KEY_PRESS_MASK | GDK_KEY_RELEASE_MASK);
    g_signal_connect(editor, "event", G_CALLBACK(event), NULL);
    g_signal_connect(window, "destroy", G_CALLBACK(gtk_main_quit), NULL);
    gtk_widget_show_all(window); gtk_widget_grab_focus(editor);
    GdkWindow *native = gtk_widget_get_window(window);
    Display *display = GDK_WINDOW_XDISPLAY(native); Window xid = GDK_WINDOW_XID(native);
    XSetInputFocus(display, xid, RevertToParent, CurrentTime); XWarpPointer(display, None, xid, 0, 0, 0, 0, 200, 200); XSync(display, False);
    printf("%lu\n", xid); fflush(stdout);
    g_timeout_add(20, snapshot, NULL); gtk_main(); return 0;
}
