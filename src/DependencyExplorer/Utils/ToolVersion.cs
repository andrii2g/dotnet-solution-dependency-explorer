namespace DependencyExplorer.Utils;

internal static class ToolVersion
{
    public static string Value =>
        typeof(ToolVersion).Assembly.GetName().Version?.ToString() ?? "0.0.0";
}
