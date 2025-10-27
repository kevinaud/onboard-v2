using Onboard.Core.Abstractions;

namespace Onboard.Console.Orchestrators;

/// <summary>
/// Orchestrator for Ubuntu (native Linux) onboarding.
/// </summary>
public class UbuntuOrchestrator : IPlatformOrchestrator
{
    private readonly IUserInteraction _ui;

    public UbuntuOrchestrator(IUserInteraction ui)
    {
        _ui = ui;
    }

    public async Task ExecuteAsync()
    {
        _ui.WriteHeader("Starting Ubuntu Onboarding...");
        await Task.CompletedTask;
    }
}
