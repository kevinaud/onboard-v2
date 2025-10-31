// <copyright file="IProcessRunner.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Abstractions;

using Onboard.Core.Models;

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

  /// <summary>
  /// Runs an external process with optional elevation.
  /// </summary>
  /// <param name="fileName">The executable or command to run.</param>
  /// <param name="arguments">The command-line arguments.</param>
  /// <param name="requestElevation">Whether to attempt elevation (Windows only).</param>
  /// <returns>A ProcessResult containing exit code and output.</returns>
  Task<ProcessResult> RunAsync(string fileName, string arguments, bool requestElevation);

  /// <summary>
  /// Runs an external process with optional elevation and shell execution.
  /// </summary>
  /// <param name="fileName">The executable or command to run.</param>
  /// <param name="arguments">The command-line arguments.</param>
  /// <param name="requestElevation">Whether to attempt elevation (Windows only).</param>
  /// <param name="useShellExecute">Whether to launch via the shell instead of directly (Windows only).</param>
  /// <returns>A ProcessResult containing exit code and output.</returns>
  Task<ProcessResult> RunAsync(string fileName, string arguments, bool requestElevation, bool useShellExecute);
}
