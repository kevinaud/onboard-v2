namespace Onboard.Core.Models;

/// <summary>
/// Holds execution-wide flags for the onboarding workflow.
/// </summary>
public record ExecutionOptions(bool IsDryRun, bool IsVerbose);
