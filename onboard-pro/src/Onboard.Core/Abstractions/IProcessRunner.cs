using Onboard.Core.Models;

namespace Onboard.Core.Abstractions;

/// <summary>
/// An abstraction for running external processes.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs an external process and returns the result.
    /// </summary>
    /// <param name="fileName">The executable or command to run.</param>
    /// <param name="arguments">The command-line arguments.</param>
    /// <returns>A ProcessResult containing exit code and output.</returns>
    Task<ProcessResult> RunAsync(string fileName, string arguments);
}
