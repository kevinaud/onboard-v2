namespace Onboard.Core.Models;

/// <summary>
/// Provides central configuration values for onboarding steps.
/// </summary>
public record OnboardingConfiguration
{
    /// <summary>
    /// Gets the default WSL distribution name to check/install.
    /// </summary>
    public string WslDistroName { get; init; } = "Ubuntu-22.04";

    /// <summary>
    /// Gets the default WSL distribution image to install when provisioning a distro.
    /// </summary>
    public string WslDistroImage { get; init; } = "Ubuntu-22.04";

    /// <summary>
    /// Gets the expected Git Credential Manager executable path on Windows.
    /// </summary>
    public string GitCredentialManagerPath { get; init; } = @"C:\\Program Files\\Git\\mingw64\\bin\\git-credential-manager.exe";

    /// <summary>
    /// Gets or sets the active WSL distribution name detected during onboarding.
    /// </summary>
    public string? ActiveWslDistroName { get; set; }
}
