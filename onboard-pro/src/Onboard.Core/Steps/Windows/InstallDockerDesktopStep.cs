namespace Onboard.Core.Steps.Windows;

using System;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;

/// <summary>
/// Installs Docker Desktop via winget and reminds the user to finish configuration.
/// </summary>
public class InstallDockerDesktopStep : IOnboardingStep
{
    private const string DetectionArguments = "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\"";
    private const string WingetCommand = "install --id Docker.DockerDesktop -e --source winget";

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;

    public InstallDockerDesktopStep(IProcessRunner processRunner, IUserInteraction userInteraction)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
    }

    public string Description => "Install Docker Desktop";

    public async Task<bool> ShouldExecuteAsync()
    {
        var result = await processRunner.RunAsync("powershell", DetectionArguments).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return true;
        }

        string output = result.StandardOutput.Trim();
        return !bool.TryParse(output, out bool isInstalled) || !isInstalled;
    }

    public async Task ExecuteAsync()
    {
        var result = await processRunner.RunAsync("winget", WingetCommand).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            string message = string.IsNullOrWhiteSpace(result.StandardError)
                ? "winget failed to install Docker Desktop."
                : result.StandardError.Trim();
            throw new InvalidOperationException(message);
        }

        userInteraction.WriteSuccess("Docker Desktop installed via winget.");
        userInteraction.WriteLine("Launch Docker Desktop, accept the terms, and enable WSL integration for Ubuntu-22.04.");
        userInteraction.WriteLine("Return to this onboarding tool once Docker Desktop reports it is running.");
    }
}
