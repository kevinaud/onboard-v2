using System.Diagnostics;
using Onboard.Core.Abstractions;
using Onboard.Core.Models;

namespace Onboard.Core.Services;

/// <summary>
/// Concrete implementation of IProcessRunner using System.Diagnostics.Process.
/// </summary>
public class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdout = await outputTask;
        var stderr = await errorTask;

        return new ProcessResult(process.ExitCode, stdout, stderr);
    }
}
