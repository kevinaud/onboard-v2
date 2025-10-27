// <copyright file="IPlatformDetector.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Abstractions;

using Onboard.Core.Models;

/// <summary>
/// An abstraction for detecting platform information.
/// </summary>
public interface IPlatformDetector
{
    /// <summary>
    /// Detects and returns the current platform facts.
    /// </summary>
    /// <returns>An immutable PlatformFacts record containing OS, architecture, and environment details.</returns>
    PlatformFacts Detect();
}
