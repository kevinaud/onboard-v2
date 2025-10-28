namespace Onboard.Core.Abstractions;

/// <summary>
/// Represents a UI status context that allows updating a spinner-style status display while work executes.
/// </summary>
public interface IStatusContext
{
    /// <summary>
    /// Updates the displayed status message.
    /// </summary>
    /// <param name="status">The status message to show.</param>
    void UpdateStatus(string status);

    /// <summary>
    /// Writes a standard message within the context.
    /// </summary>
    /// <param name="message">The message to display.</param>
    void WriteNormal(string message);

    /// <summary>
    /// Writes a success message within the context.
    /// </summary>
    /// <param name="message">The message to display.</param>
    void WriteSuccess(string message);

    /// <summary>
    /// Writes a warning message within the context.
    /// </summary>
    /// <param name="message">The message to display.</param>
    void WriteWarning(string message);

    /// <summary>
    /// Writes an error message within the context.
    /// </summary>
    /// <param name="message">The message to display.</param>
    void WriteError(string message);

    /// <summary>
    /// Writes a debug message within the context.
    /// </summary>
    /// <param name="message">The message to display.</param>
    void WriteDebug(string message);
}
