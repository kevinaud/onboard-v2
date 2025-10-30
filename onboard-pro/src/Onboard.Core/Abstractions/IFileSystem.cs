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

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">Path to evaluate.</param>
    /// <returns><c>true</c> when the file exists; otherwise <c>false</c>.</returns>
    bool FileExists(string path);

    /// <summary>
    /// Reads the entire contents of a text file.
    /// </summary>
    /// <param name="path">Path to read.</param>
    /// <returns>File contents as a string.</returns>
    string ReadAllText(string path);

    /// <summary>
    /// Writes the provided text to a file, overwriting any existing content.
    /// </summary>
    /// <param name="path">Destination path.</param>
    /// <param name="contents">Text to write.</param>
    void WriteAllText(string path, string contents);

    /// <summary>
    /// Moves or renames a file to a new location, optionally overwriting the destination.
    /// </summary>
    /// <param name="sourcePath">Path of the file to move.</param>
    /// <param name="destinationPath">Destination path for the file.</param>
    /// <param name="overwrite">Whether to overwrite the destination file when it exists.</param>
    void MoveFile(string sourcePath, string destinationPath, bool overwrite);

    /// <summary>
    /// Deletes the specified file when it exists.
    /// </summary>
    /// <param name="path">File path to delete.</param>
    void DeleteFile(string path);
}
