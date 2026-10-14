// Small C ABI over Apple's public frameworks. No event tap, screen capture or root.
#import <AppKit/AppKit.h>
#import <ApplicationServices/ApplicationServices.h>
#import <Carbon/Carbon.h>
#import <Security/Security.h>
#import <SystemConfiguration/SystemConfiguration.h>
#import <CoreAudio/CoreAudio.h>
#import <IOKit/pwr_mgt/IOPMLib.h>
#import <IOKit/hidsystem/ev_keymap.h>
#import <ServiceManagement/ServiceManagement.h>
#include <stdatomic.h>
#include <unistd.h>
#include <sys/stat.h>
#include <limits.h>
#include <pthread.h>

static atomic_ulong generation;
static id layoutObserver; // Keyboard layout change observer (see cache_layout_on_main).
static void refresh_layout(void);
static void cache_layout_on_main(void);
// Advances only when the trusted LAN may have changed (network keys, system sleep/wake, session
// switch, power-off). Space and display transitions pause input via generation only.
static atomic_ulong networkGeneration;
static atomic_int inactive;
static atomic_ulong armedGeneration;
static atomic_int enforceGeneration;
static _Atomic(void (*)(void)) unsafeCallback;
static void invalidate(void) { atomic_fetch_add(&generation, 1); void (*callback)(void)=atomic_load(&unsafeCallback); if(callback) callback(); }
static void invalidate_network(void) { atomic_fetch_add(&networkGeneration, 1); invalidate(); }
unsigned long pc_network_generation(void) { return atomic_load(&networkGeneration); }
// Test hook: only ever adds a conservative pause (and, for network=1, LAN revocation).
int pc_test_notify(int network) { if(network) invalidate_network(); else invalidate(); return 1; }
int pc_arm(unsigned long expected) { if(atomic_load(&generation)!=expected) return 0; atomic_store(&armedGeneration, expected); return atomic_load(&generation)==expected; }
static SCDynamicStoreRef networkStore;
static NSMutableArray *observers;
int pc_permission(void) { return AXIsProcessTrusted() && CGPreflightPostEventAccess(); }
void pc_onboard(void) {
    NSDictionary *options = @{(__bridge NSString *)kAXTrustedCheckOptionPrompt: @YES};
    AXIsProcessTrustedWithOptions((__bridge CFDictionaryRef)options);
    [[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:@"x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility"]];
}
int pc_test_activate(int pid) { @autoreleasepool { return [[NSRunningApplication runningApplicationWithProcessIdentifier:pid] activateWithOptions:NSApplicationActivateAllWindows]; } }
int pc_frontmost(void) {
    // AX is a live IPC query. NSRunningApplication's time-varying properties can
    // remain cached until its main run loop advances (not present in CLI smoke).
    AXUIElementRef system=AXUIElementCreateSystemWide(); CFTypeRef app=NULL; pid_t pid=0;
    if(AXUIElementCopyAttributeValue(system,kAXFocusedApplicationAttribute,&app)==kAXErrorSuccess && app)
        AXUIElementGetPid((AXUIElementRef)app,&pid);
    if(app) CFRelease(app); CFRelease(system); return pid;
}
int pc_session(void) { @autoreleasepool {
    if (getuid() == 0 || atomic_load(&inactive) || IsSecureEventInputEnabled()) return 0;
    CFDictionaryRef session = CGSessionCopyCurrentDictionary();
    if (!session) return 0;
    BOOL active = [((__bridge NSDictionary *)session)[(__bridge NSString *)kCGSessionOnConsoleKey] boolValue];
    CFRelease(session);
    // Login UI and secure input are never remote targets. Poll again before every post.
    AXUIElementRef system=AXUIElementCreateSystemWide(); CFTypeRef focused=NULL;
    AXError status=AXUIElementCopyAttributeValue(system, kAXFocusedApplicationAttribute, &focused);
    pid_t pid=0; if(status==kAXErrorSuccess && focused) AXUIElementGetPid((AXUIElementRef)focused, &pid);
    if(focused) CFRelease(focused); CFRelease(system);
    NSString *focusedBundle=pid ? [NSRunningApplication runningApplicationWithProcessIdentifier:pid].bundleIdentifier : nil;
    return active && status==kAXErrorSuccess && pid>0 && ![focusedBundle isEqualToString:@"com.apple.loginwindow"];
} }
int pc_diagnostics(void) {
    int bits=(AXIsProcessTrusted()?1:0) | (CGPreflightPostEventAccess()?2:0) | (IsSecureEventInputEnabled()?8:0) | (atomic_load(&inactive)?32:0);
    CFDictionaryRef session=CGSessionCopyCurrentDictionary();
    if(session) { if([((__bridge NSDictionary *)session)[(__bridge NSString *)kCGSessionOnConsoleKey] boolValue]) bits|=4; CFRelease(session); }
    AXUIElementRef system=AXUIElementCreateSystemWide(); CFTypeRef focused=NULL;
    if(AXUIElementCopyAttributeValue(system,kAXFocusedApplicationAttribute,&focused)==kAXErrorSuccess && focused) bits|=16;
    if(focused) CFRelease(focused); CFRelease(system); return bits;
}
static void networkChanged(SCDynamicStoreRef store, CFArrayRef keys, void *info) {
    (void)store; (void)keys; (void)info; invalidate_network();
}
int pc_observe(void (*callback)(void)) {
    atomic_store(&unsafeCallback, callback);
    refresh_layout();
    if (!layoutObserver) layoutObserver = [[NSDistributedNotificationCenter defaultCenter] addObserverForName:(__bridge NSString *)kTISNotifySelectedKeyboardInputSourceChanged
        object:nil queue:[NSOperationQueue mainQueue] usingBlock:^(NSNotification *n) { (void)n; cache_layout_on_main(); }];
    if (observers) return networkStore != NULL;
    observers = [NSMutableArray array];
    atomic_store(&enforceGeneration, 1);
    NSNotificationCenter *center = [NSWorkspace sharedWorkspace].notificationCenter;
    // System sleep/wake, session switches and power-off may change the network: revoke LAN approval.
    NSArray *stop = @[NSWorkspaceWillSleepNotification, NSWorkspaceSessionDidResignActiveNotification, NSWorkspaceWillPowerOffNotification];
    NSArray *wake = @[NSWorkspaceDidWakeNotification, NSWorkspaceSessionDidBecomeActiveNotification];
    for (NSString *name in stop) [observers addObject:[center addObserverForName:name object:nil queue:nil usingBlock:^(NSNotification *n) {
        (void)n; atomic_store(&inactive, 1); invalidate_network();
    }]];
    for (NSString *name in wake) [observers addObject:[center addObserverForName:name object:nil queue:nil usingBlock:^(NSNotification *n) {
        (void)n; atomic_store(&inactive, 0); invalidate_network();
    }]];
    // Display sleep/wake pauses and releases input but keeps the session's LAN approval.
    [observers addObject:[center addObserverForName:NSWorkspaceScreensDidSleepNotification object:nil queue:nil usingBlock:^(NSNotification *n) {
        (void)n; atomic_store(&inactive, 1); invalidate();
    }]];
    [observers addObject:[center addObserverForName:NSWorkspaceScreensDidWakeNotification object:nil queue:nil usingBlock:^(NSNotification *n) {
        (void)n; atomic_store(&inactive, 0); invalidate();
    }]];
    // A Space/full-screen transition conservatively pauses control (including lock-screen transitions);
    // it does not change the network, so LAN approval is kept.
    [observers addObject:[center addObserverForName:NSWorkspaceActiveSpaceDidChangeNotification object:nil queue:nil usingBlock:^(NSNotification *n) {
        (void)n; invalidate();
    }]];
    networkStore = SCDynamicStoreCreate(NULL, CFSTR("PC Remote network lifecycle"), networkChanged, NULL);
    if (!networkStore) return 0;
    NSArray *patterns = @[@"State:/Network/.*", @"Setup:/Network/.*"];
    if (!SCDynamicStoreSetNotificationKeys(networkStore, NULL, (__bridge CFArrayRef)patterns) ||
        !SCDynamicStoreSetDispatchQueue(networkStore, dispatch_get_global_queue(QOS_CLASS_USER_INITIATED, 0))) {
        CFRelease(networkStore); networkStore = NULL; return 0;
    }
    return 1;
}
unsigned long pc_generation(void) { return atomic_load(&generation); }
void pc_unobserve(void) {
    atomic_store(&unsafeCallback, NULL);
    NSNotificationCenter *center = [NSWorkspace sharedWorkspace].notificationCenter;
    for (id observer in observers) [center removeObserver:observer];
    observers = nil;
    if (layoutObserver) { [[NSDistributedNotificationCenter defaultCenter] removeObserver:layoutObserver]; layoutObserver = nil; }
    if (networkStore) { SCDynamicStoreSetDispatchQueue(networkStore, NULL); CFRelease(networkStore); networkStore = NULL; }
}
static int inTargetWindow(int pid, CGPoint point) {
    if(!pid) return 1;
    AXUIElementRef app=AXUIElementCreateApplication(pid); CFTypeRef window=NULL, position=NULL, size=NULL;
    CGPoint p; CGSize s; BOOL valid=NO;
    if(AXUIElementCopyAttributeValue(app,kAXFocusedWindowAttribute,&window)==kAXErrorSuccess && window &&
       AXUIElementCopyAttributeValue((AXUIElementRef)window,kAXPositionAttribute,&position)==kAXErrorSuccess &&
       AXUIElementCopyAttributeValue((AXUIElementRef)window,kAXSizeAttribute,&size)==kAXErrorSuccess &&
       AXValueGetValue((AXValueRef)position,kAXValueCGPointType,&p) && AXValueGetValue((AXValueRef)size,kAXValueCGSizeType,&s))
        valid=CGRectContainsPoint(CGRectMake(p.x,p.y,s.width,s.height),point);
    if(size) CFRelease(size); if(position) CFRelease(position); if(window) CFRelease(window); CFRelease(app); return valid;
}
static int post(CGEventRef event, uint64_t flags, int guard, int release) {
    if (!event) return 0;
    // Cleanup may run in an inactive/secure session but still requires Accessibility.
    BOOL allowed = pc_permission() && (release || (pc_session() && (!atomic_load(&enforceGeneration) || atomic_load(&armedGeneration) == atomic_load(&generation)))) && (release || !guard || pc_frontmost() == guard);
    CGEventType type=CGEventGetType(event);
    BOOL pointer=type==kCGEventMouseMoved || type==kCGEventLeftMouseDragged || type==kCGEventRightMouseDragged || type==kCGEventLeftMouseDown || type==kCGEventRightMouseDown || type==kCGEventScrollWheel;
    if(allowed && guard && !release && pointer) allowed=inTargetWindow(guard,CGEventGetLocation(event));
    if (allowed) { CGEventSetFlags(event, flags); CGEventPost(kCGHIDEventTap, event); }
    CFRelease(event);
    // CGEventPost is void: success means submitted, NOT receipt by the target.
    return allowed && pc_permission();
}
static CGEventSourceRef source(void) {
    static CGEventSourceRef owned; static dispatch_once_t once;
    dispatch_once(&once, ^{ owned=CGEventSourceCreate(kCGEventSourceStatePrivate); if(owned) CGEventSourceSetLocalEventsSuppressionInterval(owned, 0); });
    // Keep one private state for the process, including held transitions across events.
    if(owned) CFRetain(owned); return owned;
}
static CGPoint location(void) { CGEventRef e = CGEventCreate(NULL); CGPoint p = e ? CGEventGetLocation(e) : CGPointMake(NAN, NAN); if (e) CFRelease(e); return p; }
void pc_pointer(double *x, double *y) { CGPoint p=location(); *x=p.x; *y=p.y; }
int pc_move(int dx, int dy, int drag, uint64_t flags, int guard) {
    CGPoint p = location(); if(!isfinite(p.x) || !isfinite(p.y)) return 0; p.x += dx; p.y += dy;
    CGDirectDisplayID displays[32]; uint32_t count = 0;
    if (CGGetActiveDisplayList(32, displays, &count) != kCGErrorSuccess || !count) return 0;
    CGPoint closest = p; double distance = INFINITY;
    for (uint32_t i = 0; i < count; i++) {
        CGRect r = CGDisplayBounds(displays[i]);
        CGPoint q = CGPointMake(fmax(CGRectGetMinX(r), fmin(p.x, CGRectGetMaxX(r) - 1)), fmax(CGRectGetMinY(r), fmin(p.y, CGRectGetMaxY(r) - 1)));
        double d = pow(q.x-p.x, 2) + pow(q.y-p.y, 2);
        if (d < distance) { closest=q; distance=d; }
    }
    CGEventSourceRef s = source(); if(!s) return 0;
    CGEventRef e = CGEventCreateMouseEvent(s, drag == 1 ? kCGEventLeftMouseDragged : drag == 2 ? kCGEventRightMouseDragged : kCGEventMouseMoved, closest, drag == 2 ? kCGMouseButtonRight : kCGMouseButtonLeft);
    if (s) CFRelease(s);
    if (e) { CGEventSetIntegerValueField(e, kCGMouseEventDeltaX, dx); CGEventSetIntegerValueField(e, kCGMouseEventDeltaY, dy); }
    // Synchronize the cursor before a following move/button reads its position;
    // posting alone queues an event and can otherwise move a click back to stale coordinates.
    BOOL allowed=pc_permission() && pc_session() && (!atomic_load(&enforceGeneration) || atomic_load(&armedGeneration)==atomic_load(&generation)) &&
        (!guard || (pc_frontmost()==guard && inTargetWindow(guard,closest)));
    if(!e || !allowed || CGWarpMouseCursorPosition(closest)!=kCGErrorSuccess) { if(e) CFRelease(e); return 0; }
    return post(e, flags, guard, 0);
}
int pc_button(int right, int down, int clicks, uint64_t flags, int guard) {
    CGEventSourceRef s=source(); if(!s) return 0;
    CGPoint p=location(); if(!isfinite(p.x) || !isfinite(p.y)) { CFRelease(s); return 0; }
    CGEventType type = right ? (down ? kCGEventRightMouseDown : kCGEventRightMouseUp) : (down ? kCGEventLeftMouseDown : kCGEventLeftMouseUp);
    CGEventRef e=CGEventCreateMouseEvent(s, type, p, right ? kCGMouseButtonRight : kCGMouseButtonLeft);
    if(s) CFRelease(s);
    if(e) CGEventSetIntegerValueField(e, kCGMouseEventClickState, clicks);
    return post(e, flags, guard, !down);
}
int pc_scroll(int x, int y, uint64_t flags, int guard) {
    CGEventSourceRef s=source(); if(!s) return 0;
    CGEventRef e=CGEventCreateScrollWheelEvent(s, kCGScrollEventUnitPixel, 2, y, -x);
    if(e) CGEventSetLocation(e,location());
    if(s) CFRelease(s); return post(e, flags, guard, 0);
}
int pc_key(unsigned short code, int down, int repeat, uint64_t flags, int guard) {
    CGEventSourceRef s=source(); if(!s) return 0; CGEventRef e=CGEventCreateKeyboardEvent(s, code, down);
    if(s) CFRelease(s);
    if(e) CGEventSetIntegerValueField(e, kCGKeyboardEventAutorepeat, repeat);
    return post(e, flags, guard, !down);
}
int pc_unicode(const unsigned short *chars, int length, int down, int guard) {
    CGEventSourceRef s=source(); if(!s) return 0; CGEventRef e=CGEventCreateKeyboardEvent(s, 0, down);
    if(s) CFRelease(s);
    if(e) CGEventKeyboardSetUnicodeString(e, length, chars);
    return post(e, 0, guard, !down);
}
static int layoutKey(TISInputSourceRef input, unsigned short character) {
    if(!input) return -1;
    CFDataRef data=TISGetInputSourceProperty(input, kTISPropertyUnicodeKeyLayoutData);
    int result=-1;
    if(data) for(UInt16 key=0; key<128; key++) {
        UniChar text[4]; UniCharCount count=0; UInt32 dead=0;
        OSStatus status=UCKeyTranslate((const UCKeyboardLayout *)CFDataGetBytePtr(data), key, kUCKeyActionDown, cmdKey >> 8, LMGetKbdType(), kUCKeyTranslateNoDeadKeysMask, &dead, 4, &count, text);
        if(status==noErr && count==1 && text[0]==character) { result=key; break; }
    }
    return result;
}
// Text Input Source (TIS) APIs assert the main queue on current macOS: an off-main call traps
// and never returns (host setup and phone shortcuts run on worker threads). Resolve the current
// layout only on the main thread, at observe time and on every layout change, and answer
// lookups from this cache on any thread. Same result as scanning keys 0-127 in order for the
// first exact single-character match (Command map with cmdKey, plain map without modifiers).
static _Atomic int commandKeys[128], plainKeys[128];
static atomic_int layoutCached;
static void cache_layout_on_main(void) {
    int command[128], plain[128];
    for(int i=0;i<128;i++) command[i]=plain[i]=-1;
    TISInputSourceRef input=TISCopyCurrentKeyboardLayoutInputSource();
    CFDataRef data=input ? TISGetInputSourceProperty(input,kTISPropertyUnicodeKeyLayoutData) : NULL;
    if(data) for(UInt16 key=0;key<128;key++) for(int pass=0;pass<2;pass++) {
        UInt32 dead=0; UniChar text[4]; UniCharCount count=0; int *map=pass ? plain : command;
        OSStatus status=UCKeyTranslate((const UCKeyboardLayout *)CFDataGetBytePtr(data),key,kUCKeyActionDown,pass ? 0 : cmdKey >> 8,LMGetKbdType(),kUCKeyTranslateNoDeadKeysMask,&dead,4,&count,text);
        if(status==noErr && count==1 && text[0]<128 && map[text[0]]<0) map[text[0]]=key;
    }
    if(input) CFRelease(input);
    for(int i=0;i<128;i++) { atomic_store(&commandKeys[i],command[i]); atomic_store(&plainKeys[i],plain[i]); }
    atomic_store(&layoutCached, data != NULL);
}
static void refresh_layout(void) {
    if([NSThread isMainThread]) cache_layout_on_main();
    else dispatch_async(dispatch_get_main_queue(), ^{ cache_layout_on_main(); });
}
static int cached_key(_Atomic int *map, unsigned short character) {
    if([NSThread isMainThread]) cache_layout_on_main();
    return character<128 && atomic_load(&layoutCached) ? atomic_load(&map[character]) : -1;
}
int pc_command_key(unsigned short character) { return cached_key(commandKeys, character); }
int pc_layout_key(unsigned short character) { return cached_key(plainKeys, character); }

