namespace Onboard.Core.Steps.MacOs;

using System;
using System.Threading.Tasks;
using Onboard.Core.Abstractions;

/// <summary>
/// Installs the required Homebrew packages for macOS development.
/// </summary>
public class InstallBrewPackagesStep : IOnboardingStep
{
  private const string DetectionCommand = "brew";
  private const string DetectionArguments = "list gh";
  private const string InstallArguments = "install git gh chezmoi";

  private readonly IProcessRunner processRunner;
  private readonly IUserInteraction userInteraction;

  public InstallBrewPackagesStep(IProcessRunner processRunner, IUserInteraction userInteraction)
  {
    this.processRunner = processRunner;
    this.userInteraction = userInteraction;
  }

  public string Description => "Install Homebrew packages";

  public async Task<bool> ShouldExecuteAsync()
  {
    var result = await processRunner.RunAsync(DetectionCommand, DetectionArguments).ConfigureAwait(false);
    return !result.IsSuccess;
  }

  public async Task ExecuteAsync()
  {
    var installResult = await processRunner.RunAsync(DetectionCommand, InstallArguments).ConfigureAwait(false);
    if (!installResult.IsSuccess)
    {
      string message = string.IsNullOrWhiteSpace(installResult.StandardError)
        ? "Failed to install Homebrew packages."
        : installResult.StandardError.Trim();
      throw new InvalidOperationException(message);
    }

    userInteraction.WriteSuccess("Homebrew packages installed (git, gh, chezmoi).");
  }
}
