namespace Onboard.Core.Abstractions;

/// <summary>
/// Interface for platform-specific orchestrators that manage the execution of onboarding steps.
/// </summary>
public interface IPlatformOrchestrator
{
    /// <summary>
    /// Executes the onboarding process for this platform.
    /// </summary>
    Task ExecuteAsync();
}
