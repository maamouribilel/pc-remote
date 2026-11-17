using System.Runtime.CompilerServices;

// Update checks accept an in-memory feed and test keys only through internal constructors.
[assembly: InternalsVisibleTo("PcController.PortableTests")]
[assembly: InternalsVisibleTo("PcController.Tests")]
[assembly: InternalsVisibleTo("PcController.DesktopTests")]
