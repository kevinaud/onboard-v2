namespace Onboard.Core.Abstractions;

/// <summary>
/// The base contract for all onboarding steps.
/// </summary>
public interface IOnboardingStep
{
    /// <summary>
    /// User-friendly description for progress reporting.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The idempotency check. Returns true if the step needs to run, false if complete.
    /// </summary>
    /// <returns>True if the step should execute, false if it's already complete.</returns>
    Task<bool> ShouldExecuteAsync();

    /// <summary>
    /// The action. This only runs if ShouldExecuteAsync() returns true.
    /// </summary>
    Task ExecuteAsync();
}
