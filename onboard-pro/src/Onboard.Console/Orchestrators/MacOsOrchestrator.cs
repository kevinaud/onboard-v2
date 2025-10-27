using Onboard.Core.Abstractions;

namespace Onboard.Console.Orchestrators;

/// <summary>
/// Orchestrator for macOS onboarding.
/// </summary>
public class MacOsOrchestrator : IPlatformOrchestrator
{
    private readonly IUserInteraction _ui;

    public MacOsOrchestrator(IUserInteraction ui)
    {
        _ui = ui;
    }

    public async Task ExecuteAsync()
    {
        _ui.WriteHeader("Starting macOS Onboarding...");
        await Task.CompletedTask;
    }
}
