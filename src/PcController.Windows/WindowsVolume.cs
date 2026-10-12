using System.Runtime.InteropServices;
using PcController.Core;

namespace PcController.Windows;

// Public Core Audio interfaces. Open the current default output for each operation,
// so reconnecting headphones or changing Windows' output never leaves a stale endpoint.
public static class WindowsVolume
{
    public static VolumeState Read() => WithEndpoint(endpoint =>
    {
        Marshal.ThrowExceptionForHR(endpoint.GetMasterVolumeLevelScalar(out var level));
        Marshal.ThrowExceptionForHR(endpoint.GetMute(out var muted));
        return new(true, Math.Clamp((int)Math.Round(level * 100), 0, 100), muted);
    });
    public static VolumeState Set(int level)
    {
        if (level is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(level));
        return WithEndpoint(endpoint =>
        {
            var context = Guid.Empty;
            Marshal.ThrowExceptionForHR(endpoint.SetMasterVolumeLevelScalar(level / 100f, ref context));
            Marshal.ThrowExceptionForHR(endpoint.SetMute(false, ref context));
            Marshal.ThrowExceptionForHR(endpoint.GetMasterVolumeLevelScalar(out var actual));
            Marshal.ThrowExceptionForHR(endpoint.GetMute(out var muted));
            return new(true, Math.Clamp((int)Math.Round(actual * 100), 0, 100), muted);
        });
    }
    private static VolumeState WithEndpoint(Func<IAudioEndpointVolume, VolumeState> action)
    {
        object? enumeratorObject = null, deviceObject = null, endpointObject = null;
        try
        {
            enumeratorObject = Activator.CreateInstance(Type.GetTypeFromCLSID(new("BCDE0395-E52F-467C-8E3D-C4579291692E"), true)!);
            var enumerator = (IMMDeviceEnumerator)enumeratorObject!;
            Marshal.ThrowExceptionForHR(enumerator.GetDefaultAudioEndpoint(0, 1, out var device)); deviceObject = device;
            var iid = typeof(IAudioEndpointVolume).GUID;
            Marshal.ThrowExceptionForHR(device.Activate(ref iid, 23, IntPtr.Zero, out endpointObject));
            return action((IAudioEndpointVolume)endpointObject);
        }
        catch (COMException) { return new(false); }
        finally
        {
            if (endpointObject != null) Marshal.ReleaseComObject(endpointObject);
            if (deviceObject != null) Marshal.ReleaseComObject(deviceObject);
            if (enumeratorObject != null) Marshal.ReleaseComObject(enumeratorObject);
        }
    }
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int flow, uint state, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int flow, int role, out IMMDevice device);
    }
    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint context, IntPtr parameters, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
    }
    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int GetChannelCount(out uint count);
        [PreserveSig] int SetMasterVolumeLevel(float level, ref Guid context);
        [PreserveSig] int SetMasterVolumeLevelScalar(float level, ref Guid context);
        [PreserveSig] int GetMasterVolumeLevel(out float level);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig] int SetChannelVolumeLevel(uint channel, float level, ref Guid context);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, ref Guid context);
        [PreserveSig] int GetChannelVolumeLevel(uint channel, out float level);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool muted, ref Guid context);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);
    }
}
