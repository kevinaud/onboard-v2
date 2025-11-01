namespace Onboard.Core.Steps.WslGuest;

using System;
using System.Threading.Tasks;
using Onboard.Core.Abstractions;

/// <summary>
/// Installs the baseline tooling required for the WSL guest environment.
/// </summary>
public class InstallWslPrerequisitesStep : IOnboardingStep
{
  private const string PackageDetectionArguments = "-s build-essential";
  private const string InstallArguments = "apt-get install -y git gh curl chezmoi python3 build-essential";

  private readonly IProcessRunner processRunner;
  private readonly IUserInteraction userInteraction;

  public InstallWslPrerequisitesStep(IProcessRunner processRunner, IUserInteraction userInteraction)
  {
    this.processRunner = processRunner;
    this.userInteraction = userInteraction;
  }

  public string Description => "Install WSL prerequisites";

  public async Task<bool> ShouldExecuteAsync()
  {
    var result = await processRunner.RunAsync("dpkg", PackageDetectionArguments).ConfigureAwait(false);
    if (!result.IsSuccess)
    {
      return true;
    }

    return !result.StandardOutput.Contains("Status: install ok installed", StringComparison.OrdinalIgnoreCase);
  }

  public async Task ExecuteAsync()
  {
    var result = await processRunner.RunAsync("sudo", InstallArguments).ConfigureAwait(false);
    if (!result.IsSuccess)
    {
      string message = string.IsNullOrWhiteSpace(result.StandardError)
        ? "apt-get failed to install prerequisite packages."
        : result.StandardError.Trim();
      throw new InvalidOperationException(message);
    }

    userInteraction.WriteSuccess("WSL prerequisites installed.");
  }
}
