// <copyright file="IUserInteraction.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Abstractions;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Onboard.Core.Models;

/// <summary>
/// Abstraction for all user-facing interactions.
/// </summary>
public interface IUserInteraction
{
    /// <summary>
    /// Writes a standard message to the console.
    /// </summary>
    void WriteNormal(string message);

    /// <summary>
    /// Writes formatted markdown content to the console.
    /// </summary>
    /// <param name="markdown">Markdown payload to render.</param>
    void WriteMarkdown(string markdown);

    /// <summary>
    /// Writes a success message to the console (typically styled green).
    /// </summary>
    void WriteSuccess(string message);

    /// <summary>
    /// Writes a warning message to the console (typically styled yellow).
    /// </summary>
    void WriteWarning(string message);

    /// <summary>
    /// Writes an error message to the console (typically styled red).
    /// </summary>
    void WriteError(string message);

    /// <summary>
    /// Writes a verbose/debug message to the console when verbose mode is enabled.
    /// </summary>
    void WriteDebug(string message);

    /// <summary>
    /// Displays the welcome banner for the onboarding session.
    /// </summary>
    /// <param name="platformFacts">Facts about the detected platform.</param>
    void ShowWelcomeBanner(PlatformFacts platformFacts);

    /// <summary>
    /// Presents a completion summary for executed steps.
    /// </summary>
    /// <param name="results">The set of step results to display.</param>
    void ShowSummary(IReadOnlyCollection<StepResult> results);

    /// <summary>
    /// Executes work while displaying a status spinner.
    /// </summary>
    /// <param name="statusMessage">Initial status message.</param>
    /// <param name="action">The action to execute within the status context.</param>
    /// <param name="cancellationToken">Cancellation token for the work.</param>
    /// <returns>A task that completes when the action has finished.</returns>
    Task RunStatusAsync(string statusMessage, Func<IStatusContext, Task> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prompts the user for input and returns their response.
    /// </summary>
    /// <param name="prompt">Prompt text to display.</param>
    /// <param name="defaultValue">Optional default value returned when the user provides no input.</param>
    /// <returns>The user's response.</returns>
    string Ask(string prompt, string? defaultValue = null);

    /// <summary>
    /// Prompts the user for a yes/no confirmation.
    /// </summary>
    /// <param name="prompt">Prompt text to display.</param>
    /// <param name="defaultValue">Default confirmation value when the user provides no input.</param>
    /// <returns><see langword="true"/> when the user confirms; otherwise <see langword="false"/>.</returns>
    bool Confirm(string prompt, bool defaultValue = false);
}
