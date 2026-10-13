/* Included into the bridge: uses its observed desktop and armed generation. */
#include <gio/gdesktopappinfo.h>
#include <pulse/pulseaudio.h>
#include <math.h>

int pc_linux_feature_ready(void) { return pc_linux_ready() && atomic_load(&armed)==atomic_load(&revision); }
static int feature_can_power(int action) {
    const char *method=action==0?"CanSuspend":action==1?"CanReboot":action==2?"CanPowerOff":NULL;
    if(!method || !session_ready() || !pc_linux_observed()) return 0;
    GVariant *r=call(system_bus,"org.freedesktop.login1","/org/freedesktop/login1","org.freedesktop.login1.Manager",method,NULL,G_VARIANT_TYPE("(s)"));
    if(!r) return 0;
    const char *permission; g_variant_get(r,"(&s)",&permission); int yes=!strcmp(permission,"yes"); g_variant_unref(r); return yes;
}
static int feature_power(int action) {
    if(!pc_linux_feature_ready() || !feature_can_power(action)) return 0;
    const char *method=action==0?"Suspend":action==1?"Reboot":action==2?"PowerOff":NULL;
    if(!method || !pc_linux_feature_ready()) return 0;
    // No interactive polkit escalation, inhibitor override or forced termination.
    GVariant *r=call(system_bus,"org.freedesktop.login1","/org/freedesktop/login1","org.freedesktop.login1.Manager",method,g_variant_new("(b)",FALSE),G_VARIANT_TYPE_UNIT);
    if(!r) return 0;
    g_variant_unref(r); return 1;
}

typedef struct { pa_mainloop *loop; pa_context *context; char *sink; uint32_t index; pa_cvolume volume; int mute, found, done, ok; } Audio;
static void server_info(pa_context *c,const pa_server_info *i,void *data) { (void)c; Audio *a=data; if(i && i->default_sink_name) a->sink=g_strdup(i->default_sink_name); a->done=1; }
static void sink_info(pa_context *c,const pa_sink_info *i,int end,void *data) {
    (void)c; Audio *a=data; if(i) { a->index=i->index; a->volume=i->volume; a->mute=i->mute; a->found=1; } if(end) a->done=1;
}
static void audio_success(pa_context *c,int success,void *data) { (void)c; Audio *a=data; a->ok=success; a->done=1; }
static int audio_wait(Audio *a,pa_operation *operation) {
    if(!operation) return 0;
    gint64 deadline=g_get_monotonic_time()+1500000;
    while(!a->done && PA_CONTEXT_IS_GOOD(pa_context_get_state(a->context)) && g_get_monotonic_time()<deadline) { pa_mainloop_iterate(a->loop,0,NULL); g_usleep(1000); }
    int result=a->done; if(pa_operation_get_state(operation)==PA_OPERATION_RUNNING) pa_operation_cancel(operation); pa_operation_unref(operation); return result;
}
// Native PulseAudio client also supports GNOME's PipeWire-Pulse server. Never run pactl/shell.
static int feature_volume(int mode,int requested,int *level,int *muted) {
    if(mode < -1 || mode>1 || (mode==0 && (requested<0 || requested>100)) || (mode>=0 && !pc_linux_feature_ready())) return 0;
    Audio a={0}; int result=0; a.loop=pa_mainloop_new(); if(!a.loop) return 0;
    a.context=pa_context_new(pa_mainloop_get_api(a.loop),"PC Remote"); if(!a.context) goto finish;
    if(pa_context_connect(a.context,NULL,PA_CONTEXT_NOAUTOSPAWN,NULL)<0) goto finish;
    gint64 deadline=g_get_monotonic_time()+1500000;
    while(pa_context_get_state(a.context)!=PA_CONTEXT_READY && PA_CONTEXT_IS_GOOD(pa_context_get_state(a.context)) && g_get_monotonic_time()<deadline) { pa_mainloop_iterate(a.loop,0,NULL); g_usleep(1000); }
    if(pa_context_get_state(a.context)!=PA_CONTEXT_READY) goto finish;
    if(!audio_wait(&a,pa_context_get_server_info(a.context,server_info,&a)) || !a.sink) goto finish;
    a.done=0;
    if(!audio_wait(&a,pa_context_get_sink_info_by_name(a.context,a.sink,sink_info,&a)) || !a.found || !pa_cvolume_valid(&a.volume)) goto finish;
    if(mode>=0) {
        if(!pc_linux_feature_ready()) goto finish;
        if(mode==0) {
            // Preserve relative channel balance; the percentage is the loudest channel, capped at 100%.
            pa_cvolume_scale(&a.volume,(pa_volume_t)lround(requested*PA_VOLUME_NORM/100.0)); a.done=0; a.ok=0;
            if(!audio_wait(&a,pa_context_set_sink_volume_by_index(a.context,a.index,&a.volume,audio_success,&a)) || !a.ok) goto finish;
        }
        a.done=0; a.ok=0;
        if(!pc_linux_feature_ready() || !audio_wait(&a,pa_context_set_sink_mute_by_index(a.context,a.index,mode==1?!a.mute:0,audio_success,&a)) || !a.ok) goto finish;
        a.done=0; a.found=0;
        if(!audio_wait(&a,pa_context_get_sink_info_by_index(a.context,a.index,sink_info,&a)) || !a.found) goto finish;
    }
    *level=(int)lround(fmin(100,pa_cvolume_max(&a.volume)*100.0/PA_VOLUME_NORM)); *muted=a.mute; result=1;
finish:
    if(a.context) { pa_context_disconnect(a.context); pa_context_unref(a.context); }
    pa_mainloop_free(a.loop); g_free(a.sink); return result;
}

