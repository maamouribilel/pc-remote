using System.Diagnostics;
using System.Text.Json;
namespace PcController.MacOS;
internal static class SwitchApp
{
    internal static async Task Run(MacInput input, int firstPid, string firstDirectory, Action<bool,string> check)
    {
        var directory = "/private/tmp/PcMacSmoke-" + Guid.NewGuid().ToString("N"); Directory.CreateDirectory(directory);
        var app = Path.Combine(AppContext.BaseDirectory, "Second Native Target.app");
        using var second = Process.Start(new ProcessStartInfo("/usr/bin/open") { ArgumentList = { "-n", "-W", app, "--args", directory } })!;
        try
        {
            int pid=0;
            for(var i=0;i<100;i++)
            {
                if(File.Exists(Path.Combine(directory,"state.json")))
                {
                    using var state=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory,"state.json")));
                    pid=state.RootElement.GetProperty("pid").GetInt32(); if(MacNative.pc_frontmost()==pid) break;
                }
                await Task.Delay(50);
            }
            if(pid==0 || MacNative.pc_frontmost()!=pid) throw new PcController.Core.InputFailureException("Second disposable target failed to obtain focus; switch test aborted.");
            if(MacNative.pc_test_activate(firstPid)==0) throw new PcController.Core.InputFailureException("First disposable target could not activate; switch test aborted.");
            for(var i=0;i<100 && MacNative.pc_frontmost()!=firstPid;i++) await Task.Delay(50);
            if(MacNative.pc_frontmost()!=firstPid) throw new PcController.Core.InputFailureException("First disposable target lost focus; switch test aborted.");
            input.Shortcut("switchApp");
            for(var i=0;i<30 && MacNative.pc_frontmost()!=pid;i++) await Task.Delay(50);
            check(MacNative.pc_frontmost()==pid, "ACTUAL Command+Tab switches between two explicitly created disposable apps");
            // No new input is permitted after the expected focus switch; only tracked releases.
            input.ReleaseAll();
        }
        finally
        {
            input.ReleaseAll(); File.WriteAllText(Path.Combine(directory,"stop"),"stop");
            using var timeout=new CancellationTokenSource(3000);
            try { await second.WaitForExitAsync(timeout.Token); } catch(OperationCanceledException) { second.Kill(); await second.WaitForExitAsync(); }
            Directory.Delete(directory,true);
        }
    }
}
