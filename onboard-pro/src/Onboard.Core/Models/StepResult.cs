namespace Onboard.Core.Models;

using System;

/// <summary>
/// Represents the execution status for an onboarding step.
/// </summary>
public enum StepStatus
{
  /// <summary>
  /// The step executed and performed work.
  /// </summary>
  Executed,

  /// <summary>
  /// The step was skipped because no work was required.
  /// </summary>
  Skipped,

  /// <summary>
  /// The step failed.
  /// </summary>
  Failed,
}

/// <summary>
/// Captures the outcome of an onboarding step for summary reporting.
/// </summary>
/// <param name="StepName">Human-friendly step name.</param>
/// <param name="Status">Execution status.</param>
/// <param name="SkipReason">Optional reason why the step was skipped.</param>
/// <param name="Exception">Optional exception if the step failed.</param>
public sealed record StepResult(
  string StepName,
  StepStatus Status,
  string? SkipReason = null,
  Exception? Exception = null
);