int pc_test_layout_key(const char *identifier, unsigned short character) {
    @autoreleasepool {
        NSDictionary *filter=@{(__bridge NSString *)kTISPropertyInputSourceID:[NSString stringWithUTF8String:identifier]};
        CFArrayRef sources=TISCreateInputSourceList((__bridge CFDictionaryRef)filter, true);
        int result=-1; if(sources && CFArrayGetCount(sources)) result=layoutKey((TISInputSourceRef)CFArrayGetValueAtIndex(sources,0),character);
        if(sources) CFRelease(sources); return result;
    }
}
// Explicitly use the per-user file-based default Keychain for unsigned development.
// Never silently substitute files or the System keychain; no implicit unlock dialogs.
static OSStatus keychain(const char *path, SecKeychainRef *keychain) {
    // Legacy file-based Keychain ACL reads can ignore kSecUseAuthenticationUIFail.
    // This process never prompts/unlocks implicitly; changed signing/ACL must fail closed.
    OSStatus interaction=SecKeychainSetUserInteractionAllowed(false); if(interaction) return interaction;
    OSStatus status=path ? SecKeychainOpen(path, keychain) : SecKeychainCopyDefault(keychain);
    if(status) return status;
    char keychainPath[PATH_MAX]={0}; UInt32 size=sizeof(keychainPath); struct stat metadata;
    status=SecKeychainGetPath(*keychain, &size, keychainPath);
    if(!status && (size>=sizeof(keychainPath) || lstat(keychainPath, &metadata)!=0 || metadata.st_uid!=geteuid())) status=errSecNoDefaultKeychain;
    SecKeychainStatus flags=0; if(!status) status=SecKeychainGetStatus(*keychain, &flags);
    if(!status && !(flags & kSecUnlockStateStatus)) status=errSecInteractionNotAllowed;
    if(status) { CFRelease(*keychain); *keychain=NULL; }
    return status;
}
// SecKeychainSetUserInteractionAllowed is process-wide. Normal reads share this lock and never
// wait behind an explicit authorization dialog: while one is pending they fail closed instead.
static pthread_rwlock_t keychainGate=PTHREAD_RWLOCK_INITIALIZER;
static int secret_locked(const char *path, const char *account, unsigned char *bytes, int create) {
    @autoreleasepool {
        SecKeychainRef kc=NULL; OSStatus status=keychain(path, &kc); if(status) return status;
        NSMutableDictionary *query=[@{(__bridge id)kSecClass:(__bridge id)kSecClassGenericPassword,
            (__bridge id)kSecAttrService:@"local.pc-remote.storage.v1",
            (__bridge id)kSecAttrAccount:[NSString stringWithUTF8String:account],
            (__bridge id)kSecMatchSearchList:@[(__bridge id)kc],
            (__bridge id)kSecUseAuthenticationUI:(__bridge id)kSecUseAuthenticationUIFail,
            (__bridge id)kSecReturnData:@YES} mutableCopy];
        CFTypeRef value=NULL; status=SecItemCopyMatching((__bridge CFDictionaryRef)query, &value);
        if(status==errSecItemNotFound && create) {
            [query removeObjectForKey:(__bridge id)kSecMatchSearchList]; [query removeObjectForKey:(__bridge id)kSecReturnData];
            query[(__bridge id)kSecUseKeychain]=(__bridge id)kc;
            query[(__bridge id)kSecAttrSynchronizable]=@NO;
            query[(__bridge id)kSecValueData]=[NSData dataWithBytes:bytes length:32];
            status=SecItemAdd((__bridge CFDictionaryRef)query, NULL);
        } else if(!status) {
            NSData *data=(__bridge NSData *)value;
            if(data.length!=32) status=errSecDecode; else memcpy(bytes, data.bytes, 32);
        }
        if(value) CFRelease(value); CFRelease(kc); return status;
    }
}
static int secret(const char *path, const char *account, unsigned char *bytes, int create) {
    if(pthread_rwlock_tryrdlock(&keychainGate)) return errSecInteractionNotAllowed;
    int status=secret_locked(path, account, bytes, create);
    pthread_rwlock_unlock(&keychainGate); return status;
}
int pc_delete_test_secret(const char *account) {
    @autoreleasepool {
        if(strncmp(account, "test-", 5)!=0) return errSecParam;
        if(pthread_rwlock_tryrdlock(&keychainGate)) return errSecInteractionNotAllowed;
        SecKeychainRef kc=NULL; OSStatus status=keychain(NULL, &kc);
        if(!status) {
            NSDictionary *query=@{(__bridge id)kSecClass:(__bridge id)kSecClassGenericPassword,
                (__bridge id)kSecAttrService:@"local.pc-remote.storage.v1", (__bridge id)kSecAttrAccount:[NSString stringWithUTF8String:account],
                (__bridge id)kSecMatchSearchList:@[(__bridge id)kc], (__bridge id)kSecUseAuthenticationUI:(__bridge id)kSecUseAuthenticationUIFail};
            status=SecItemDelete((__bridge CFDictionaryRef)query); CFRelease(kc);
        }
        pthread_rwlock_unlock(&keychainGate); return status;
    }
}

