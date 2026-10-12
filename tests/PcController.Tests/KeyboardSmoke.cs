using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using PcController.Core;
using PcController.Windows;
internal static class KeyboardSmoke
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern nint GetKeyboardLayout(uint thread);
    internal static Task Run(Action<bool, string> check)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            var field = new TextBox { AcceptsReturn = true, AcceptsTab = true, FontSize = 24, Margin = new Thickness(24) };
            var window = new Window { Title = "PC Remote — disposable keyboard test", Width = 640, Height = 420, WindowStartupLocation = WindowStartupLocation.CenterScreen, Topmost = true, Content = field };
            var input = new WindowsInput(); window.Show(); window.Activate(); field.Focus();
            dispatcher.BeginInvoke(async () =>
            {
                try
                {
                    await Task.Delay(200); var handle = new WindowInteropHelper(window).Handle;
                    void Guard() { if (GetForegroundWindow() != handle || !field.IsKeyboardFocused) throw new Exception("Disposable keyboard target lost focus; stop injection."); }
                    Guard(); check(new[] { 0x10, 0x11, 0x12, 0x5B }.All(k => (GetAsyncKeyState(k) & 0x8000) == 0), "physical modifiers released before keyboard smoke");
                    Console.WriteLine($"Disposable target layout ID: {GetKeyboardLayout(0).ToInt64() & 0xFFFF:X4}");
                    const string sample = "Bonjour, été café — 😀";
                    input.Text(sample); await Task.Delay(100);
                    check(field.Text == sample, "actual Unicode accents and surrogate pair inserted once");
                    Guard(); input.Shortcut("selectAll"); await Task.Delay(100);
                    check(field.SelectionLength == sample.Length, "actual Ctrl+A selects disposable field text");
                    Guard(); input.Press("ArrowRight"); await Task.Delay(100);
                    check(field.SelectionLength == 0 && field.CaretIndex == sample.Length, "actual navigation arrow moves caret");
                    Guard(); input.Press("Home"); await Task.Delay(100);
                    check(field.CaretIndex == 0, "actual Home navigation targets line start");
                    Guard(); input.Key("Shift", true); input.Press("End"); await Task.Delay(100);
                    check(field.SelectionLength == sample.Length && (GetAsyncKeyState(0x10) & 0x8000) != 0, "actual Shift+End holds modifier and selects text");
                    input.ReleaseAll(); await Task.Delay(100);
                    check((GetAsyncKeyState(0x10) & 0x8000) == 0, "actual cleanup releases Shift");
                    Guard(); input.Press("Backspace"); await Task.Delay(100); check(field.Text.Length == 0, "actual Backspace deletes disposable selection");
                    Guard(); input.Text(sample + "\nNext"); await Task.Delay(100);
                    check(field.Text.Replace("\r\n", "\n") == sample + "\nNext", "actual multiline text separates Enter from Unicode units");
                    Guard(); input.Chord(MediaProfiles.Mappings["YouTube"]["playPause"]); await Task.Delay(100);
                    check(field.Text.EndsWith("Nextk"), "actual YouTube profile chord emits physical K");
                    Guard(); input.Chord(MediaProfiles.Mappings["VLC"]["seekBack"]); await Task.Delay(100);
                    check(field.SelectionLength == 1 && (GetAsyncKeyState(0x10) & 0x8000) == 0, "actual VLC seek chord emits Shift+Left and releases modifier");
                    Guard(); input.Chord(MediaProfiles.Mappings["Stremio"]["next"]); await Task.Delay(100);
                    check(field.Text.EndsWith("NextN") && (GetAsyncKeyState(0x10) & 0x8000) == 0, "actual Stremio next episode chord emits Shift+N and releases modifier");
                    Guard(); input.Chord(MediaProfiles.Mappings["Stremio"]["seekBack"]); await Task.Delay(100);
                    check(field.SelectionLength == 0 && field.CaretIndex == field.Text.Length - 1, "actual Stremio seek chord emits Left");
                    using var controller = new InputController(input, new());
                    Guard(); controller.Execute(new(1, "key.down", Key: "Control")); await Task.Delay(100);
                    check((GetAsyncKeyState(0x11) & 0x8000) != 0, "actual remote Control hold reaches Windows");
                    await Task.Delay(1500);
                    check((GetAsyncKeyState(0x11) & 0x8000) == 0, "actual key watchdog releases Control");
                    done.TrySetResult();
                }
                catch (Exception e) { done.TrySetException(e); }
                finally { try { input.ReleaseAll(); } finally { window.Close(); dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); } }
            });
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); return done.Task;
    }
}