static int feature_media(int action) {
    const char *method=action==0?"PlayPause":action==1?"Next":action==2?"Previous":NULL;
    const char *capability=action==0?"CanPause":action==1?"CanGoNext":action==2?"CanGoPrevious":NULL;
    if(!method || !pc_linux_feature_ready()) return 0;
    GVariant *r=call(bus,"org.freedesktop.DBus","/org/freedesktop/DBus","org.freedesktop.DBus","ListNames",NULL,G_VARIANT_TYPE("(as)"));
    if(!r) return 0;
    gchar **names; g_variant_get(r,"(^as)",&names); g_variant_unref(r);
    char *selected=NULL,*playing=NULL; int count=0, active=0, inspected=0, selected_can=0, playing_can=0; gint64 deadline=g_get_monotonic_time()+1500000;
    // Bind to the unique owner so a player cannot be replaced between inspection and invocation.
    for(int i=0;names[i];i++) {
        if(!g_str_has_prefix(names[i],"org.mpris.MediaPlayer2.")) continue;
        if(++inspected>16 || g_get_monotonic_time()>deadline) { active=2; break; }
        GVariant *owner=call(bus,"org.freedesktop.DBus","/org/freedesktop/DBus","org.freedesktop.DBus","GetNameOwner",g_variant_new("(s)",names[i]),G_VARIANT_TYPE("(s)"));
        if(!owner) continue;
        const char *unique; g_variant_get(owner,"(&s)",&unique);
        GVariant *props=call(bus,unique,"/org/mpris/MediaPlayer2","org.freedesktop.DBus.Properties","GetAll",g_variant_new("(s)","org.mpris.MediaPlayer2.Player"),G_VARIANT_TYPE("(a{sv})"));
        if(props) {
            GVariant *values=g_variant_get_child_value(props,0); gboolean control=FALSE,can=FALSE,play=FALSE; const char *status=NULL;
            g_variant_lookup(values,"CanControl","b",&control); g_variant_lookup(values,capability,"b",&can); g_variant_lookup(values,"CanPlay","b",&play); g_variant_lookup(values,"PlaybackStatus","&s",&status);
            if(control) {
                int is_playing=status && !strcmp(status,"Playing");
                int supported=can && (action!=0 || is_playing || play);
                count++; g_free(selected); selected=g_strdup(unique); selected_can=supported;
                if(is_playing) { active++; g_free(playing); playing=g_strdup(unique); playing_can=supported; }
            }
            g_variant_unref(values); g_variant_unref(props);
        }
        g_variant_unref(owner);
    }
    g_strfreev(names);
    const char *target=active==1 && playing_can?playing:active==0 && count==1 && selected_can?selected:NULL;
    int ok=0;
    if(target && pc_linux_feature_ready()) { GVariant *sent=call(bus,target,"/org/mpris/MediaPlayer2","org.mpris.MediaPlayer2.Player",method,NULL,G_VARIANT_TYPE_UNIT); if(sent) { g_variant_unref(sent); ok=1; } }
    g_free(selected); g_free(playing); return ok;
}

