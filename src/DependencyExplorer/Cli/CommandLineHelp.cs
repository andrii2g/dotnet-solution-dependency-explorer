namespace DependencyExplorer.Cli;

internal static class CommandLineHelp
{
    public static void WriteGeneralHelp(TextWriter writer)
    {
        writer.WriteLine("DependencyExplorer");
        writer.WriteLine("Usage:");
        writer.WriteLine("  DependencyExplorer analyze --solution <path-to-sln-or-slnx> [options]");
        writer.WriteLine();
        writer.WriteLine("Run 'DependencyExplorer analyze --help' for analyze command details.");
    }

    public static void WriteAnalyzeHelp(TextWriter writer)
    {
        writer.WriteLine("DependencyExplorer analyze");
        writer.WriteLine("Usage:");
        writer.WriteLine("  DependencyExplorer analyze --solution <path-to-sln-or-slnx> [options]");
        writer.WriteLine();
        writer.WriteLine("Implemented in Phase 1:");
        writer.WriteLine("  --solution <path>         Required path to a .sln or .slnx file.");
        writer.WriteLine("  --output <directory>      Output directory. Defaults to ./dependency-explorer-output.");
        writer.WriteLine("  --level <value>           project | namespace | class | all. Defaults to all.");
        writer.WriteLine("  --graph-format <value>    mermaid | none. Defaults to mermaid.");
        writer.WriteLine("  --verbose                 Enable verbose console output.");
        writer.WriteLine();
        writer.WriteLine("Reserved for later phases:");
        writer.WriteLine("  --project");
        writer.WriteLine("  --directory");
        writer.WriteLine("  --include-external");
        writer.WriteLine("  --exclude-tests");
        writer.WriteLine("  --exclude-generated");
        writer.WriteLine("  --project-filter");
        writer.WriteLine("  --namespace-filter");
        writer.WriteLine("  --max-class-graph-nodes");
        writer.WriteLine("  --focus-project");
        writer.WriteLine("  --focus-namespace");
        writer.WriteLine("  --focus-class");
        writer.WriteLine("  --detect-cycles");
        writer.WriteLine("  --detect-hubs");
        writer.WriteLine("  --collapse-packages");
        writer.WriteLine("  --skip-classification");
        writer.WriteLine("  --skip-di-graph");
    }
}
