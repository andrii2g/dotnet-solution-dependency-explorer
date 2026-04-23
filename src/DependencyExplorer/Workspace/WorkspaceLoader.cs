using A2G.DependencyExplorer.Models;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Xml.Linq;

namespace A2G.DependencyExplorer.Workspace;

internal sealed class WorkspaceLoader
{
    private static readonly object RegistrationLock = new();
    private static bool _isRegistered;

    public async Task<WorkspaceLoadResult> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken)
    {
        EnsureMsBuildRegistered();

        using var workspace = MSBuildWorkspace.Create();
        Solution openedSolution;
        var extension = Path.GetExtension(solutionPath);
        if (string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase))
        {
            openedSolution = await OpenSlnxAsync(workspace, solutionPath, cancellationToken);
        }
        else
        {
            openedSolution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);
        }

        var diagnostics = workspace.Diagnostics
            .Select(diagnostic => new WorkspaceDiagnosticInfo
            {
                Kind = diagnostic.Kind.ToString(),
                Message = diagnostic.Message,
            })
            .ToArray();

        return new WorkspaceLoadResult(openedSolution, diagnostics);
    }

    private static async Task<Solution> OpenSlnxAsync(
        MSBuildWorkspace workspace,
        string solutionPath,
        CancellationToken cancellationToken)
    {
        var projectPaths = await ReadProjectPathsFromSlnxAsync(solutionPath, cancellationToken);
        if (projectPaths.Count == 0)
        {
            throw new InvalidOperationException($"No projects were found in the .slnx file: {solutionPath}");
        }

        var loadedProjectPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var projectPath in projectPaths)
        {
            if (loadedProjectPaths.Contains(projectPath))
            {
                continue;
            }

            await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
            foreach (var loadedProjectPath in workspace.CurrentSolution.Projects
                .Select(project => project.FilePath)
                .Where(path => !string.IsNullOrWhiteSpace(path)))
            {
                loadedProjectPaths.Add(Path.GetFullPath(loadedProjectPath!));
            }
        }

        return workspace.CurrentSolution;
    }

    private static async Task<IReadOnlyList<string>> ReadProjectPathsFromSlnxAsync(
        string solutionPath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(solutionPath);
        var document = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);
        var baseDirectory = Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory;

        return document
            .Descendants()
            .Where(element => element.Name.LocalName == "Project")
            .Select(element => element.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => Path.GetFullPath(Path.Combine(baseDirectory, path!)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void EnsureMsBuildRegistered()
    {
        lock (RegistrationLock)
        {
            if (_isRegistered || MSBuildLocator.IsRegistered)
            {
                _isRegistered = true;
                return;
            }

            MSBuildLocator.RegisterDefaults();
            _isRegistered = true;
        }
    }
}

internal sealed class WorkspaceLoadResult
{
    private readonly Dictionary<ProjectId, string> _projectNames;

    public WorkspaceLoadResult(Microsoft.CodeAnalysis.Solution solution, IReadOnlyList<WorkspaceDiagnosticInfo> diagnostics)
    {
        Solution = solution;
        Diagnostics = diagnostics;
        Projects = solution.Projects.ToArray();
        _projectNames = Projects.ToDictionary(project => project.Id, project => project.Name);
    }

    public Microsoft.CodeAnalysis.Solution Solution { get; }

    public IReadOnlyList<Microsoft.CodeAnalysis.Project> Projects { get; }

    public IReadOnlyList<WorkspaceDiagnosticInfo> Diagnostics { get; }

    public string? GetProjectName(ProjectId projectId)
    {
        return _projectNames.GetValueOrDefault(projectId);
    }
}
