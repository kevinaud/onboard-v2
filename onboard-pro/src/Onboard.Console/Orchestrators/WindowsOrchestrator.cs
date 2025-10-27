using Onboard.Core.Abstractions;

namespace Onboard.Console.Orchestrators;

/// <summary>
/// Orchestrator for Windows host onboarding.
/// </summary>
public class WindowsOrchestrator : IPlatformOrchestrator
{
    private readonly IUserInteraction _ui;
    private readonly IEnumerable<IOnboardingStep> _steps;

    public WindowsOrchestrator(
        IUserInteraction ui,
        IOnboardingStep configureGitUserStep)
    {
        _ui = ui;
        _steps = new[] { configureGitUserStep };
    }

    public async Task ExecuteAsync()
    {
        _ui.WriteHeader("Starting Windows Host Onboarding...");
        
        foreach (var step in _steps)
        {
            _ui.WriteLine($"Checking: {step.Description}");
            
            if (await step.ShouldExecuteAsync())
            {
                _ui.WriteLine($"Executing: {step.Description}");
                await step.ExecuteAsync();
            }
            else
            {
                _ui.WriteSuccess($"Already configured: {step.Description}");
            }
        }
        
        _ui.WriteLine("");
        _ui.WriteSuccess("Windows onboarding complete!");
    }
}
