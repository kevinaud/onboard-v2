namespace Onboard.Core.Services;

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Onboard.Core.Abstractions;

/// <summary>
/// Refreshes the current process environment variables from machine and user scopes.
/// </summary>
public sealed class EnvironmentRefresher : IEnvironmentRefresher
{
  public Task RefreshAsync(CancellationToken cancellationToken = default)
  {
    ApplyScope(EnvironmentVariableTarget.Machine, cancellationToken);
    ApplyScope(EnvironmentVariableTarget.User, cancellationToken);

    string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
    string userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
    string combinedPath = CombinePath(machinePath, userPath);

    Environment.SetEnvironmentVariable("PATH", combinedPath, EnvironmentVariableTarget.Process);
    return Task.CompletedTask;
  }

  private static void ApplyScope(EnvironmentVariableTarget scope, CancellationToken cancellationToken)
  {
    IDictionary variables = Environment.GetEnvironmentVariables(scope);
    foreach (DictionaryEntry entry in variables)
    {
      cancellationToken.ThrowIfCancellationRequested();
      string key = (string)entry.Key;
      string? value = entry.Value?.ToString();
      Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
    }
  }

  private static string CombinePath(string machinePath, string userPath)
  {
    if (string.IsNullOrWhiteSpace(machinePath))
    {
      return userPath ?? string.Empty;
    }

    if (string.IsNullOrWhiteSpace(userPath))
    {
      return machinePath;
    }

    return string.Concat(machinePath.TrimEnd(';'), ";", userPath.TrimStart(';'));
  }
}
