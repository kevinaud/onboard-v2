using Onboard.Core.Abstractions;

namespace Onboard.Core.Steps.PlatformAware;

/// <summary>
/// Placeholder step for installing VS Code.
/// </summary>
public class InstallVsCodeStep : IOnboardingStep
{
    public string Description => "Install Visual Studio Code";

    public Task<bool> ShouldExecuteAsync()
    {
        return Task.FromResult(false);
    }

    public Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