int pc_secret(const char *account, unsigned char *bytes, int create) { return secret(NULL, account, bytes, create); }
// Explicit local action only, for the EXISTING storage key after an update/rebuild changed the
// unsigned app's code identity. macOS shows its own Keychain dialog and requires the user's
// keychain password; Always Allow updates the item's access for this app. The bridge never edits
// ACLs, unlocks a locked Keychain, creates/replaces keys or returns key bytes from this call.
int pc_secret_authorize(const char *account) {
    @autoreleasepool {
        if(pthread_rwlock_wrlock(&keychainGate)) return errSecInternalComponent;
        SecKeychainRef kc=NULL; OSStatus status=keychain(NULL, &kc);
        if(!status) {
            status=SecKeychainSetUserInteractionAllowed(true);
            if(!status) {
                NSDictionary *query=@{(__bridge id)kSecClass:(__bridge id)kSecClassGenericPassword,
                    (__bridge id)kSecAttrService:@"local.pc-remote.storage.v1",
                    (__bridge id)kSecAttrAccount:[NSString stringWithUTF8String:account],
                    (__bridge id)kSecMatchSearchList:@[(__bridge id)kc],
                    (__bridge id)kSecUseAuthenticationUI:(__bridge id)kSecUseAuthenticationUIAllow,
                    (__bridge id)kSecReturnData:@YES};
                CFTypeRef value=NULL; status=SecItemCopyMatching((__bridge CFDictionaryRef)query, &value);
                if(!status && (!value || CFGetTypeID(value)!=CFDataGetTypeID() || CFDataGetLength((CFDataRef)value)!=32)) status=errSecDecode;
                if(value) CFRelease(value);
            }
            OSStatus restored=SecKeychainSetUserInteractionAllowed(false); if(!status) status=restored;
            CFRelease(kc);
        }
        pthread_rwlock_unlock(&keychainGate); return status;
    }
}
static int testPath(const char *path) { return path && strncmp(path, "/private/tmp/PcMacKeychain-", strlen("/private/tmp/PcMacKeychain-"))==0; }
int pc_test_keychain_create(const char *path, const unsigned char *password, int length) {
    if(!testPath(path)) return errSecParam;
    SecKeychainRef kc=NULL; OSStatus status=SecKeychainCreate(path, length, password, false, NULL, &kc);
    if(kc) CFRelease(kc); return status;
}
int pc_test_keychain_lock(const char *path) {
    if(!testPath(path)) return errSecParam;
    SecKeychainRef kc=NULL; OSStatus status=SecKeychainOpen(path, &kc);
    if(!status) { status=SecKeychainLock(kc); CFRelease(kc); } return status;
}
int pc_test_keychain_delete(const char *path) {
    if(!testPath(path)) return errSecParam;
    SecKeychainRef kc=NULL; OSStatus status=SecKeychainOpen(path, &kc);
    if(!status) { status=SecKeychainDelete(kc); CFRelease(kc); } return status;
}
int pc_test_secret(const char *path, const char *account, unsigned char *bytes, int create) {
    if(!testPath(path) || strncmp(account, "test-", 5)!=0) return errSecParam;
    return secret(path, account, bytes, create);
}


