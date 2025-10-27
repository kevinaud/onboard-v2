using Onboard.Core.Abstractions;

namespace Onboard.Core.Steps.Windows;

/// <summary>
/// Placeholder step for installing Git for Windows.
/// </summary>
public class InstallGitForWindowsStep : IOnboardingStep
{
    public string Description => "Install Git for Windows";

    public Task<bool> ShouldExecuteAsync()
    {
        return Task.FromResult(false);
    }

    public Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
