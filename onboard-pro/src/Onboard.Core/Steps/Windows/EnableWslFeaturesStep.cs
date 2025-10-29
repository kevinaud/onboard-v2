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
    private const string WslListDistributionsCommand = "-l -v";
    private const string OsReleaseCommand = "cat /etc/os-release";

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

    private static string BuildOsReleaseArguments(string distributionName)
    {
        string formattedName = distributionName.Any(char.IsWhiteSpace) ? $"\"{distributionName}\"" : distributionName;
        return $"-d {formattedName} -- {OsReleaseCommand}";
    }

    private static IReadOnlyCollection<string> ParseDistributionNames(string commandOutput)
    {
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in commandOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string? candidateName = TryExtractDistributionName(rawLine);
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

    private static string? TryExtractDistributionName(string rawLine)
    {
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            return null;
        }

        string line = rawLine.Trim();
        if (line.Length == 0)
        {
            return null;
        }

        line = line.TrimStart('\ufeff');

        if (line.StartsWith("Windows Subsystem", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("The following", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("Distributions", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (line[0] == '*')
        {
            line = line[1..].TrimStart();
        }

        line = line.TrimStart();
        if (line.Length == 0)
        {
            return null;
        }

        if (line.StartsWith("NAME", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        int columnBreak = FindColumnBreak(line);
        string candidateName = columnBreak >= 0 ? line[..columnBreak] : line;
        candidateName = candidateName.Trim().Trim('\ufeff');

        if (candidateName.EndsWith("(Default)", StringComparison.OrdinalIgnoreCase))
        {
            candidateName = candidateName[..^"(Default)".Length].TrimEnd();
        }

        if (string.IsNullOrWhiteSpace(candidateName) || string.Equals(candidateName, "*", StringComparison.Ordinal))
        {
            return null;
        }

        return candidateName;
    }

    private static int FindColumnBreak(string line)
    {
        for (int index = 0; index < line.Length - 1; index++)
        {
            char current = line[index];
            char next = line[index + 1];

            if (current == '\t' || (current == ' ' && next == ' '))
            {
                return index;
            }
        }

        return -1;
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
            var distroResult = await processRunner.RunAsync("wsl.exe", BuildOsReleaseArguments(distroName)).ConfigureAwait(false);
            if (!distroResult.IsSuccess && string.IsNullOrWhiteSpace(distroResult.StandardOutput) && string.IsNullOrWhiteSpace(distroResult.StandardError))
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
