namespace Onboard.Console.Orchestrators;

using System;

/// <summary>
/// Represents a failure that occurred while executing an onboarding step.
/// </summary>
public sealed class OnboardingStepException : Exception
{
    public OnboardingStepException()
    {
    }

    public OnboardingStepException(string message)
        : base(message)
    {
    }

    public OnboardingStepException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static OnboardingStepException CheckFailed(string stepDescription, Exception innerException)
    {
        string message = $"Failed while checking '{stepDescription}': {innerException.Message}";
        return new OnboardingStepException(message, innerException);
    }

    public static OnboardingStepException ExecutionFailed(string stepDescription, Exception innerException)
    {
        string message = $"Step '{stepDescription}' failed: {innerException.Message}";
        return new OnboardingStepException(message, innerException);
    }
}
