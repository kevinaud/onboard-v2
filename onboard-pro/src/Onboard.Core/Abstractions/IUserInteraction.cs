// <copyright file="IUserInteraction.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Abstractions;

/// <summary>
/// An abstraction for all console I/O.
/// </summary>
public interface IUserInteraction
{
    /// <summary>
    /// Writes a standard message to the console.
    /// </summary>
    void WriteLine(string message);

    /// <summary>
    /// Writes a header/title message to the console (typically colored/styled).
    /// </summary>
    void WriteHeader(string message);

    /// <summary>
    /// Writes a success message to the console (typically green).
    /// </summary>
    void WriteSuccess(string message);

    /// <summary>
    /// Writes a warning message to the console (typically yellow).
    /// </summary>
    void WriteWarning(string message);

    /// <summary>
    /// Writes an error message to the console (typically red).
    /// </summary>
    void WriteError(string message);

    /// <summary>
    /// Prompts the user for input and returns their response.
    /// </summary>
    /// <param name="message">The prompt message to display.</param>
    /// <returns>The user's input string.</returns>
    string Prompt(string message);
}
