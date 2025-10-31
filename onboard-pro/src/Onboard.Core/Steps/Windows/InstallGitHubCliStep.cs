namespace Onboard.Core.Steps.Windows;

using System;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;

/// <summary>
/// Ensures the GitHub CLI is installed on Windows hosts.
/// </summary>
public class InstallGitHubCliStep : IOnboardingStep
{
    private const string WingetArguments = "install --id GitHub.cli -e --source winget --accept-package-agreements --accept-source-agreements --disable-interactivity";

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;

    public InstallGitHubCliStep(IProcessRunner processRunner, IUserInteraction userInteraction)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
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

        userInteraction.WriteSuccess("GitHub CLI installed via winget.");
    }
}
