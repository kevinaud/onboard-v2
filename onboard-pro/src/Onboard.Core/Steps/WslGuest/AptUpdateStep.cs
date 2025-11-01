namespace Onboard.Core.Steps.WslGuest;

using System;
using System.Threading.Tasks;
using Onboard.Core.Abstractions;

/// <summary>
/// Runs apt-get update to refresh package metadata inside WSL.
/// </summary>
public class AptUpdateStep : IOnboardingStep
{
  private readonly IProcessRunner processRunner;
  private readonly IUserInteraction userInteraction;

  public AptUpdateStep(IProcessRunner processRunner, IUserInteraction userInteraction)
  {
    this.processRunner = processRunner;
    this.userInteraction = userInteraction;
  }

  public string Description => "Update apt package lists";

  public Task<bool> ShouldExecuteAsync()
  {
    // Refreshing package metadata is fast and safe to run on every invocation.
    return Task.FromResult(true);
  }

  public async Task ExecuteAsync()
  {
    var result = await processRunner.RunAsync("sudo", "apt-get update").ConfigureAwait(false);
    if (!result.IsSuccess)
    {
      string message = string.IsNullOrWhiteSpace(result.StandardError)
        ? "Failed to update apt package lists."
        : result.StandardError.Trim();
      throw new InvalidOperationException(message);
    }

    userInteraction.WriteSuccess("APT package lists updated.");
  }
}
