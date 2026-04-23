using System.CommandLine;
using A2G.DependencyExplorer.Utils;

namespace A2G.DependencyExplorer.Cli;

internal static class CommandLineApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        var solutionOption = new Option<string>("--solution")
        {
            Description = "Required path to a .sln or .slnx file.",
            Required = true,
        };
        solutionOption.Validators.Add(result =>
        {
            var value = result.GetValueOrDefault<string>();
            if (string.IsNullOrWhiteSpace(value))
            {
                result.AddError("Option '--solution' requires a value.");
            }
        });

        var outputOption = new Option<string>("--output")
        {
            Description = "Output directory. Defaults to ./dependency-explorer-output.",
            DefaultValueFactory = _ => Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "dependency-explorer-output")),
        };

        var levelOption = new Option<AnalysisLevel>("--level")
        {
            Description = "project | namespace | class | all. Defaults to all.",
            DefaultValueFactory = _ => AnalysisLevel.All,
        };

        var graphFormatOption = new Option<GraphFormat>("--graph-format")
        {
            Description = "mermaid | none. Defaults to mermaid.",
            DefaultValueFactory = _ => GraphFormat.Mermaid,
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Description = "Enable verbose console output.",
        };
        var skipClassificationOption = new Option<bool>("--skip-classification")
        {
            Description = "Skip heuristic classification and emit only non-classification findings.",
        };
        var skipDiGraphOption = new Option<bool>("--skip-di-graph")
        {
            Description = "Skip constructor dependency extraction.",
        };
        var focusProjectOption = new Option<string?>("--focus-project")
        {
            Description = "Narrow focused outputs to one project name while keeping full-scope analysis.",
        };
        var focusNamespaceOption = new Option<string?>("--focus-namespace")
        {
            Description = "Narrow focused outputs to one namespace or namespace prefix.",
        };
        var focusClassOption = new Option<string?>("--focus-class")
        {
            Description = "Narrow focused outputs to one full type name.",
        };

        var analyzeCommand = new Command("analyze", "Analyze a .sln or .slnx and emit dependency artifacts.");
        analyzeCommand.Options.Add(solutionOption);
        analyzeCommand.Options.Add(outputOption);
        analyzeCommand.Options.Add(levelOption);
        analyzeCommand.Options.Add(graphFormatOption);
        analyzeCommand.Options.Add(verboseOption);
        analyzeCommand.Options.Add(skipClassificationOption);
        analyzeCommand.Options.Add(skipDiGraphOption);
        analyzeCommand.Options.Add(focusProjectOption);
        analyzeCommand.Options.Add(focusNamespaceOption);
        analyzeCommand.Options.Add(focusClassOption);

        AddUnsupportedOption(analyzeCommand, new Option<string>("--project"), expectsValue: true);
        AddUnsupportedOption(analyzeCommand, new Option<string>("--directory"), expectsValue: true);
        AddUnsupportedOption(analyzeCommand, new Option<bool>("--include-external"));
        AddUnsupportedOption(analyzeCommand, new Option<bool>("--exclude-tests"));
        AddUnsupportedOption(analyzeCommand, new Option<bool>("--exclude-generated"));
        AddUnsupportedOption(analyzeCommand, new Option<string>("--project-filter"), expectsValue: true);
        AddUnsupportedOption(analyzeCommand, new Option<string>("--namespace-filter"), expectsValue: true);
        AddUnsupportedOption(analyzeCommand, new Option<int>("--max-class-graph-nodes"), expectsValue: true);
        AddUnsupportedOption(analyzeCommand, new Option<bool>("--detect-cycles"));
        AddUnsupportedOption(analyzeCommand, new Option<bool>("--detect-hubs"));
        AddUnsupportedOption(analyzeCommand, new Option<bool>("--collapse-packages"));

        analyzeCommand.SetAction(async parseResult =>
        {
            var options = new AnalyzeCommandOptions(
                Path.GetFullPath(parseResult.GetValue(solutionOption)!),
                Path.GetFullPath(parseResult.GetValue(outputOption)!),
                parseResult.GetValue(levelOption),
                parseResult.GetValue(graphFormatOption),
                parseResult.GetValue(verboseOption),
                parseResult.GetValue(skipClassificationOption),
                parseResult.GetValue(skipDiGraphOption),
                parseResult.GetValue(focusProjectOption),
                parseResult.GetValue(focusNamespaceOption),
                parseResult.GetValue(focusClassOption));

            var logger = new ConsoleLogger(options.Verbose);
            var commandRunner = new AnalyzeCommand(logger);
            return await commandRunner.RunAsync(options);
        });

        var rootCommand = new RootCommand("Analyze .NET solutions and emit dependency artifacts.");
        rootCommand.Subcommands.Add(analyzeCommand);

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static void AddUnsupportedOption<T>(Command command, Option<T> option, bool expectsValue = false)
    {
        option.Hidden = true;
        option.Validators.Add(result =>
        {
            if (expectsValue || result.Tokens.Count > 0)
            {
                result.AddError($"Option '{option.Name}' is not implemented yet.");
            }
        });

        command.Options.Add(option);
    }
}
