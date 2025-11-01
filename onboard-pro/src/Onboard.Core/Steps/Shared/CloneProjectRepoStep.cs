// <copyright file="CloneProjectRepoStep.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Steps.Shared;

using System.Globalization;
using System.IO;
using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Clones (or updates) the target project repository into the developer workspace.
/// </summary>
public class CloneProjectRepoStep : IOnboardingStep
{
  private const string WorkspaceEnvironmentVariable = "ONBOARD_WORKSPACE_DIR";
  private const string DefaultWorkspaceDirectoryName = "projects";
  private const string RepositoryName = "mental-health-app-frontend";
  private const string RepositoryUrl = "https://github.com/psps-mental-health-app/mental-health-app-frontend.git";

  private readonly IProcessRunner processRunner;
  private readonly IUserInteraction userInteraction;
  private readonly IFileSystem fileSystem;
  private readonly PlatformFacts platformFacts;

  public CloneProjectRepoStep(
    IProcessRunner processRunner,
    IUserInteraction userInteraction,
    IFileSystem fileSystem,
    PlatformFacts platformFacts
  )
  {
    this.processRunner = processRunner;
    this.userInteraction = userInteraction;
    this.fileSystem = fileSystem;
    this.platformFacts = platformFacts;
  }

  public string Description => "Clone project repository";

  public Task<bool> ShouldExecuteAsync()
  {
    var paths = ResolvePaths();

    if (!fileSystem.DirectoryExists(paths.RepositoryPath))
    {
      return Task.FromResult(true);
    }

    if (IsGitRepository(paths.RepositoryPath))
    {
      // Repository exists; run ExecuteAsync to fetch latest changes.
      return Task.FromResult(true);
    }

    userInteraction.WriteWarning(
      $"Path '{paths.RepositoryPath}' exists but is not a Git repository. Move or remove it, then re-run onboarding."
    );
    return Task.FromResult(false);
  }

  public async Task ExecuteAsync()
  {
    var paths = this.ResolvePaths();

    if (!this.fileSystem.DirectoryExists(paths.WorkspacePath))
    {
      this.userInteraction.WriteNormal($"Creating workspace directory at {paths.WorkspacePath}");
      this.fileSystem.CreateDirectory(paths.WorkspacePath);
    }

    if (!this.fileSystem.DirectoryExists(paths.RepositoryPath))
    {
      await this.CloneRepositoryAsync(paths.RepositoryPath).ConfigureAwait(false);
      return;
    }

    if (!this.IsGitRepository(paths.RepositoryPath))
    {
      // Safety net: bail out if a non-git directory suddenly appears between ShouldExecuteAsync and ExecuteAsync.
      userInteraction.WriteWarning(
        $"Path '{paths.RepositoryPath}' exists but is not a Git repository. Skipping clone/update."
      );
      return;
    }

    await UpdateRepositoryAsync(paths.RepositoryPath).ConfigureAwait(false);
  }

  private async Task CloneRepositoryAsync(string repositoryPath)
  {
    string arguments = string.Create(CultureInfo.InvariantCulture, $"clone {RepositoryUrl} \"{repositoryPath}\"");
    var result = await processRunner.RunAsync("git", arguments).ConfigureAwait(false);

    if (!result.IsSuccess)
    {
      string message = string.IsNullOrWhiteSpace(result.StandardError)
        ? "Failed to clone repository."
        : result.StandardError.Trim();
      throw new InvalidOperationException(message);
    }

    userInteraction.WriteSuccess($"Repository cloned to {repositoryPath}.");
  }

  private async Task UpdateRepositoryAsync(string repositoryPath)
  {
    string arguments = string.Create(CultureInfo.InvariantCulture, $"-C \"{repositoryPath}\" pull --ff-only");
    var result = await processRunner.RunAsync("git", arguments).ConfigureAwait(false);

    if (!result.IsSuccess)
    {
      string message = string.IsNullOrWhiteSpace(result.StandardError)
        ? "Failed to update repository."
        : result.StandardError.Trim();
      throw new InvalidOperationException(message);
    }

    userInteraction.WriteSuccess("Repository updated to the latest changes.");
  }

  private bool IsGitRepository(string repositoryPath)
  {
    string gitDirectory = Path.Combine(repositoryPath, ".git");
    return fileSystem.DirectoryExists(gitDirectory);
  }

  private (string WorkspacePath, string RepositoryPath) ResolvePaths()
  {
    string? workspace = Environment.GetEnvironmentVariable(WorkspaceEnvironmentVariable);
    workspace = NormalizeWorkspacePath(
      string.IsNullOrWhiteSpace(workspace)
        ? Path.Combine(platformFacts.HomeDirectory, DefaultWorkspaceDirectoryName)
        : workspace!
    );

    string repositoryPath = Path.Combine(workspace, RepositoryName);
    return (workspace, repositoryPath);
  }

  private string NormalizeWorkspacePath(string rawPath)
  {
    string expanded = ExpandTilde(Environment.ExpandEnvironmentVariables(rawPath));

    if (!Path.IsPathRooted(expanded) && !LooksLikeWindowsPath(expanded))
    {
      expanded = Path.Combine(platformFacts.HomeDirectory, expanded);
    }

    if (platformFacts.IsWsl && LooksLikeWindowsPath(expanded))
    {
      expanded = ConvertWindowsPathToWsl(expanded);
    }

    return Path.GetFullPath(expanded);
  }

  private string ExpandTilde(string path)
  {
    if (string.Equals(path, "~", StringComparison.Ordinal))
    {
      return platformFacts.HomeDirectory;
    }

    if (path.StartsWith("~/", StringComparison.Ordinal))
    {
      string remainder = path[2..];
      return Path.Combine(platformFacts.HomeDirectory, remainder);
    }

    return path;
  }

  private bool LooksLikeWindowsPath(string path)
  {
    return path.Length >= 2 && path[1] == ':' && char.IsLetter(path[0]);
  }

  private string ConvertWindowsPathToWsl(string path)
  {
    char driveLetter = char.ToLowerInvariant(path[0]);
    string remainder = path[2..].Replace('\\', '/').TrimStart('/');
    return $"/mnt/{driveLetter}/{remainder}";
  }
}
