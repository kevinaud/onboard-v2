// <copyright file="ProcessRunner.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Services;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Concrete implementation of IProcessRunner using System.Diagnostics.Process.
/// </summary>
public class ProcessRunner : IProcessRunner
{
    private const int MaxLoggedOutputLength = 1024;

    private readonly ILogger<ProcessRunner> logger;
    private readonly ExecutionOptions executionOptions;
    private readonly IUserInteraction userInteraction;

    public ProcessRunner(ILogger<ProcessRunner> logger, ExecutionOptions executionOptions, IUserInteraction userInteraction)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.executionOptions = executionOptions ?? throw new ArgumentNullException(nameof(executionOptions));
        this.userInteraction = userInteraction ?? throw new ArgumentNullException(nameof(userInteraction));
    }

    public async Task<ProcessResult> RunAsync(string fileName, string arguments)
    {
        try
        {
            string commandLine = FormatCommandLine(fileName, arguments);

            if (this.executionOptions.IsDryRun)
            {
                this.logger.LogDebug("Dry run enabled; skipping execution of {Command}", commandLine);

                if (this.executionOptions.IsVerbose)
                {
                    this.userInteraction.WriteDebug($"[DRY-RUN] Would execute: {commandLine}");
                }

                return new ProcessResult(0, string.Empty, string.Empty);
            }

            this.logger.LogDebug("Executing command {Command}", commandLine);

            if (this.executionOptions.IsVerbose)
            {
                this.userInteraction.WriteDebug($"Executing: {commandLine}");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                !startInfo.Environment.ContainsKey("DEBIAN_FRONTEND"))
            {
                startInfo.Environment["DEBIAN_FRONTEND"] = "noninteractive";
            }

            using var process = new Process { StartInfo = startInfo };

            if (!process.Start())
            {
                this.logger.LogWarning("Failed to start process {Command}", fileName);
                return new ProcessResult(-1, string.Empty, $"Failed to start process '{fileName}'.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync().ConfigureAwait(false);

            string stdout = await outputTask.ConfigureAwait(false);
            string stderr = await errorTask.ConfigureAwait(false);

            this.logger.LogDebug(
                "Command {Command} exited with code {ExitCode}. StdOut: {Stdout} StdErr: {Stderr}",
                commandLine,
                process.ExitCode,
                TruncateForLog(stdout),
                TruncateForLog(stderr));

            if (this.executionOptions.IsVerbose)
            {
                this.userInteraction.WriteDebug($"Completed with exit code {process.ExitCode}");

                if (!string.IsNullOrWhiteSpace(stdout))
                {
                    this.userInteraction.WriteDebug($"stdout: {stdout.TrimEnd()}");
                }

                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    this.userInteraction.WriteDebug($"stderr: {stderr.TrimEnd()}");
                }
            }

            return new ProcessResult(process.ExitCode, stdout, stderr);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unhandled exception while executing {Command} {Arguments}", fileName, arguments);
            return new ProcessResult(-1, string.Empty, ex.Message);
        }
    }

    private static string TruncateForLog(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= MaxLoggedOutputLength)
        {
            return value;
        }

        return string.Create(MaxLoggedOutputLength + 3, value, static (span, source) =>
        {
            source.AsSpan(0, MaxLoggedOutputLength).CopyTo(span);
            span[^3] = '.';
            span[^2] = '.';
            span[^1] = '.';
        });
    }

    private static string FormatCommandLine(string fileName, string arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return fileName;
        }

        return $"{fileName} {arguments}";
    }
}
