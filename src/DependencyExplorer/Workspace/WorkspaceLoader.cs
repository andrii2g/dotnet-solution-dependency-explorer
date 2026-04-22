using DependencyExplorer.Models;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

namespace DependencyExplorer.Workspace;

internal sealed class WorkspaceLoader
{
    private static readonly object RegistrationLock = new();
    private static bool _isRegistered;

    public async Task<WorkspaceLoadResult> LoadSolutionAsync(string solutionPath, CancellationToken cancellationToken)
    {
        EnsureMsBuildRegistered();

        using var workspace = MSBuildWorkspace.Create();
        var openedSolution = await workspace.OpenSolutionAsync(solutionPath, cancellationToken: cancellationToken);

        var diagnostics = workspace.Diagnostics
            .Select(diagnostic => new WorkspaceDiagnosticInfo
            {
                Kind = diagnostic.Kind.ToString(),
                Message = diagnostic.Message,
            })
            .ToArray();

        return new WorkspaceLoadResult(openedSolution, diagnostics);
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
