namespace Onboard.Core.Steps.Windows;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Verifies that WSL optional features are enabled and Ubuntu 22.04 is installed.
/// </summary>
public class EnableWslFeaturesStep : IOnboardingStep
{
    private const string WslListDistributionsCommand = "-l -q";

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;
    private readonly OnboardingConfiguration configuration;

    private WslReadiness readiness = WslReadiness.Uninitialized;

    public EnableWslFeaturesStep(
        IProcessRunner processRunner,
        IUserInteraction userInteraction,
        OnboardingConfiguration configuration)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
        this.configuration = configuration;
    }

    public string Description => "Verify Windows Subsystem for Linux prerequisites";

    public async Task<bool> ShouldExecuteAsync()
    {
        readiness = await EvaluateReadinessAsync().ConfigureAwait(false);
        return !readiness.CanQueryDistributions || !readiness.HasTargetDistribution;
    }

    public async Task ExecuteAsync()
    {
        if (!readiness.IsInitialized)
        {
            readiness = await EvaluateReadinessAsync().ConfigureAwait(false);
        }

        if (!readiness.CanQueryDistributions)
        {
            PromptForManualInstall();
            throw new InvalidOperationException("WSL could not enumerate distributions. Install the required distro and rerun the onboarding tool.");
        }

        if (readiness.HasTargetDistribution)
        {
            return;
        }

        HandleMissingDistribution(readiness.DetectedDistributions);
    }

    internal static IReadOnlyCollection<string> ParseDistributionNamesForTesting(string commandOutput) =>
        ParseDistributionNames(commandOutput);

    private static IReadOnlyCollection<string> ParseDistributionNames(string commandOutput)
    {
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in commandOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string? candidate = SanitizeDistributionName(rawLine);
            if (string.IsNullOrEmpty(candidate))
            {
                continue;
            }

            if (seen.Add(candidate))
            {
                names.Add(candidate);
            }
        }

        return names;
    }

    private static string? SanitizeDistributionName(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            return null;
        }

        string trimmed = rawLine.Trim().Trim('\ufeff');
        if (trimmed.Length == 0)
        {
            return null;
        }

        Span<char> buffer = stackalloc char[trimmed.Length];
        int index = 0;
        foreach (char character in trimmed)
        {
            if (!char.IsControl(character) || character == '\t' || character == '\n' || character == '\r')
            {
                buffer[index++] = character;
            }
        }

        if (index == 0)
        {
            return null;
        }

        string sanitized = new(buffer[..index]);
        sanitized = sanitized.Trim();
        return sanitized.Length == 0 ? null : sanitized;
    }

    private async Task<WslReadiness> EvaluateReadinessAsync()
    {
        var listResult = await processRunner.RunAsync("wsl.exe", WslListDistributionsCommand).ConfigureAwait(false);
        if (!listResult.IsSuccess)
        {
            this.configuration.ActiveWslDistroName = null;
            return WslReadiness.Create(false, false, Array.Empty<string>());
        }

        var distributionNames = ParseDistributionNames(listResult.StandardOutput);
        bool hasTarget = distributionNames.Any(name => string.Equals(name, this.configuration.WslDistroName, StringComparison.OrdinalIgnoreCase));

        this.configuration.ActiveWslDistroName = hasTarget ? this.configuration.WslDistroName : null;
        return WslReadiness.Create(true, hasTarget, distributionNames);
    }

    private void HandleMissingDistribution(IReadOnlyCollection<string> detectedDistributions)
    {
        string targetName = this.configuration.WslDistroName;

        this.userInteraction.WriteWarning($"WSL distribution '{targetName}' was not found.");

        if (detectedDistributions.Count == 0)
        {
            this.userInteraction.WriteWarning("No WSL distributions are currently registered.");
            PromptForManualInstall();
            throw new InvalidOperationException($"Install '{this.configuration.WslDistroImage}' and rerun the onboarding tool.");
        }

        this.userInteraction.WriteNormal("Detected WSL distributions:");
        foreach (string name in detectedDistributions)
        {
            this.userInteraction.WriteNormal($"  • {name}");
        }

        this.userInteraction.WriteNormal("Run this command to confirm the distro version:");
        this.userInteraction.WriteNormal("  wsl.exe -d <distro> sh -c \"grep -w 'VERSION_ID' /etc/os-release\"");

        string? selection = PromptForDistributionSelection(detectedDistributions);

        if (selection is null)
        {
            PromptForManualInstall();
            throw new InvalidOperationException($"Install '{this.configuration.WslDistroImage}' and rerun the onboarding tool.");
        }

        this.userInteraction.WriteNormal($"You selected '{selection}' as your Ubuntu 22.04 environment.");
        this.userInteraction.WriteNormal($"Rename it so future runs detect it: wsl.exe --rename \"{selection}\" \"{targetName}\"");
        this.userInteraction.WriteNormal($"Alternatively install the official image: wsl.exe --install -d {this.configuration.WslDistroImage}");

        throw new InvalidOperationException($"Rename the selected distribution to '{targetName}' (or install it) and rerun the onboarding tool.");
    }

    private void PromptForManualInstall()
    {
        this.userInteraction.WriteWarning("Manual WSL setup required");
        this.userInteraction.WriteNormal("Follow these steps in an administrator PowerShell window:");
        this.userInteraction.WriteNormal($"  1. Run: wsl --install -d {this.configuration.WslDistroImage}");
        this.userInteraction.WriteNormal("  2. Restart Windows if prompted to complete the installation.");
        this.userInteraction.WriteNormal($"  3. Launch {this.configuration.WslDistroName} once so the user account is created, then rerun this onboarding tool.");
    }

    private string? PromptForDistributionSelection(IReadOnlyCollection<string> detectedDistributions)
    {
        if (detectedDistributions.Count == 0)
        {
            return null;
        }

        while (true)
        {
            string response = this.userInteraction.Ask("Enter the name of the Ubuntu 22.04 distribution from the list above (or type NONE):", "NONE");
            if (string.IsNullOrWhiteSpace(response) || string.Equals(response, "NONE", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string trimmed = response.Trim();
            string? match = detectedDistributions.FirstOrDefault(name => string.Equals(name, trimmed, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match;
            }

            this.userInteraction.WriteWarning($"'{response}' did not match any detected distribution. Try again or type NONE.");
        }
    }

    private sealed class WslReadiness
    {
        private WslReadiness(bool canQueryDistributions, bool hasTargetDistribution, IReadOnlyCollection<string> detectedDistributions, bool isInitialized)
        {
            CanQueryDistributions = canQueryDistributions;
            HasTargetDistribution = hasTargetDistribution;
            DetectedDistributions = detectedDistributions;
            IsInitialized = isInitialized;
        }

        public static WslReadiness Uninitialized { get; } = new(false, false, Array.Empty<string>(), false);

        public static WslReadiness Create(bool canQueryDistributions, bool hasTargetDistribution, IReadOnlyCollection<string> detectedDistributions) =>
            new(canQueryDistributions, hasTargetDistribution, detectedDistributions, true);

        public bool CanQueryDistributions { get; }

        public bool HasTargetDistribution { get; }

        public IReadOnlyCollection<string> DetectedDistributions { get; }

        public bool IsInitialized { get; }
    }
}
