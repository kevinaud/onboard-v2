// <copyright file="IPlatformOrchestrator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Abstractions;

/// <summary>
/// Interface for platform-specific orchestrators that manage the execution of onboarding steps.
/// </summary>
public interface IPlatformOrchestrator
{
  /// <summary>
  /// Executes the onboarding process for this platform.
  /// </summary>
  /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
  Task ExecuteAsync();
}
