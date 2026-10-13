#import <AppKit/AppKit.h>
#import <ApplicationServices/ApplicationServices.h>
#include <unistd.h>
#include <sys/stat.h>
static NSString *directory;
static NSMutableDictionary *counts;
static BOOL leftHeld, shiftHeld;
@interface ScratchView : NSTextView
@property(strong) NSString *scratchClipboard;
@end
@implementation ScratchView
- (BOOL)validateUserInterfaceItem:(id<NSValidatedUserInterfaceItem>)item {
    if(item.action==@selector(paste:)) return self.scratchClipboard!=nil;
    if(item.action==@selector(copy:)) return self.selectedRange.length>0;
    if(item.action==@selector(selectAll:)) return YES;
    if(item.action==@selector(undo:)) return self.undoManager.canUndo;
    return [super validateUserInterfaceItem:item];
}
- (void)copy:(id)sender { (void)sender; counts[@"copy"]=@([counts[@"copy"] intValue]+1); self.scratchClipboard=[self.string substringWithRange:self.selectedRange]; }
- (void)paste:(id)sender { (void)sender; counts[@"paste"]=@([counts[@"paste"] intValue]+1); [self insertText:self.scratchClipboard ?: @"" replacementRange:self.selectedRange]; }
- (void)undo:(id)sender { (void)sender; counts[@"undo"]=@([counts[@"undo"] intValue]+1); [self.undoManager undo]; }
- (void)mouseDown:(NSEvent *)event { (void)event; [self.window makeFirstResponder:self]; }
- (void)mouseDragged:(NSEvent *)event { (void)event; counts[@"drag"]=@([counts[@"drag"] intValue]+1); }
- (void)scrollWheel:(NSEvent *)event { counts[@"scroll"]=@([counts[@"scroll"] intValue]+1); [super scrollWheel:event]; }
- (void)rightMouseDown:(NSEvent *)event { (void)event; } // No context menu or other app.
@end
@interface Target : NSObject<NSApplicationDelegate>
@property(strong) NSWindow *window;
@property(strong) ScratchView *text;
@end
@implementation Target
- (void)applicationDidFinishLaunching:(NSNotification *)notification {
    (void)notification;
    NSMenu *menu=[NSMenu new]; NSMenuItem *edit=[NSMenuItem new]; NSMenu *editMenu=[[NSMenu alloc] initWithTitle:@"Edit"];
    for(NSArray *spec in @[@[@"Copy",@"copy:",@"c"],@[@"Paste",@"paste:",@"v"],@[@"Select All",@"selectAll:",@"a"],@[@"Undo",@"undo:",@"z"]])
        [editMenu addItem:[[NSMenuItem alloc] initWithTitle:spec[0] action:NSSelectorFromString(spec[1]) keyEquivalent:spec[2]]];
    edit.submenu=editMenu; [menu addItem:edit]; NSApp.mainMenu=menu;
    self.window=[[NSWindow alloc] initWithContentRect:NSMakeRect(200,200,640,420) styleMask:NSWindowStyleMaskTitled|NSWindowStyleMaskClosable backing:NSBackingStoreBuffered defer:NO];
    self.window.title=@"PC Remote disposable native input target";
    self.text=[[ScratchView alloc] initWithFrame:NSMakeRect(0,0,640,420)]; self.text.font=[NSFont systemFontOfSize:18]; self.text.allowsUndo=YES; self.text.richText=NO;
    self.text.automaticQuoteSubstitutionEnabled=NO; self.text.automaticDashSubstitutionEnabled=NO;
    self.text.automaticTextReplacementEnabled=NO; self.text.automaticSpellingCorrectionEnabled=NO;
    self.text.automaticTextCompletionEnabled=NO; self.text.continuousSpellCheckingEnabled=NO;
    self.text.grammarCheckingEnabled=NO; self.text.smartInsertDeleteEnabled=NO;
    self.window.contentView=self.text; [self.window makeKeyAndOrderFront:nil]; [self.window makeFirstResponder:self.text]; self.window.acceptsMouseMovedEvents=YES;
    [NSApp activateIgnoringOtherApps:YES];
    [NSEvent addLocalMonitorForEventsMatchingMask:NSEventMaskAny handler:^NSEvent *(NSEvent *e) {
        NSString *key=[NSString stringWithFormat:@"%lu", (unsigned long)e.type]; counts[key]=@([counts[key] intValue]+1);
        if(e.type==NSEventTypeLeftMouseDown) leftHeld=YES;
        if(e.type==NSEventTypeLeftMouseUp) leftHeld=NO;
        if(e.type==NSEventTypeFlagsChanged && e.keyCode==56) shiftHeld=(e.modifierFlags & NSEventModifierFlagShift)!=0; if(e.type==NSEventTypeLeftMouseDown) counts[@"maxClick"]=@(MAX([counts[@"maxClick"] intValue], (int)e.clickCount)); return e;
    }];
    [NSTimer scheduledTimerWithTimeInterval:0.05 repeats:YES block:^(NSTimer *timer) {
        if([[NSFileManager defaultManager] fileExistsAtPath:[directory stringByAppendingPathComponent:@"stop"]]) { [timer invalidate]; [NSApp terminate:nil]; return; }
        NSRect frame=self.window.frame; CGFloat screenHeight=NSScreen.screens[0].frame.size.height;
        NSDictionary *state=@{@"pid":@(getpid()), @"leftHeld":@(leftHeld), @"shiftHeld":@(shiftHeld), @"text":self.text.string, @"clipboard":self.text.scratchClipboard ?: @"", @"selectionLength":@(self.text.selectedRange.length), @"counts":counts, @"frontmost":@([NSWorkspace sharedWorkspace].frontmostApplication.processIdentifier == getpid()),
            @"x":@(NSMidX(frame)), @"y":@(screenHeight-NSMidY(frame)), @"flags":@([NSEvent modifierFlags])};
        NSData *bytes=[NSJSONSerialization dataWithJSONObject:state options:0 error:nil];
        [bytes writeToFile:[directory stringByAppendingPathComponent:@"state.json"] atomically:YES];
    }];
}
@end
int main(int argc, char **argv) {
    if(argc!=2 || strncmp(argv[1], "/private/tmp/PcMacSmoke-", strlen("/private/tmp/PcMacSmoke-"))!=0) return 2;
    @autoreleasepool {
        umask(077); directory=[NSString stringWithUTF8String:argv[1]]; counts=[NSMutableDictionary dictionary];
        NSApplication *app=[NSApplication sharedApplication]; app.activationPolicy=NSApplicationActivationPolicyRegular;
        Target *target=[Target new]; app.delegate=target; [app run];
    }
    return 0;
}
