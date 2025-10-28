namespace Onboard.Core.Steps.Windows;

using System;
using System.Linq;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Verifies that WSL optional features are enabled and Ubuntu 22.04 is installed.
/// </summary>
public class EnableWslFeaturesStep : IOnboardingStep
{
    private const string WslStatusCommand = "--status";
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
        return !readiness.FeaturesEnabled || !readiness.HasUbuntuDistribution;
    }

    public async Task ExecuteAsync()
    {
        if (!readiness.IsInitialized)
        {
            readiness = await EvaluateReadinessAsync().ConfigureAwait(false);
        }

        if (readiness.FeaturesEnabled && readiness.HasUbuntuDistribution)
        {
            return;
        }

        userInteraction.WriteHeader("Manual WSL setup required");

        if (!readiness.FeaturesEnabled)
        {
            userInteraction.WriteWarning("Windows Subsystem for Linux optional features are not enabled.");
        }

        if (!readiness.HasUbuntuDistribution)
        {
            userInteraction.WriteWarning($"{configuration.WslDistroName} is not installed in WSL.");
        }

        userInteraction.WriteLine("Follow these steps in an administrator PowerShell window:");
        userInteraction.WriteLine($"  1. Run: wsl --install -d {configuration.WslDistroImage}");
        userInteraction.WriteLine("  2. Restart Windows if prompted to complete the installation.");
        userInteraction.WriteLine($"  3. Launch {configuration.WslDistroName} once so the user account is created, then rerun this onboarding tool.");
    }

    private async Task<WslReadiness> EvaluateReadinessAsync()
    {
        var status = await processRunner.RunAsync("wsl.exe", WslStatusCommand).ConfigureAwait(false);
        if (!status.IsSuccess)
        {
            return WslReadiness.Create(false, false);
        }

        var listResult = await processRunner.RunAsync("wsl.exe", WslListDistributionsCommand).ConfigureAwait(false);
        if (!listResult.IsSuccess)
        {
            return WslReadiness.Create(true, false);
        }

        bool hasUbuntu = listResult.StandardOutput
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(line => string.Equals(line.Trim(), configuration.WslDistroName, StringComparison.OrdinalIgnoreCase));

        return WslReadiness.Create(true, hasUbuntu);
    }

    private sealed class WslReadiness
    {
        private WslReadiness(bool featuresEnabled, bool hasUbuntuDistribution, bool isInitialized)
        {
            FeaturesEnabled = featuresEnabled;
            HasUbuntuDistribution = hasUbuntuDistribution;
            IsInitialized = isInitialized;
        }

        public static WslReadiness Uninitialized { get; } = new(false, false, false);

        public static WslReadiness Create(bool featuresEnabled, bool hasUbuntuDistribution) =>
            new(featuresEnabled, hasUbuntuDistribution, true);

        public bool FeaturesEnabled { get; }

        public bool HasUbuntuDistribution { get; }

        public bool IsInitialized { get; }
    }
}
