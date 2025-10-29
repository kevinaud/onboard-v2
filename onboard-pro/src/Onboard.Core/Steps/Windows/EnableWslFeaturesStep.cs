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
    private readonly string? expectedUbuntuVersionId;

    private WslReadiness readiness = WslReadiness.Uninitialized;

    public EnableWslFeaturesStep(
        IProcessRunner processRunner,
        IUserInteraction userInteraction,
        OnboardingConfiguration configuration)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
        this.configuration = configuration;
        this.expectedUbuntuVersionId = ExtractVersionFromImage(configuration.WslDistroImage);
    }

    public string Description => "Verify Windows Subsystem for Linux prerequisites";

    public async Task<bool> ShouldExecuteAsync()
    {
        readiness = await EvaluateReadinessAsync().ConfigureAwait(false);
        return !readiness.CanQueryDistributions || !readiness.HasUbuntuDistribution;
    }

    public async Task ExecuteAsync()
    {
        if (!readiness.IsInitialized)
        {
            readiness = await EvaluateReadinessAsync().ConfigureAwait(false);
        }

        if (readiness.CanQueryDistributions && readiness.HasUbuntuDistribution)
        {
            return;
        }

        var issues = new List<string>();

        this.userInteraction.WriteWarning("Manual WSL setup required");

        if (!readiness.CanQueryDistributions)
        {
            this.userInteraction.WriteWarning("WSL command not available. Unable to list installed distributions.");
            issues.Add("WSL command unavailable");
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

    /// <summary>
    /// Exposed for testing purposes only.
    /// </summary>
    /// <param name="commandOutput">The raw output from wsl.exe -l -q.</param>
    /// <returns>Collection of distribution names extracted from the output.</returns>
    internal static IReadOnlyCollection<string> ParseDistributionNamesForTesting(string commandOutput) =>
        ParseDistributionNames(commandOutput);

    private static IReadOnlyCollection<string> ParseDistributionNames(string commandOutput)
    {
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in commandOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string? candidateName = SanitizeDistributionName(rawLine);

            if (string.IsNullOrEmpty(candidateName))
            {
                continue;
            }

            if (seen.Add(candidateName))
            {
                names.Add(candidateName);
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

    private static string CombineOutputs(string standardOutput, string standardError)
    {
        if (string.IsNullOrWhiteSpace(standardError))
        {
            return standardOutput;
        }

        if (string.IsNullOrWhiteSpace(standardOutput))
        {
            return standardError;
        }

        return standardOutput + Environment.NewLine + standardError;
    }

    private static string? ExtractOsReleaseValue(IEnumerable<string> lines, string key)
    {
        string prefix = key + "=";
        foreach (string line in lines)
        {
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string value = line[prefix.Length..].Trim().Trim('"');
                return value;
            }
        }

        return null;
    }

    private static string? ExtractVersionFromImage(string distroImage)
    {
        if (string.IsNullOrWhiteSpace(distroImage))
        {
            return null;
        }

        int lastDash = distroImage.LastIndexOf('-');
        if (lastDash < 0 || lastDash == distroImage.Length - 1)
        {
            return null;
        }

        return distroImage[(lastDash + 1)..];
    }

    private static string DescribeForDebug(string value)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append(value);
        builder.Append(" (length=");
        builder.Append(value.Length);
        builder.Append(", codepoints=[");

        for (int i = 0; i < value.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append("U+");
            builder.Append(((int)value[i]).ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
        }

        builder.Append("])");
        return builder.ToString();
    }

    private string BuildOsReleaseArguments(string distributionName)
    {
        string trimmedName = distributionName.Trim();
        string script = BuildValidationScript();
        string escapedScript = script.Replace("\"", "\\\"", StringComparison.Ordinal);

        if (trimmedName.Length == 0)
        {
            return $"-d  -- sh -c \"{escapedScript}\"";
        }

        string escapedName = trimmedName.Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"-d \"{escapedName}\" -- sh -c \"{escapedScript}\"";
    }

    private string BuildValidationScript()
    {
        var builder = new System.Text.StringBuilder("grep -qx 'ID=ubuntu' /etc/os-release");

        if (!string.IsNullOrEmpty(expectedUbuntuVersionId))
        {
            builder.Append(" && grep -qx 'VERSION_ID=\"");
            builder.Append(expectedUbuntuVersionId);
            builder.Append("\"' /etc/os-release");
        }

        return builder.ToString();
    }

    private async Task<WslReadiness> EvaluateReadinessAsync()
    {
        var listResult = await processRunner.RunAsync("wsl.exe", WslListDistributionsCommand).ConfigureAwait(false);
        if (!listResult.IsSuccess)
        {
            return WslReadiness.Create(false, false, null);
        }

        var distributionNames = ParseDistributionNames(listResult.StandardOutput);
        foreach (string distroName in distributionNames)
        {
            if (string.IsNullOrWhiteSpace(distroName))
            {
                continue;
            }

            userInteraction.WriteDebug($"Inspecting WSL distribution candidate '{DescribeForDebug(distroName)}'");

            var distroResult = await processRunner.RunAsync("wsl.exe", BuildOsReleaseArguments(distroName), requestElevation: false, useShellExecute: true).ConfigureAwait(false);

            if (distroResult.IsSuccess)
            {
                return WslReadiness.Create(true, true, distroName);
            }

            if (string.IsNullOrWhiteSpace(distroResult.StandardOutput) && string.IsNullOrWhiteSpace(distroResult.StandardError))
            {
                continue;
            }

            string combinedOutput = CombineOutputs(distroResult.StandardOutput, distroResult.StandardError);
            if (IsTargetUbuntuDistribution(combinedOutput))
            {
                return WslReadiness.Create(true, true, distroName);
            }
        }

        return WslReadiness.Create(true, false, null);
    }

    private bool IsTargetUbuntuDistribution(string osReleaseOutput)
    {
        if (string.IsNullOrWhiteSpace(osReleaseOutput))
        {
            return false;
        }

        var lines = osReleaseOutput
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim());

        string? idValue = ExtractOsReleaseValue(lines, "ID");
        if (!string.Equals(idValue, "ubuntu", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string? versionId = ExtractOsReleaseValue(lines, "VERSION_ID");
        if (!string.IsNullOrEmpty(expectedUbuntuVersionId) && !string.Equals(versionId, expectedUbuntuVersionId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private sealed class WslReadiness
    {
        private WslReadiness(bool canQueryDistributions, bool hasUbuntuDistribution, string? matchingDistributionName, bool isInitialized)
        {
            CanQueryDistributions = canQueryDistributions;
            HasUbuntuDistribution = hasUbuntuDistribution;
            MatchingDistributionName = matchingDistributionName;
            IsInitialized = isInitialized;
        }

        public static WslReadiness Uninitialized { get; } = new(false, false, null, false);

        public static WslReadiness Create(bool canQueryDistributions, bool hasUbuntuDistribution, string? matchingDistributionName) =>
            new(canQueryDistributions, hasUbuntuDistribution, matchingDistributionName, true);

        public bool CanQueryDistributions { get; }

        public bool HasUbuntuDistribution { get; }

        public string? MatchingDistributionName { get; }

        public bool IsInitialized { get; }
    }
}