/* Phase 5: all producing actions recheck the native lifecycle generation. */
int pc_feature_ready(void) {
    return pc_permission() && pc_session() && (!atomic_load(&enforceGeneration) || atomic_load(&armedGeneration)==atomic_load(&generation));
}
static AudioDeviceID output_device(void) {
    AudioDeviceID device=0; UInt32 size=sizeof(device);
    AudioObjectPropertyAddress property={kAudioHardwarePropertyDefaultOutputDevice,kAudioObjectPropertyScopeGlobal,kAudioObjectPropertyElementMain};
    return AudioObjectGetPropertyData(kAudioObjectSystemObject,&property,0,NULL,&size,&device)==noErr ? device : 0;
}
static int audio_property(AudioDeviceID device, AudioObjectPropertySelector selector, UInt32 channel, void *value, UInt32 size, int write) {
    AudioObjectPropertyAddress p={selector,kAudioDevicePropertyScopeOutput,channel};
    Boolean settable=false;
    if(!AudioObjectHasProperty(device,&p) || AudioObjectIsPropertySettable(device,&p,&settable)!=noErr || !settable) return 0;
    return (write ? AudioObjectSetPropertyData(device,&p,0,NULL,size,value) : AudioObjectGetPropertyData(device,&p,0,NULL,&size,value))==noErr;
}
// mode -1 reads; 0 sets exact level/unmutes; 1 toggles mute. Read-only HDMI is unavailable.
int pc_volume(int mode, int requested, int *level, int *muted) {
    AudioDeviceID device=output_device(); if(!device || (mode>=0 && !pc_feature_ready())) return 0;
    UInt32 mute=0; if(!audio_property(device,kAudioDevicePropertyMute,0,&mute,sizeof(mute),0)) return 0;
    Float32 volume=0; UInt32 channels[32], count=0;
    if(audio_property(device,kAudioDevicePropertyVolumeScalar,0,&volume,sizeof(volume),0)) channels[count++]=0;
    else {
        AudioObjectPropertyAddress p={kAudioDevicePropertyStreamConfiguration,kAudioDevicePropertyScopeOutput,0}; UInt32 size=0;
        if(AudioObjectGetPropertyDataSize(device,&p,0,NULL,&size)!=noErr || size>65536) return 0;
        AudioBufferList *buffers=malloc(size); if(!buffers) return 0;
        if(AudioObjectGetPropertyData(device,&p,0,NULL,&size,buffers)!=noErr) { free(buffers); return 0; }
        UInt32 total=0; for(UInt32 i=0;i<buffers->mNumberBuffers;i++) total+=buffers->mBuffers[i].mNumberChannels;
        free(buffers); if(!total || total>32) return 0;
        for(UInt32 channel=1;channel<=total;channel++) { Float32 v=0; if(!audio_property(device,kAudioDevicePropertyVolumeScalar,channel,&v,sizeof(v),0)) return 0; channels[count++]=channel; volume+=v/total; }
    }
    if(mode==0) {
        if(requested<0 || requested>100) return 0;
        Float32 v=requested/100.0f; mute=0;
        for(UInt32 i=0;i<count;i++) if(!pc_feature_ready() || output_device()!=device || !audio_property(device,kAudioDevicePropertyVolumeScalar,channels[i],&v,sizeof(v),1)) return 0;
        if(!pc_feature_ready() || output_device()!=device || !audio_property(device,kAudioDevicePropertyMute,0,&mute,sizeof(mute),1)) return 0;
    } else if(mode==1) { mute=!mute; if(!pc_feature_ready() || output_device()!=device || !audio_property(device,kAudioDevicePropertyMute,0,&mute,sizeof(mute),1)) return 0; }
    else if(mode!=-1) return 0;
    if(output_device()!=device) return 0;
    if(mode>=0) return pc_volume(-1,0,level,muted);
    *level=(int)lroundf(fminf(1,fmaxf(0,volume))*100); *muted=mute!=0; return 1;
}
int pc_system_key(int code, int down, int guard) { @autoreleasepool {
    if(code!=NX_KEYTYPE_PLAY && code!=NX_KEYTYPE_NEXT && code!=NX_KEYTYPE_PREVIOUS) return 0;
    if(!pc_permission() || (down && (!pc_feature_ready() || (guard && pc_frontmost()!=guard)))) return 0;
    NSEvent *e=[NSEvent otherEventWithType:NSEventTypeSystemDefined location:NSZeroPoint modifierFlags:down?0xa00:0xb00
        timestamp:0 windowNumber:0 context:nil subtype:8 data1:(code<<16)|((down?0xa:0xb)<<8) data2:-1];
    CGEventRef cg=e.CGEvent; if(!cg) return 0; CGEventPost(kCGHIDEventTap,cg); return pc_permission();
} }
int pc_player_focus(const char *profile) { @autoreleasepool {
    NSString *bundle=[NSRunningApplication runningApplicationWithProcessIdentifier:pc_frontmost()].bundleIdentifier;
    if(!strcmp(profile,"VLC")) return [bundle isEqualToString:@"org.videolan.vlc"];
    if(!strcmp(profile,"Stremio")) return [bundle isEqualToString:@"com.smartcodeltd.stremio"] || [bundle isEqualToString:@"com.stremio.stremio"];
    if(!strcmp(profile,"YouTube")) return [@[@"com.apple.Safari",@"com.google.Chrome",@"org.mozilla.firefox",@"com.microsoft.edgemac"] containsObject:bundle ?: @""];
    return 0;
} }
int pc_app_valid(const char *path) { @autoreleasepool {
    NSString *p=[NSString stringWithUTF8String:path]; NSBundle *bundle=[NSBundle bundleWithPath:p];
    return [p.pathExtension isEqualToString:@"app"] && bundle.bundleIdentifier && bundle.executableURL && [[NSFileManager defaultManager] isExecutableFileAtPath:bundle.executablePath];
} }
int pc_launch(const char *path) { @autoreleasepool {
    if(!pc_feature_ready() || !pc_app_valid(path)) return 0;
    NSWorkspaceOpenConfiguration *config=[NSWorkspaceOpenConfiguration configuration]; config.activates=YES; config.createsNewApplicationInstance=NO;
    // LaunchServices opens exactly the selected bundle: no arguments, URL or shell.
    if([NSThread isMainThread]) return 0; // Remote requests run on Kestrel workers; never block the UI queue.
    dispatch_semaphore_t completed=dispatch_semaphore_create(0); __block BOOL opened=NO;
    [[NSWorkspace sharedWorkspace] openApplicationAtURL:[NSURL fileURLWithPath:[NSString stringWithUTF8String:path]] configuration:config
        completionHandler:^(NSRunningApplication *app,NSError *error) { opened=app!=nil && error==nil; dispatch_semaphore_signal(completed); }];
    return dispatch_semaphore_wait(completed,dispatch_time(DISPATCH_TIME_NOW,5*NSEC_PER_SEC))==0 && opened; // Never claim foreground receipt.
} }
int pc_can_sleep(void) { return IOPMSleepEnabled() && pc_session(); }
int pc_sleep(void) {
    if(!pc_feature_ready() || !pc_can_sleep()) return 0;
    io_connect_t connection=IOPMFindPowerManagement(MACH_PORT_NULL); if(!connection) return 0;
    IOReturn result=IOPMSleepSystem(connection); IOServiceClose(connection); return result==kIOReturnSuccess;
}
int pc_login_status(void) { @autoreleasepool {
    if(![[NSBundle mainBundle].bundlePath.pathExtension isEqualToString:@"app"]) return -1;
    return (int)SMAppService.mainAppService.status;
} }
int pc_login_set(int enable) { @autoreleasepool {
    if(pc_login_status()<0) return 0;
    NSError *error=nil; BOOL ok=enable ? [SMAppService.mainAppService registerAndReturnError:&error] : [SMAppService.mainAppService unregisterAndReturnError:&error];
    return ok && !error;
} }
