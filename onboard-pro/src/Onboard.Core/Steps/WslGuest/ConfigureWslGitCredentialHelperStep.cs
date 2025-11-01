namespace Onboard.Core.Steps.WslGuest;

using System;
using System.Threading.Tasks;
using Onboard.Core.Abstractions;

/// <summary>
/// Configures Git to use the Windows Git Credential Manager from inside WSL.
/// </summary>
public class ConfigureWslGitCredentialHelperStep : IOnboardingStep
{
  private const string HelperPath = "/mnt/c/Program Files/Git/mingw64/bin/git-credential-manager.exe";
  private const string QueryArguments = "config --global credential.helper";

  private readonly IProcessRunner processRunner;
  private readonly IUserInteraction userInteraction;

  public ConfigureWslGitCredentialHelperStep(IProcessRunner processRunner, IUserInteraction userInteraction)
  {
    this.processRunner = processRunner;
    this.userInteraction = userInteraction;
  }

  public string Description => "Configure Git credential helper";

  public async Task<bool> ShouldExecuteAsync()
  {
    var result = await processRunner.RunAsync("git", QueryArguments).ConfigureAwait(false);
    if (!result.IsSuccess)
    {
      return true;
    }

    string currentValue = result.StandardOutput.Trim();
    return !string.Equals(currentValue, HelperPath, StringComparison.OrdinalIgnoreCase);
  }

  public async Task ExecuteAsync()
  {
    string setArguments = $"config --global credential.helper \"{HelperPath}\"";
    var result = await processRunner.RunAsync("git", setArguments).ConfigureAwait(false);
    if (!result.IsSuccess)
    {
      string message = string.IsNullOrWhiteSpace(result.StandardError)
        ? "Failed to configure Git credential helper."
        : result.StandardError.Trim();
      throw new InvalidOperationException(message);
    }

    userInteraction.WriteSuccess("Configured Git credential helper for Windows GCM.");
  }
}
