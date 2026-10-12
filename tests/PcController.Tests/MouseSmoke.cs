using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using PcController.Core;
using PcController.Windows;

internal static class MouseSmoke
{
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    internal static Task Run(Action<bool, string> check)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            var target = new Border { Background = Brushes.DarkSlateGray, Child = new TextBlock { Text = "Disposable mouse input test\nNo documents or text input", FontSize = 24, Foreground = Brushes.White, Margin = new Thickness(24) } };
            var window = new Window { Title = "PC Remote — disposable mouse test", Width = 640, Height = 420, WindowStartupLocation = WindowStartupLocation.CenterScreen, Topmost = true, Content = target };
            var input = new WindowsInput(); var left = 0; var right = 0; var doubleClicks = 0; var wheels = 0;
            target.MouseDown += (_, e) => { if (e.ChangedButton == MouseButton.Left) left++; if (e.ChangedButton == MouseButton.Right) right++; if (e.ClickCount == 2) doubleClicks++; };
            target.MouseWheel += (_, _) => wheels++;
            GetCursorPos(out var original);
            window.Show(); window.Activate();
            dispatcher.BeginInvoke(async () =>
            {
                try
                {
                    await Task.Delay(200);
                    var handle = new WindowInteropHelper(window).Handle;
                    check(GetForegroundWindow() == handle, "disposable test window owns foreground before injection");
                    check((GetAsyncKeyState(1) & 0x8000) == 0 && (GetAsyncKeyState(2) & 0x8000) == 0, "physical mouse buttons are released before smoke test");
                    var center = target.PointToScreen(new System.Windows.Point(target.ActualWidth / 2, target.ActualHeight / 2));
                    if (!SetCursorPos((int)center.X, (int)center.Y)) throw new Exception("Could not position cursor in disposable target.");
                    await Task.Delay(100); GetCursorPos(out var before);
                    void Guard()
                    {
                        GetCursorPos(out var position);
                        var local = target.PointFromScreen(new System.Windows.Point(position.X, position.Y));
                        if (GetForegroundWindow() != handle || local.X < 0 || local.Y < 0 || local.X >= target.ActualWidth || local.Y >= target.ActualHeight) throw new Exception("Disposable target lost focus or cursor; stop injection.");
                    }
                    Guard(); input.Move(12, 8); await Task.Delay(100); GetCursorPos(out var after);
                    check(after.X > before.X && after.Y > before.Y, "real SendInput moves Windows cursor relatively");
                    Guard(); input.Click("left", false); await Task.Delay(100);
                    check(left >= 1, "real left click received by disposable window");
                    Guard(); input.Click("right", false); await Task.Delay(100);
                    check(right == 1, "real right click received by disposable window");
                    await Task.Delay(600); Guard(); input.Click("left", true); await Task.Delay(100);
                    check(doubleClicks >= 1, "real double click received by disposable window");
                    Guard(); input.Scroll(0, 120); await Task.Delay(100);
                    check(wheels >= 1, "real wheel scroll received by disposable window");
                    using var controller = new InputController(input, new());
                    Guard(); controller.Execute(new(1, "pointer.button", Button: "left", Action: "down")); await Task.Delay(100);
                    check((GetAsyncKeyState(1) & 0x8000) != 0, "real drag holds Windows left button");
                    Guard(); controller.Execute(new(2, "pointer.move", 10, 0)); controller.ReleaseAll(); await Task.Delay(100);
                    check((GetAsyncKeyState(1) & 0x8000) == 0, "real cleanup releases Windows drag button");
                    Guard(); controller.Execute(new(3, "pointer.button", Button: "left", Action: "down")); await Task.Delay(1500);
                    check((GetAsyncKeyState(1) & 0x8000) == 0, "actual timer watchdog releases real Windows button");
                    done.TrySetResult();
                }
                catch (Exception e) { done.TrySetException(e); }
                finally
                {
                    try { input.ReleaseAll(); } finally { window.Close(); SetCursorPos(original.X, original.Y); dispatcher.BeginInvokeShutdown(DispatcherPriority.Background); }
                }
            });
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        return done.Task;
    }
}
