using Onboard.Core.Models;

namespace Onboard.Core.Abstractions;

/// <summary>
/// An abstraction for detecting platform information.
/// </summary>
public interface IPlatformDetector
{
    /// <summary>
    /// Detects and returns the current platform facts.
    /// </summary>
    /// <returns>An immutable PlatformFacts record containing OS, architecture, and environment details.</returns>
    PlatformFacts Detect();
}
