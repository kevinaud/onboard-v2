namespace Onboard.Core.Steps.Windows;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Ensures Git Credential Manager has an authenticated GitHub credential before development work begins.
/// </summary>
public class PreAuthenticateGitCredentialManagerStep : IOnboardingStep
{
  private const string GithubLoginCommand = "auth login --hostname github.com --git-protocol https --web";

  private readonly IProcessRunner processRunner;
  private readonly IUserInteraction userInteraction;
  private readonly OnboardingConfiguration configuration;

  public PreAuthenticateGitCredentialManagerStep(
    IProcessRunner processRunner,
    IUserInteraction userInteraction,
    OnboardingConfiguration configuration
  )
  {
    this.processRunner = processRunner;
    this.userInteraction = userInteraction;
    this.configuration = configuration;
  }

  public string Description => "Authenticate Git Credential Manager with GitHub";

  public async Task<bool> ShouldExecuteAsync()
  {
    var result = await processRunner
      .RunAsync("cmd.exe", BuildCredentialProbeArguments(configuration.GitCredentialManagerPath))
      .ConfigureAwait(false);
    if (!result.IsSuccess)
    {
      return true;
    }

    return !ContainsCredentialMarker(result.StandardOutput);
  }

  public async Task ExecuteAsync()
  {
    userInteraction.WriteNormal("Launching GitHub authentication flow via Git Credential Manager...");

    string? cliExecutable = await ResolveGitHubCliPathAsync().ConfigureAwait(false);

    ProcessResult result = string.IsNullOrWhiteSpace(cliExecutable)
      ? await processRunner
        .RunAsync("gh", GithubLoginCommand, requestElevation: false, useShellExecute: true)
        .ConfigureAwait(false)
      : await processRunner
        .RunAsync(cliExecutable, GithubLoginCommand, requestElevation: false, useShellExecute: true)
        .ConfigureAwait(false);
    if (!result.IsSuccess)
    {
      string message = string.IsNullOrWhiteSpace(result.StandardError)
        ? "GitHub authentication failed via Git Credential Manager."
        : result.StandardError.Trim();
      throw new InvalidOperationException(message);
    }

    userInteraction.WriteSuccess("Git Credential Manager authenticated with GitHub.");
  }

  private static string BuildCredentialProbeArguments(string executablePath)
  {
    if (string.IsNullOrWhiteSpace(executablePath))
    {
      throw new ArgumentException("Git Credential Manager path must be provided.", nameof(executablePath));
    }

    string escapedPath = executablePath.Replace("\"", "\\\"", StringComparison.Ordinal);
    var builder = new StringBuilder();
    builder.Append("/c \"set GCM_INTERACTIVE=never && (echo protocol=https & echo host=github.com & echo.) | \"");
    builder.Append(escapedPath);
    builder.Append("\" get\"");
    return builder.ToString();
  }

  private static bool ContainsCredentialMarker(string? output)
  {
    if (string.IsNullOrEmpty(output))
    {
      return false;
    }

    return output.IndexOf("password=", StringComparison.OrdinalIgnoreCase) >= 0
      || output.IndexOf("secret=", StringComparison.OrdinalIgnoreCase) >= 0;
  }

  private static IEnumerable<string> EnumerateLines(string input)
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

  private async Task<string?> ResolveGitHubCliPathAsync()
  {
    if (!string.IsNullOrWhiteSpace(configuration.GitHubCliPath) && File.Exists(configuration.GitHubCliPath))
    {
      return configuration.GitHubCliPath;
    }

    var result = await processRunner.RunAsync("where", "gh.exe").ConfigureAwait(false);
    if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StandardOutput))
    {
      return null;
    }

    foreach (string line in EnumerateLines(result.StandardOutput))
    {
      if (string.IsNullOrWhiteSpace(line))
      {
        continue;
      }

      string candidate = line.Trim();
      if (File.Exists(candidate))
      {
        configuration.GitHubCliPath = candidate;
        return candidate;
      }
    }

    return null;
  }
}
