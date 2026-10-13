namespace PcController.Windows;
public static class WindowsShortcutMappings
{
    public static readonly IReadOnlyDictionary<string, string[]> Shortcuts = new Dictionary<string, string[]>
    { ["copy"] = ["Control", "C"], ["paste"] = ["Control", "V"], ["selectAll"] = ["Control", "A"], ["undo"] = ["Control", "Z"], ["switchApp"] = ["Alt", "Tab"] };
}
