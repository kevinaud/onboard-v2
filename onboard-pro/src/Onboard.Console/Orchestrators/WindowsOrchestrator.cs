using Onboard.Core.Abstractions;

namespace Onboard.Console.Orchestrators;

/// <summary>
/// Orchestrator for Windows host onboarding.
/// </summary>
public class WindowsOrchestrator : IPlatformOrchestrator
{
    private readonly IUserInteraction _ui;

    public WindowsOrchestrator(IUserInteraction ui)
    {
        _ui = ui;
    }

    public async Task ExecuteAsync()
    {
        _ui.WriteHeader("Starting Windows Host Onboarding...");
        await Task.CompletedTask;
    }
}
