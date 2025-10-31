namespace Onboard.Core.Steps.Windows;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Ensures the GitHub CLI is installed on Windows hosts.
/// </summary>
public class InstallGitHubCliStep : IOnboardingStep
{
  private const string WingetArguments =
    "install --id GitHub.cli -e --source winget --accept-package-agreements --accept-source-agreements --disable-interactivity";

  private readonly IProcessRunner processRunner;
  private readonly IUserInteraction userInteraction;
  private readonly OnboardingConfiguration configuration;
  private readonly IEnvironmentRefresher environmentRefresher;

  public InstallGitHubCliStep(
    IProcessRunner processRunner,
    IUserInteraction userInteraction,
    OnboardingConfiguration configuration,
    IEnvironmentRefresher environmentRefresher
  )
  {
    this.processRunner = processRunner;
    this.userInteraction = userInteraction;
    this.configuration = configuration;
    this.environmentRefresher = environmentRefresher;
  }

  public string Description => "Install GitHub CLI";

  public async Task<bool> ShouldExecuteAsync()
  {
    var result = await processRunner.RunAsync("where", "gh.exe").ConfigureAwait(false);
    return !result.IsSuccess;
  }

  public async Task ExecuteAsync()
  {
    var result = await processRunner.RunAsync("winget", WingetArguments).ConfigureAwait(false);
    if (!result.IsSuccess)
    {
      string message = string.IsNullOrWhiteSpace(result.StandardError)
        ? "winget failed to install the GitHub CLI."
        : result.StandardError.Trim();
      throw new InvalidOperationException(message);
    }

    await environmentRefresher.RefreshAsync().ConfigureAwait(false);
    await CaptureGitHubCliPathAsync().ConfigureAwait(false);

    userInteraction.WriteSuccess("GitHub CLI installed via winget.");
  }

  private async Task CaptureGitHubCliPathAsync()
  {
    var result = await processRunner.RunAsync("where", "gh.exe").ConfigureAwait(false);
    if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StandardOutput))
    {
      return;
    }

    foreach (string candidate in GitHubCliStepHelpers.EnumerateLines(result.StandardOutput))
    {
      if (string.IsNullOrWhiteSpace(candidate))
      {
        continue;
      }

      string trimmed = candidate.Trim();
      if (!File.Exists(trimmed))
      {
        continue;
      }

      configuration.GitHubCliPath = trimmed;
      GitHubCliStepHelpers.EnsurePathContains(Path.GetDirectoryName(trimmed));
      break;
    }
  }

  private static class GitHubCliStepHelpers
  {
    public static IEnumerable<string> EnumerateLines(string input)
    {
      int start = 0;
      for (int i = 0; i < input.Length; i++)
      {
        char c = input[i];
        if (c == '\r' || c == '\n')
        {
          if (i > start)
          {
            yield return input[start..i];
          }

          if (c == '\r' && i + 1 < input.Length && input[i + 1] == '\n')
          {
            i++;
          }

          start = i + 1;
        }
      }

      if (start < input.Length)
      {
        yield return input[start..];
      }
    }

    public static void EnsurePathContains(string? directory)
    {
      if (string.IsNullOrWhiteSpace(directory))
      {
        return;
      }

      string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
      foreach (string segment in currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries))
      {
        if (string.Equals(segment.Trim(), directory, StringComparison.OrdinalIgnoreCase))
        {
          return;
        }
      }

      string newPath = string.IsNullOrEmpty(currentPath) ? directory : string.Concat(directory, ";", currentPath);

      Environment.SetEnvironmentVariable("PATH", newPath);
    }
  }
}
