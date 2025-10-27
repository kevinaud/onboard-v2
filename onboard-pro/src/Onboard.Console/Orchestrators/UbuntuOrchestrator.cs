using Onboard.Core.Abstractions;

namespace Onboard.Console.Orchestrators;

/// <summary>
/// Orchestrator for Ubuntu (native Linux) onboarding.
/// </summary>
public class UbuntuOrchestrator : IPlatformOrchestrator
{
    private readonly IUserInteraction _ui;
    private readonly IEnumerable<IOnboardingStep> _steps;

    public UbuntuOrchestrator(
        IUserInteraction ui,
        IOnboardingStep configureGitUserStep)
    {
        _ui = ui;
        _steps = new[] { configureGitUserStep };
    }

    public async Task ExecuteAsync()
    {
        _ui.WriteHeader("Starting Ubuntu Onboarding...");
        
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
        _ui.WriteSuccess("Ubuntu onboarding complete!");
    }
}
