// <copyright file="ProcessRunner.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Services;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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

  public ProcessRunner(
    ILogger<ProcessRunner> logger,
    ExecutionOptions executionOptions,
    IUserInteraction userInteraction
  )
  {
    this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    this.executionOptions = executionOptions ?? throw new ArgumentNullException(nameof(executionOptions));
    this.userInteraction = userInteraction ?? throw new ArgumentNullException(nameof(userInteraction));
  }

  public Task<ProcessResult> RunAsync(string fileName, string arguments)
  {
    return this.RunAsync(fileName, arguments, requestElevation: false, useShellExecute: false);
  }

  public Task<ProcessResult> RunAsync(string fileName, string arguments, bool requestElevation)
  {
    return this.RunAsync(fileName, arguments, requestElevation, useShellExecute: false);
  }

  public async Task<ProcessResult> RunAsync(
    string fileName,
    string arguments,
    bool requestElevation,
    bool useShellExecute
  )
  {
    if (requestElevation)
    {
      if (useShellExecute)
      {
        this.logger.LogWarning(
          "useShellExecute is ignored when requestElevation is true for command {Command}",
          FormatCommandLine(fileName, arguments)
        );
      }

      return await this.RunElevatedWindowsAsync(fileName, arguments).ConfigureAwait(false);
    }

    return await this.RunStandardAsync(fileName, arguments, useShellExecute).ConfigureAwait(false);
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

    return string.Create(
      MaxLoggedOutputLength + 3,
      value,
      static (span, source) =>
      {
        source.AsSpan(0, MaxLoggedOutputLength).CopyTo(span);
        span[^3] = '.';
        span[^2] = '.';
        span[^1] = '.';
      }
    );
  }

  private static string FormatCommandLine(string fileName, string arguments)
  {
    if (string.IsNullOrWhiteSpace(arguments))
    {
      return fileName;
    }

    return $"{fileName} {arguments}";
  }

  private static string EscapeForSingleQuotedPowerShell(string value)
  {
    return value.Replace("'", "''", StringComparison.Ordinal);
  }

  private static string BuildElevatedPowerShellScript(string fileName, string arguments, string tempFile)
  {
    var builder = new StringBuilder();
    builder.Append("& '");
    builder.Append(EscapeForSingleQuotedPowerShell(fileName));
    builder.Append('\'');

    if (!string.IsNullOrWhiteSpace(arguments))
    {
      builder.Append(' ');
      builder.Append(arguments);
    }

    builder.Append(" *> '");
    builder.Append(EscapeForSingleQuotedPowerShell(tempFile));
    builder.Append('\'');
    builder.AppendLine();
    builder.AppendLine("$exitCode = $LASTEXITCODE");
    builder.AppendLine("exit $exitCode");

    return builder.ToString();
  }

  private async Task<ProcessResult> RunStandardAsync(string fileName, string arguments, bool useShellExecute)
  {
    string commandLine = FormatCommandLine(fileName, arguments);

    try
    {
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

      bool redirect = !useShellExecute;

      var startInfo = new ProcessStartInfo
      {
        FileName = fileName,
        Arguments = arguments,
        RedirectStandardOutput = redirect,
        RedirectStandardError = redirect,
        UseShellExecute = useShellExecute,
        CreateNoWindow = redirect,
      };

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !startInfo.Environment.ContainsKey("DEBIAN_FRONTEND"))
      {
        startInfo.Environment["DEBIAN_FRONTEND"] = "noninteractive";
      }

      using var process = new Process { StartInfo = startInfo };

      if (!process.Start())
      {
        this.logger.LogWarning("Failed to start process {Command}", fileName);
        return new ProcessResult(-1, string.Empty, $"Failed to start process '{fileName}'.");
      }

      Task<string> outputTask = redirect ? process.StandardOutput.ReadToEndAsync() : Task.FromResult(string.Empty);
      Task<string> errorTask = redirect ? process.StandardError.ReadToEndAsync() : Task.FromResult(string.Empty);

      await process.WaitForExitAsync().ConfigureAwait(false);

      string stdout = await outputTask.ConfigureAwait(false);
      string stderr = await errorTask.ConfigureAwait(false);

      this.logger.LogDebug(
        "Command {Command} exited with code {ExitCode}. StdOut: {Stdout} StdErr: {Stderr}",
        commandLine,
        process.ExitCode,
        TruncateForLog(stdout),
        TruncateForLog(stderr)
      );

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
      this.logger.LogError(ex, "Unhandled exception while executing {Command}", commandLine);
      return new ProcessResult(-1, string.Empty, ex.Message);
    }
  }

  private async Task<ProcessResult> RunElevatedWindowsAsync(string fileName, string arguments)
  {
    string commandLine = FormatCommandLine(fileName, arguments);

    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      this.logger.LogWarning(
        "Elevation requested for {Command} on non-Windows platform; falling back to standard execution.",
        commandLine
      );
      return await this.RunStandardAsync(fileName, arguments, useShellExecute: false).ConfigureAwait(false);
    }

    if (this.executionOptions.IsDryRun)
    {
      this.logger.LogDebug("Dry run enabled; skipping elevated execution of {Command}", commandLine);

      if (this.executionOptions.IsVerbose)
      {
        this.userInteraction.WriteDebug($"[DRY-RUN][ELEVATED] Would execute: {commandLine}");
      }

      return new ProcessResult(0, string.Empty, string.Empty);
    }

    string tempFile = Path.Combine(Path.GetTempPath(), $"onboard-pro-{Guid.NewGuid():N}.log");

    try
    {
      this.logger.LogDebug("Executing elevated command {Command}", commandLine);

      if (this.executionOptions.IsVerbose)
      {
        this.userInteraction.WriteDebug($"[ELEVATED] Executing: {commandLine}");
      }

      string script = BuildElevatedPowerShellScript(fileName, arguments, tempFile);
      string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

      var startInfo = new ProcessStartInfo
      {
        FileName = "powershell.exe",
        Arguments = $"-NoLogo -NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
        Verb = "runas",
        UseShellExecute = true,
        WindowStyle = ProcessWindowStyle.Hidden,
      };

      using var process = new Process { StartInfo = startInfo };

      if (!process.Start())
      {
        this.logger.LogWarning("Failed to start elevated process for {Command}", commandLine);
        return new ProcessResult(-1, string.Empty, $"Failed to start elevated process '{fileName}'.");
      }

      await process.WaitForExitAsync().ConfigureAwait(false);

      string combinedOutput = string.Empty;

      if (File.Exists(tempFile))
      {
        combinedOutput = await File.ReadAllTextAsync(tempFile).ConfigureAwait(false);
      }

      this.logger.LogDebug(
        "Elevated command {Command} exited with code {ExitCode}. Output: {Output}",
        commandLine,
        process.ExitCode,
        TruncateForLog(combinedOutput)
      );

      if (this.executionOptions.IsVerbose)
      {
        this.userInteraction.WriteDebug($"[ELEVATED] Completed with exit code {process.ExitCode}");

        if (!string.IsNullOrWhiteSpace(combinedOutput))
        {
          this.userInteraction.WriteDebug($"output: {combinedOutput.TrimEnd()}");
        }
      }

      return new ProcessResult(process.ExitCode, combinedOutput, string.Empty);
    }
    catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
    {
      this.logger.LogWarning(ex, "Elevated command {Command} was canceled by the user.", commandLine);
      return new ProcessResult(1223, string.Empty, ex.Message);
    }
    catch (Exception ex)
    {
      this.logger.LogError(ex, "Unhandled exception while executing elevated command {Command}", commandLine);
      return new ProcessResult(-1, string.Empty, ex.Message);
    }
    finally
    {
      try
      {
        if (File.Exists(tempFile))
        {
          File.Delete(tempFile);
        }
      }
      catch (Exception cleanupEx)
      {
        this.logger.LogDebug(cleanupEx, "Failed to delete temporary output file {TempFile}", tempFile);
      }
    }
  }
}
