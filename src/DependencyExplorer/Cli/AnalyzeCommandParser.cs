namespace DependencyExplorer.Cli;

internal static class AnalyzeCommandParser
{
    public static AnalyzeParseResult Parse(IReadOnlyList<string> args)
    {
        var errors = new List<string>();
        string? solutionPath = null;
        string outputDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "dependency-explorer-output"));
        var level = AnalysisLevel.All;
        var graphFormat = GraphFormat.Mermaid;
        var verbose = false;

        for (var index = 0; index < args.Count; index++)
        {
            var arg = args[index];

            switch (arg)
            {
                case "--help":
                case "-h":
                    return AnalyzeParseResult.HelpRequested();

                case "--solution":
                    solutionPath = ReadRequiredValue(args, ref index, "--solution", errors);
                    break;

                case "--output":
                    var outputValue = ReadRequiredValue(args, ref index, "--output", errors);
                    if (!string.IsNullOrWhiteSpace(outputValue))
                    {
                        outputDirectory = Path.GetFullPath(outputValue);
                    }
                    break;

                case "--level":
                    var levelValue = ReadRequiredValue(args, ref index, "--level", errors);
                    if (!TryParseLevel(levelValue, out level))
                    {
                        errors.Add($"Unsupported --level value '{levelValue}'. Expected one of: project, namespace, class, all.");
                    }
                    break;

                case "--graph-format":
                    var graphFormatValue = ReadRequiredValue(args, ref index, "--graph-format", errors);
                    if (!TryParseGraphFormat(graphFormatValue, out graphFormat))
                    {
                        errors.Add($"Unsupported --graph-format value '{graphFormatValue}'. Expected one of: mermaid, none.");
                    }
                    break;

                case "--verbose":
                    verbose = true;
                    break;

                case "--project":
                case "--directory":
                case "--include-external":
                case "--exclude-tests":
                case "--exclude-generated":
                case "--project-filter":
                case "--namespace-filter":
                case "--max-class-graph-nodes":
                case "--focus-project":
                case "--focus-namespace":
                case "--focus-class":
                case "--detect-cycles":
                case "--detect-hubs":
                case "--collapse-packages":
                case "--skip-classification":
                case "--skip-di-graph":
                    errors.Add($"Option '{arg}' is reserved for later phases and is not implemented in Phase 1.");
                    if (OptionRequiresValue(arg))
                    {
                        _ = ReadRequiredValue(args, ref index, arg, errors);
                    }

                    break;

                default:
                    errors.Add($"Unknown option '{arg}'.");
                    break;
            }
        }

        if (errors.Count > 0)
        {
            return AnalyzeParseResult.Failure(errors);
        }

        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            errors.Add("Missing required option --solution <path-to-sln-or-slnx>.");
            return AnalyzeParseResult.Failure(errors);
        }

        return AnalyzeParseResult.Success(
            new AnalyzeCommandOptions(
                Path.GetFullPath(solutionPath),
                outputDirectory,
                level,
                graphFormat,
                verbose));
    }

    private static string? ReadRequiredValue(
        IReadOnlyList<string> args,
        ref int index,
        string optionName,
        ICollection<string> errors)
    {
        if (index + 1 >= args.Count)
        {
            errors.Add($"Option '{optionName}' requires a value.");
            return null;
        }

        index++;
        return args[index];
    }

    private static bool TryParseLevel(string? value, out AnalysisLevel level)
    {
        return Enum.TryParse(value, ignoreCase: true, out level);
    }

    private static bool TryParseGraphFormat(string? value, out GraphFormat graphFormat)
    {
        return Enum.TryParse(value, ignoreCase: true, out graphFormat);
    }

    private static bool OptionRequiresValue(string optionName)
    {
        return optionName is
            "--project" or
            "--directory" or
            "--project-filter" or
            "--namespace-filter" or
            "--max-class-graph-nodes" or
            "--focus-project" or
            "--focus-namespace" or
            "--focus-class";
    }
}

internal sealed class AnalyzeParseResult
{
    private AnalyzeParseResult(bool isSuccess, bool helpRequested, AnalyzeCommandOptions? options, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        HelpRequested = helpRequested;
        Options = options;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool HelpRequested { get; }

    public AnalyzeCommandOptions? Options { get; }

    public IReadOnlyList<string> Errors { get; }

    public static AnalyzeParseResult Success(AnalyzeCommandOptions options)
    {
        return new AnalyzeParseResult(true, false, options, Array.Empty<string>());
    }

    public static AnalyzeParseResult Failure(IReadOnlyList<string> errors)
    {
        return new AnalyzeParseResult(false, false, null, errors);
    }

    public static AnalyzeParseResult HelpRequested()
    {
        return new AnalyzeParseResult(false, true, null, Array.Empty<string>());
    }
}
