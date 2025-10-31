namespace Onboard.Core.Abstractions;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Refreshes the current process environment variables from persisted machine and user values.
/// </summary>
public interface IEnvironmentRefresher
{
    /// <summary>
    /// Reloads the current process environment variables from the machine and user scopes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}
