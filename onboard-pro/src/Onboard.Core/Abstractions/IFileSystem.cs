// <copyright file="IFileSystem.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Abstractions;

/// <summary>
/// Abstraction over file system operations required by onboarding steps.
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Determines whether the specified directory exists.
    /// </summary>
    /// <param name="path">Path to evaluate.</param>
    /// <returns><c>true</c> when the directory exists; otherwise <c>false</c>.</returns>
    bool DirectoryExists(string path);

    /// <summary>
    /// Ensures a directory exists, creating it (and any necessary parents) when missing.
    /// </summary>
    /// <param name="path">Path to create.</param>
    void CreateDirectory(string path);
}
