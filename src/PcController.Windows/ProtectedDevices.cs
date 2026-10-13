namespace PcController.Windows;
public sealed class ProtectedDevices(string directory)
    : Host.ProtectedDevices(directory, new WindowsProtectedStorage());
