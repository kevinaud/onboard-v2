namespace Onboard.Core.Models;

/// <summary>
/// Represents the result of running an external process.
/// </summary>
public record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    /// <summary>
    /// Returns true if the process completed successfully (exit code 0).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;
}
