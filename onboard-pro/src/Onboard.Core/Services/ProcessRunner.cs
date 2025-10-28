// <copyright file="ProcessRunner.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Services;

using System;
using System.Diagnostics;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Concrete implementation of IProcessRunner using System.Diagnostics.Process.
/// </summary>
public class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(string fileName, string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = startInfo };

            if (!process.Start())
            {
                return new ProcessResult(-1, string.Empty, $"Failed to start process '{fileName}'.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync().ConfigureAwait(false);

            string stdout = await outputTask.ConfigureAwait(false);
            string stderr = await errorTask.ConfigureAwait(false);

            return new ProcessResult(process.ExitCode, stdout, stderr);
        }
        catch (Exception ex)
        {
            return new ProcessResult(-1, string.Empty, ex.Message);
        }
    }
}
