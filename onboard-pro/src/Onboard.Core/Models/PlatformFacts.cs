// <copyright file="PlatformFacts.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Models;

/// <summary>
/// Enumeration of supported operating systems.
/// </summary>
public enum OperatingSystem
{
    Windows,
    MacOs,
    Linux,
    Unknown,
}

/// <summary>
/// Enumeration of supported CPU architectures.
/// </summary>
public enum Architecture
{
    X64,
    Arm64,
    Unknown,
}

/// <summary>
/// An immutable record holding all facts about the current environment.
/// This will be registered as a singleton in the DI container.
/// </summary>
public record PlatformFacts(
    OperatingSystem OS,
    Architecture Arch,
    bool IsWsl,
    string HomeDirectory);
