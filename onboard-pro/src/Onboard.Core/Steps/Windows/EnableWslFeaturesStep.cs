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
    private const string WslFeatureName = "Microsoft-Windows-Subsystem-Linux";
    private const string VirtualMachinePlatformFeatureName = "VirtualMachinePlatform";
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

        var issues = new List<string>();

        this.userInteraction.WriteWarning("Manual WSL setup required");

        if (!readiness.IsWslOptionalFeatureEnabled)
        {
            this.userInteraction.WriteWarning("The 'Microsoft-Windows-Subsystem-Linux' optional feature is not enabled.");
            issues.Add("Microsoft-Windows-Subsystem-Linux feature is disabled");
        }

        if (!readiness.IsVirtualMachinePlatformEnabled)
        {
            this.userInteraction.WriteWarning("The 'VirtualMachinePlatform' optional feature is not enabled.");
            issues.Add("VirtualMachinePlatform feature is disabled");
        }

        if (!readiness.HasUbuntuDistribution)
        {
            this.userInteraction.WriteWarning($"{this.configuration.WslDistroName} is not installed in WSL.");
            issues.Add($"{this.configuration.WslDistroName} distribution is missing");
        }

        this.userInteraction.WriteNormal("Follow these steps in an administrator PowerShell window:");
        this.userInteraction.WriteNormal($"  1. Run: wsl --install -d {this.configuration.WslDistroImage}");
        this.userInteraction.WriteNormal("  2. Restart Windows if prompted to complete the installation.");
        this.userInteraction.WriteNormal($"  3. Launch {this.configuration.WslDistroName} once so the user account is created, then rerun this onboarding tool.");

        string issueSummary = issues.Count == 0 ? "WSL prerequisites are missing" : $"WSL prerequisites are missing: {string.Join(", ", issues)}";
        throw new InvalidOperationException($"{issueSummary}. Complete the manual steps above and rerun the onboarding tool.");
    }

    private async Task<WslReadiness> EvaluateReadinessAsync()
    {
        bool isWslFeatureEnabled = await IsFeatureEnabledAsync(WslFeatureName).ConfigureAwait(false);
        bool isVirtualMachinePlatformEnabled = await IsFeatureEnabledAsync(VirtualMachinePlatformFeatureName).ConfigureAwait(false);

        if (!isWslFeatureEnabled || !isVirtualMachinePlatformEnabled)
        {
            return WslReadiness.Create(isWslFeatureEnabled, isVirtualMachinePlatformEnabled, false);
        }

        var listResult = await processRunner.RunAsync("wsl.exe", WslListDistributionsCommand).ConfigureAwait(false);
        if (!listResult.IsSuccess)
        {
            return WslReadiness.Create(isWslFeatureEnabled, isVirtualMachinePlatformEnabled, false);
        }

        bool hasUbuntu = listResult.StandardOutput
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(line => string.Equals(line.Trim(), configuration.WslDistroName, StringComparison.OrdinalIgnoreCase));

        return WslReadiness.Create(isWslFeatureEnabled, isVirtualMachinePlatformEnabled, hasUbuntu);
    }

    private async Task<bool> IsFeatureEnabledAsync(string featureName)
    {
        string arguments = $"/online /Get-FeatureInfo /FeatureName:{featureName}";
        var result = await processRunner.RunAsync("dism.exe", arguments, requestElevation: true).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return false;
        }

        return result.StandardOutput
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(line => line.Contains("State", StringComparison.OrdinalIgnoreCase) && line.Contains("Enabled", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class WslReadiness
    {
        private WslReadiness(bool isWslOptionalFeatureEnabled, bool isVirtualMachinePlatformEnabled, bool hasUbuntuDistribution, bool isInitialized)
        {
            IsWslOptionalFeatureEnabled = isWslOptionalFeatureEnabled;
            IsVirtualMachinePlatformEnabled = isVirtualMachinePlatformEnabled;
            HasUbuntuDistribution = hasUbuntuDistribution;
            IsInitialized = isInitialized;
        }

        public static WslReadiness Uninitialized { get; } = new(false, false, false, false);

        public static WslReadiness Create(bool isWslOptionalFeatureEnabled, bool isVirtualMachinePlatformEnabled, bool hasUbuntuDistribution) =>
            new(isWslOptionalFeatureEnabled, isVirtualMachinePlatformEnabled, hasUbuntuDistribution, true);

        public bool FeaturesEnabled => this.IsWslOptionalFeatureEnabled && this.IsVirtualMachinePlatformEnabled;

        public bool IsWslOptionalFeatureEnabled { get; }

        public bool IsVirtualMachinePlatformEnabled { get; }

        public bool HasUbuntuDistribution { get; }

        public bool IsInitialized { get; }
    }
}
