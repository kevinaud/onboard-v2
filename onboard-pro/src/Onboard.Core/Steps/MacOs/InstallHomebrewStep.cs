namespace Onboard.Core.Steps.MacOs;

using System;
using System.Threading.Tasks;
using Onboard.Core.Abstractions;

/// <summary>
/// Installs Homebrew when it is missing from the system.
/// </summary>
public class InstallHomebrewStep : IOnboardingStep
{
  private const string DetectionCommand = "which";
  private const string DetectionArguments = "brew";
  private const string InstallerCommand = "/bin/bash";
  private const string InstallerArguments =
    "-c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\"";

  private readonly IProcessRunner processRunner;
  private readonly IUserInteraction userInteraction;

  public InstallHomebrewStep(IProcessRunner processRunner, IUserInteraction userInteraction)
  {
    this.processRunner = processRunner;
    this.userInteraction = userInteraction;
  }

  public string Description => "Install Homebrew";

  public async Task<bool> ShouldExecuteAsync()
  {
    var result = await processRunner.RunAsync(DetectionCommand, DetectionArguments).ConfigureAwait(false);
    return !result.IsSuccess || string.IsNullOrWhiteSpace(result.StandardOutput);
  }

  public async Task ExecuteAsync()
  {
    var installResult = await processRunner.RunAsync(InstallerCommand, InstallerArguments).ConfigureAwait(false);
    if (!installResult.IsSuccess)
    {
      string message = string.IsNullOrWhiteSpace(installResult.StandardError)
        ? "Failed to install Homebrew."
        : installResult.StandardError.Trim();
      throw new InvalidOperationException(message);
    }

    userInteraction.WriteSuccess("Homebrew installed.");
  }
}