static GDesktopAppInfo *checked_app(const char *path) {
    if(!g_path_is_absolute(path) || !g_str_has_suffix(path,".desktop")) return NULL;
    GDesktopAppInfo *app=g_desktop_app_info_new_from_filename(path); if(!app) return NULL;
    if(g_desktop_app_info_get_boolean(app,"Terminal") || g_desktop_app_info_get_boolean(app,"DBusActivatable") || g_desktop_app_info_get_is_hidden(app)) goto deny;
    const char *line=g_app_info_get_commandline(G_APP_INFO(app)); gint argc=0; gchar **argv=NULL; GError *error=NULL;
    if(!line || !g_shell_parse_argv(line,&argc,&argv,&error)) { g_clear_error(&error); goto deny; }
    char *executable=argc?g_find_program_in_path(argv[0]):NULL;
    int fd=executable?open(executable,O_RDONLY|O_CLOEXEC): -1; unsigned char magic[4]={0};
    int elf=fd>=0 && read(fd,magic,4)==4 && !memcmp(magic,"\177ELF",4); if(fd>=0) close(fd);
    const char *base=executable?strrchr(executable,'/'):NULL; base=base?base+1:executable;
    const char *forbidden[]={"sh","bash","dash","zsh","fish","env","python","python3","perl","ruby","node","lua","busybox","sudo","pkexec","flatpak","snap",NULL};
    int allowed=elf;
    for(int i=1;i<argc;i++) if(strcmp(argv[i],"%f") && strcmp(argv[i],"%F") && strcmp(argv[i],"%u") && strcmp(argv[i],"%U") && strcmp(argv[i],"--started-from-file")) allowed=0;
    for(int i=0;forbidden[i];i++) if(base && (!strcmp(base,forbidden[i]) || (g_str_has_prefix(base,"python") && g_ascii_isdigit(base[6])))) allowed=0;
    g_free(executable); g_strfreev(argv);
    if(allowed) return app;
deny:
    g_object_unref(app); return NULL;
}
int pc_linux_app_valid(const char *path) { GDesktopAppInfo *app=checked_app(path); if(!app) return 0; g_object_unref(app); return 1; }
static int feature_launch(const char *path) {
    if(!pc_linux_feature_ready()) return 0;
    GDesktopAppInfo *app=checked_app(path); if(!app) return 0;
    GAppLaunchContext *context=g_app_launch_context_new(); GError *error=NULL;
    int ok=pc_linux_feature_ready() && g_app_info_launch(G_APP_INFO(app),NULL,context,&error);
    g_clear_error(&error); g_object_unref(context); g_object_unref(app); return ok;
}

// Native calls may outlive the HTTP ownership lock (power especially). Teardown waits
// for their bounded dispatch/readiness checks rather than freeing a live D-Bus pointer.
int pc_linux_can_power(int action) { g_rec_mutex_lock(&feature_gate); int r=feature_can_power(action); g_rec_mutex_unlock(&feature_gate); return r; }
int pc_linux_power(int action) { g_rec_mutex_lock(&feature_gate); int r=feature_power(action); g_rec_mutex_unlock(&feature_gate); return r; }
int pc_linux_volume(int mode,int requested,int *level,int *muted) { g_rec_mutex_lock(&feature_gate); int r=feature_volume(mode,requested,level,muted); g_rec_mutex_unlock(&feature_gate); return r; }
int pc_linux_media(int action) { g_rec_mutex_lock(&feature_gate); int r=feature_media(action); g_rec_mutex_unlock(&feature_gate); return r; }
int pc_linux_launch(const char *path) { g_rec_mutex_lock(&feature_gate); int r=feature_launch(path); g_rec_mutex_unlock(&feature_gate); return r; }
