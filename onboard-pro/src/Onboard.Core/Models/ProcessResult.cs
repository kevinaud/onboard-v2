// <copyright file="ProcessResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Models;

/// <summary>
/// Represents the result of running an external process.
/// </summary>
public record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
  /// <summary>
  /// Gets a value indicating whether returns true if the process completed successfully (exit code 0).
  /// </summary>
  public bool IsSuccess => this.ExitCode == 0;
}
