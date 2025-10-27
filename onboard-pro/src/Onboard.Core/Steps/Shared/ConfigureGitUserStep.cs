using Onboard.Core.Abstractions;

namespace Onboard.Core.Steps.Shared;

/// <summary>
/// Placeholder step for configuring Git user identity.
/// </summary>
public class ConfigureGitUserStep : IOnboardingStep
{
    public string Description => "Configure Git user identity";

    public Task<bool> ShouldExecuteAsync()
    {
        return Task.FromResult(false);
    }

    public Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
