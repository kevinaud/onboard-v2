namespace Onboard.Core.Steps.Ubuntu;

using System;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;

/// <summary>
/// Installs prerequisite packages on native Ubuntu machines.
/// </summary>
public class InstallAptPackagesStep : IOnboardingStep
{
    private const string DetectionCommand = "dpkg";
    private const string DetectionArguments = "-s build-essential";
    private const string InstallCommand = "sudo";
    private const string InstallArguments = "apt-get install -y git gh curl chezmoi python3 build-essential";

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;

    public InstallAptPackagesStep(IProcessRunner processRunner, IUserInteraction userInteraction)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
    }

    public string Description => "Install apt packages";

    public async Task<bool> ShouldExecuteAsync()
    {
        var result = await processRunner.RunAsync(DetectionCommand, DetectionArguments).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return true;
        }

        return !result.StandardOutput.Contains("Status: install ok installed", StringComparison.OrdinalIgnoreCase);
    }

    public async Task ExecuteAsync()
    {
        var installResult = await processRunner.RunAsync(InstallCommand, InstallArguments).ConfigureAwait(false);
        if (!installResult.IsSuccess)
        {
            string message = string.IsNullOrWhiteSpace(installResult.StandardError)
                ? "Failed to install apt packages."
                : installResult.StandardError.Trim();
            throw new InvalidOperationException(message);
        }

        userInteraction.WriteSuccess("Apt packages installed.");
    }
}
