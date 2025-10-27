using Onboard.Core.Abstractions;

namespace Onboard.Console.Orchestrators;

/// <summary>
/// Orchestrator for WSL guest onboarding.
/// </summary>
public class WslGuestOrchestrator : IPlatformOrchestrator
{
    private readonly IUserInteraction _ui;

    public WslGuestOrchestrator(IUserInteraction ui)
    {
        _ui = ui;
    }

    public async Task ExecuteAsync()
    {
        _ui.WriteHeader("Starting WSL Guest Onboarding...");
        await Task.CompletedTask;
    }
}
