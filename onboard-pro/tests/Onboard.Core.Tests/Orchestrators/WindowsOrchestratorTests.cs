namespace Onboard.Core.Tests.Orchestrators;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using global::Onboard.Console.Orchestrators;
using global::Onboard.Core.Abstractions;
using global::Onboard.Core.Models;
using global::Onboard.Core.Steps.Shared;
using global::Onboard.Core.Steps.Windows;

using Moq;

using NUnit.Framework;

[TestFixture]
public class WindowsOrchestratorTests
{
    private const string DotfilesSettingsPath = "C:/Users/Test/AppData/Roaming/Code/User/settings.json";
    private const string DockerAppDataPath = "C:/AppData";
    private const string DockerSettingsPath = "C:/AppData/Docker/settings-store.json";

    [Test]
    public async Task ExecuteAsync_SkipsStepsWhenAlreadyConfigured()
    {
        var capturedSummary = new List<StepResult>();
        var ui = CreateUserInteractionMock(results =>
        {
            capturedSummary.Clear();
            capturedSummary.AddRange(results);
        });

        var configuration = new OnboardingConfiguration();
        var processRunner = CreateProcessRunner(new Dictionary<(string FileName, string Arguments, bool RequestElevation, bool UseShellExecute), ProcessResult>
        {
            { (FileName: "dism.exe", Arguments: "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", RequestElevation: false, UseShellExecute: false), Success("State : Enabled") },
            { (FileName: "dism.exe", Arguments: "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", RequestElevation: false, UseShellExecute: false), Success("State : Enabled") },
            { (FileName: "wsl.exe", Arguments: "-l -q", RequestElevation: false, UseShellExecute: false), Success("Ubuntu-22.04\r\n") },
            { (FileName: "wsl.exe", Arguments: "-d \"Ubuntu-22.04\" -- cat /etc/os-release", RequestElevation: false, UseShellExecute: true), Success(string.Empty) },
            { (FileName: "where", Arguments: "git.exe", RequestElevation: false, UseShellExecute: false), Success("C\\Git\\git.exe") },
            { (FileName: "where", Arguments: "gh.exe", RequestElevation: false, UseShellExecute: false), Success("C:\\GitHubCli\\gh.exe") },
            { (FileName: "where", Arguments: "code.cmd", RequestElevation: false, UseShellExecute: false), Success("C:\\VSCode\\code.cmd") },
            { (FileName: "powershell", Arguments: "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\"", RequestElevation: false, UseShellExecute: false), Success("True") },
            { (FileName: "cmd.exe", Arguments: "/c C:\\VSCode\\code.cmd --list-extensions", RequestElevation: false, UseShellExecute: false), Success("ms-vscode-remote.vscode-remote-extensionpack") },
            { (FileName: "cmd.exe", Arguments: BuildCredentialProbeArguments(configuration), RequestElevation: false, UseShellExecute: false), Success("username=user\r\npassword=secret") },
            { (FileName: "git", Arguments: "config --global user.name", RequestElevation: false, UseShellExecute: false), Success("Test User") },
            { (FileName: "git", Arguments: "config --global user.email", RequestElevation: false, UseShellExecute: false), Success("test@example.com") },
        });

        var fileSystem = new FakeFileSystem(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { DotfilesSettingsPath, "{\"dotfiles.repository\":\"someone/dots\"}" },
            { DockerSettingsPath, "{\"IntegratedWslDistros\":[\"Ubuntu-22.04\"]}" },
        });

        var orchestrator = CreateOrchestrator(
            ui,
            processRunner,
            fileSystem,
            configuration,
            new ExecutionOptions(IsDryRun: false, IsVerbose: false));

        await orchestrator.ExecuteAsync().ConfigureAwait(false);

        ui.Verify(x => x.WriteNormal("Starting Windows host onboarding..."), Times.Once);
        ui.Verify(x => x.RunStatusAsync(It.IsAny<string>(), It.IsAny<Func<IStatusContext, Task>>(), It.IsAny<CancellationToken>()), Times.Exactly(10));
        ui.Verify(x => x.ShowSummary(It.IsAny<IReadOnlyCollection<StepResult>>()), Times.Once);
        ui.Verify(x => x.WriteSuccess("Windows host onboarding complete."), Times.Once);

        Assert.That(capturedSummary, Has.Count.EqualTo(10));
        Assert.That(capturedSummary.All(result => result.Status == StepStatus.Skipped));
        Assert.That(capturedSummary.All(result => string.Equals(result.SkipReason, "Already configured", StringComparison.Ordinal)));
        Assert.That(capturedSummary.Select(result => result.StepName).ToArray(), Is.EqualTo(new[]
        {
            "Verify Windows Subsystem for Linux prerequisites",
            "Install Git for Windows",
            "Install GitHub CLI",
            "Install Visual Studio Code",
            "Install VS Code Remote Development extension pack",
            "Configure VS Code dotfiles repository",
            "Install Docker Desktop",
            "Configure Docker Desktop WSL integration",
            "Authenticate Git Credential Manager with GitHub",
            "Configure Git user identity",
        }));
    }

    [Test]
    public void ExecuteAsync_WhenStepFails_ThrowsOnboardingStepException()
    {
        var capturedSummary = new List<StepResult>();
        var ui = CreateUserInteractionMock(results =>
        {
            capturedSummary.Clear();
            capturedSummary.AddRange(results);
        });

        var configuration = new OnboardingConfiguration();
        var processRunner = CreateProcessRunner(new Dictionary<(string FileName, string Arguments, bool RequestElevation, bool UseShellExecute), ProcessResult>
        {
            { (FileName: "dism.exe", Arguments: "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", RequestElevation: false, UseShellExecute: false), Success("State : Enabled") },
            { (FileName: "dism.exe", Arguments: "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", RequestElevation: false, UseShellExecute: false), Success("State : Enabled") },
            { (FileName: "wsl.exe", Arguments: "-l -q", RequestElevation: false, UseShellExecute: false), Success("Ubuntu-22.04\r\n") },
            { (FileName: "wsl.exe", Arguments: "-d \"Ubuntu-22.04\" -- cat /etc/os-release", RequestElevation: false, UseShellExecute: true), Success(string.Empty) },
            { (FileName: "where", Arguments: "git.exe", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, "not found") },
            { (FileName: "where", Arguments: "code.cmd", RequestElevation: false, UseShellExecute: false), Success("C:\\VSCode\\code.cmd") },
            { (FileName: "powershell", Arguments: "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\"", RequestElevation: false, UseShellExecute: false), Success("True") },
            { (FileName: "cmd.exe", Arguments: "/c C:\\VSCode\\code.cmd --list-extensions", RequestElevation: false, UseShellExecute: false), Success("ms-vscode-remote.vscode-remote-extensionpack") },
            { (FileName: "cmd.exe", Arguments: BuildCredentialProbeArguments(configuration), RequestElevation: false, UseShellExecute: false), Success("username=user\r\npassword=secret") },
        });

        var fileSystem = new FakeFileSystem(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { DotfilesSettingsPath, "{\"dotfiles.repository\":\"someone/dots\"}" },
            { DockerSettingsPath, "{\"IntegratedWslDistros\":[\"Ubuntu-22.04\"]}" },
        });

        var orchestrator = CreateOrchestrator(
            ui,
            processRunner,
            fileSystem,
            configuration,
            new ExecutionOptions(IsDryRun: false, IsVerbose: false));

        var exception = Assert.ThrowsAsync<OnboardingStepException>(() => orchestrator.ExecuteAsync())!;

        Assert.That(exception.Message, Does.Contain("Install Git for Windows"));
        ui.Verify(x => x.ShowSummary(It.IsAny<IReadOnlyCollection<StepResult>>()), Times.Once);
        ui.Verify(x => x.WriteSuccess("Windows host onboarding complete."), Times.Never);

        Assert.That(capturedSummary, Has.Count.EqualTo(2));
        Assert.That(capturedSummary[0].StepName, Is.EqualTo("Verify Windows Subsystem for Linux prerequisites"));
        Assert.That(capturedSummary[0].Status, Is.EqualTo(StepStatus.Skipped));
        Assert.That(capturedSummary[1].StepName, Is.EqualTo("Install Git for Windows"));
        Assert.That(capturedSummary[1].Status, Is.EqualTo(StepStatus.Failed));
        Assert.That(capturedSummary[1].Exception, Is.Not.Null);
    }

    [Test]
    public async Task ExecuteAsync_WithDryRun_OnlyReportsSteps()
    {
        var capturedSummary = new List<StepResult>();
        var ui = CreateUserInteractionMock(results =>
        {
            capturedSummary.Clear();
            capturedSummary.AddRange(results);
        });

        var configuration = new OnboardingConfiguration();
        var processRunner = CreateProcessRunner(new Dictionary<(string FileName, string Arguments, bool RequestElevation, bool UseShellExecute), ProcessResult>
        {
            { (FileName: "dism.exe", Arguments: "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, "WSL not enabled") },
            { (FileName: "dism.exe", Arguments: "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, "WSL not enabled") },
            { (FileName: "wsl.exe", Arguments: "-l -q", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, "WSL not installed") },
            { (FileName: "where", Arguments: "git.exe", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, "git not found") },
            { (FileName: "where", Arguments: "code.cmd", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, "code not found") },
            { (FileName: "where", Arguments: "gh.exe", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, "gh not found") },
            { (FileName: "powershell", Arguments: "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\"", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, "Docker not installed") },
            { (FileName: "cmd.exe", Arguments: "/c code --list-extensions", RequestElevation: false, UseShellExecute: false), new ProcessResult(0, string.Empty, string.Empty) },
            { (FileName: "cmd.exe", Arguments: BuildCredentialProbeArguments(configuration), RequestElevation: false, UseShellExecute: false), new ProcessResult(0, string.Empty, string.Empty) },
            { (FileName: "git", Arguments: "config --global user.name", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, string.Empty) },
            { (FileName: "git", Arguments: "config --global user.email", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, string.Empty) },
        });

        var fileSystem = new FakeFileSystem(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        var orchestrator = CreateOrchestrator(
            ui,
            processRunner,
            fileSystem,
            configuration,
            new ExecutionOptions(IsDryRun: true, IsVerbose: false));

        await orchestrator.ExecuteAsync().ConfigureAwait(false);

        ui.Verify(x => x.WriteNormal("Starting Windows host onboarding..."), Times.Once);
        ui.Verify(x => x.WriteSuccess("Windows host onboarding dry run complete."), Times.Once);
        ui.Verify(x => x.RunStatusAsync(It.IsAny<string>(), It.IsAny<Func<IStatusContext, Task>>(), It.IsAny<CancellationToken>()), Times.Exactly(10));

        Assert.That(capturedSummary, Has.Count.EqualTo(10));
        Assert.That(capturedSummary.All(result => result.Status == StepStatus.Skipped));
        Assert.That(capturedSummary.All(result => string.Equals(result.SkipReason, "Dry run", StringComparison.Ordinal)));
    }

    [Test]
    public async Task ExecuteAsync_WhenInteractiveStepRuns_PromptsOutsideStatus()
    {
        var capturedSummary = new List<StepResult>();
        bool isInsideStatus = false;

        var ui = new Mock<IUserInteraction>(MockBehavior.Loose);

        ui.Setup(x => x.RunStatusAsync(It.IsAny<string>(), It.IsAny<Func<IStatusContext, Task>>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, Func<IStatusContext, Task> action, CancellationToken token) =>
            {
                isInsideStatus = true;

                try
                {
                    await action(new TestStatusContext(ui.Object, token)).ConfigureAwait(false);
                }
                finally
                {
                    isInsideStatus = false;
                }
            });

        ui.Setup(x => x.ShowSummary(It.IsAny<IReadOnlyCollection<StepResult>>()))
            .Callback((IReadOnlyCollection<StepResult> results) =>
            {
                capturedSummary.Clear();
                capturedSummary.AddRange(results);
            });

        ui.Setup(x => x.Ask(It.IsAny<string>(), It.IsAny<string?>()))
            .Returns((string prompt, string? _) =>
            {
                Assert.That(isInsideStatus, Is.False);
                return prompt.Contains("email", StringComparison.OrdinalIgnoreCase) ? "test@example.com" : "Test User";
            });

        var configuration = new OnboardingConfiguration();
        var processRunner = CreateProcessRunner(new Dictionary<(string FileName, string Arguments, bool RequestElevation, bool UseShellExecute), ProcessResult>
        {
            { (FileName: "dism.exe", Arguments: "/online /Get-FeatureInfo /FeatureName:Microsoft-Windows-Subsystem-Linux", RequestElevation: false, UseShellExecute: false), Success("State : Enabled") },
            { (FileName: "dism.exe", Arguments: "/online /Get-FeatureInfo /FeatureName:VirtualMachinePlatform", RequestElevation: false, UseShellExecute: false), Success("State : Enabled") },
            { (FileName: "wsl.exe", Arguments: "-l -q", RequestElevation: false, UseShellExecute: false), Success("Ubuntu-22.04\r\n") },
            { (FileName: "wsl.exe", Arguments: "-d \"Ubuntu-22.04\" -- cat /etc/os-release", RequestElevation: false, UseShellExecute: true), Success(string.Empty) },
            { (FileName: "where", Arguments: "git.exe", RequestElevation: false, UseShellExecute: false), Success("C\\Git\\git.exe") },
            { (FileName: "where", Arguments: "gh.exe", RequestElevation: false, UseShellExecute: false), Success("C:\\GitHubCli\\gh.exe") },
            { (FileName: "where", Arguments: "code.cmd", RequestElevation: false, UseShellExecute: false), Success("C:\\VSCode\\code.cmd") },
            { (FileName: "powershell", Arguments: "-NoProfile -Command \"(Test-Path 'C:\\Program Files\\Docker\\Docker\\Docker Desktop.exe') -or (Test-Path (Join-Path $env:LOCALAPPDATA 'Programs\\Docker\\Docker Desktop.exe'))\"", RequestElevation: false, UseShellExecute: false), Success("True") },
            { (FileName: "cmd.exe", Arguments: "/c C:\\VSCode\\code.cmd --list-extensions", RequestElevation: false, UseShellExecute: false), Success("ms-vscode-remote.vscode-remote-extensionpack") },
            { (FileName: "cmd.exe", Arguments: BuildCredentialProbeArguments(configuration), RequestElevation: false, UseShellExecute: false), Success("username=user\r\npassword=secret") },
            { (FileName: "git", Arguments: "config --global user.name", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, string.Empty) },
            { (FileName: "git", Arguments: "config --global user.email", RequestElevation: false, UseShellExecute: false), new ProcessResult(1, string.Empty, string.Empty) },
            { (FileName: "git", Arguments: "config --global user.name \"Test User\"", RequestElevation: false, UseShellExecute: false), Success(string.Empty) },
            { (FileName: "git", Arguments: "config --global user.email \"test@example.com\"", RequestElevation: false, UseShellExecute: false), Success(string.Empty) },
        });

        var fileSystem = new FakeFileSystem(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { DotfilesSettingsPath, "{\"dotfiles.repository\":\"someone/dots\"}" },
            { DockerSettingsPath, "{\"IntegratedWslDistros\":[\"Ubuntu-22.04\"]}" },
        });

        var orchestrator = CreateOrchestrator(
            ui,
            processRunner,
            fileSystem,
            configuration,
            new ExecutionOptions(IsDryRun: false, IsVerbose: false));

        await orchestrator.ExecuteAsync().ConfigureAwait(false);

        Assert.That(capturedSummary.Last().StepName, Is.EqualTo("Configure Git user identity"));
        Assert.That(capturedSummary.Last().Status, Is.EqualTo(StepStatus.Executed));
    }

    private static WindowsOrchestrator CreateOrchestrator(
        Mock<IUserInteraction> ui,
        IProcessRunner processRunner,
        IFileSystem fileSystem,
        OnboardingConfiguration configuration,
        ExecutionOptions executionOptions)
    {
        var enableWslStep = new EnableWslFeaturesStep(processRunner, ui.Object, configuration);
        var installGitStep = new InstallGitForWindowsStep(processRunner, ui.Object);
        var installGitHubCliStep = new InstallGitHubCliStep(processRunner, ui.Object);
        var installVsCodeStep = new InstallWindowsVsCodeStep(processRunner, ui.Object);
        var extensionStep = new EnsureVsCodeRemoteExtensionPackStep(processRunner, ui.Object);
        var dotfilesStep = new ConfigureVsCodeDotfilesStep(ui.Object, fileSystem, () => DotfilesSettingsPath);
        var installDockerStep = new InstallDockerDesktopStep(processRunner, ui.Object, configuration);
        var dockerIntegrationStep = new ConfigureDockerDesktopWslIntegrationStep(processRunner, ui.Object, fileSystem, configuration, () => DockerAppDataPath);
        var preAuthStep = new PreAuthenticateGitCredentialManagerStep(processRunner, ui.Object, configuration);
        var configureGitStep = new ConfigureGitUserStep(processRunner, ui.Object);

        return new WindowsOrchestrator(
            ui.Object,
            executionOptions,
            enableWslStep,
            installGitStep,
            installGitHubCliStep,
            installVsCodeStep,
            extensionStep,
            dotfilesStep,
            installDockerStep,
            dockerIntegrationStep,
            preAuthStep,
            configureGitStep);
    }

    private static Mock<IUserInteraction> CreateUserInteractionMock(Action<IReadOnlyCollection<StepResult>> summaryCallback)
    {
        var ui = new Mock<IUserInteraction>(MockBehavior.Loose);

        ui.Setup(x => x.RunStatusAsync(It.IsAny<string>(), It.IsAny<Func<IStatusContext, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((string _, Func<IStatusContext, Task> action, CancellationToken token) => action(new TestStatusContext(ui.Object, token)));

        ui.Setup(x => x.ShowSummary(It.IsAny<IReadOnlyCollection<StepResult>>()))
            .Callback(summaryCallback);

        return ui;
    }

    private static FakeProcessRunner CreateProcessRunner(IReadOnlyDictionary<(string FileName, string Arguments, bool RequestElevation, bool UseShellExecute), ProcessResult> responses) => new(responses);

    private static ProcessResult Success(string stdout) => new(0, stdout, string.Empty);

    private static string BuildCredentialProbeArguments(OnboardingConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.GitCredentialManagerPath))
        {
            throw new InvalidOperationException("Git Credential Manager path must be configured for tests.");
        }

        string escapedPath = configuration.GitCredentialManagerPath.Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"/c \"set GCM_INTERACTIVE=never && (echo protocol=https & echo host=github.com & echo.) | \"{escapedPath}\" get\"";
    }

    private sealed class TestStatusContext : IStatusContext
    {
        private readonly IUserInteraction interaction;
        private readonly CancellationToken cancellationToken;

        public TestStatusContext(IUserInteraction interaction, CancellationToken cancellationToken)
        {
            this.interaction = interaction;
            this.cancellationToken = cancellationToken;
        }

        public void UpdateStatus(string status)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
        }

        public void WriteNormal(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteNormal(message);
        }

        public void WriteSuccess(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteSuccess(message);
        }

        public void WriteWarning(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteWarning(message);
        }

        public void WriteError(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteError(message);
        }

        public void WriteDebug(string message)
        {
            this.cancellationToken.ThrowIfCancellationRequested();
            this.interaction.WriteDebug(message);
        }
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        private readonly Dictionary<string, ProcessResult> responses;

        public FakeProcessRunner(IReadOnlyDictionary<(string FileName, string Arguments, bool RequestElevation, bool UseShellExecute), ProcessResult> responses)
        {
            this.responses = new Dictionary<string, ProcessResult>(responses.Count, StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<(string FileName, string Arguments, bool RequestElevation, bool UseShellExecute), ProcessResult> entry in responses)
            {
                string key = BuildKey(entry.Key.FileName, entry.Key.Arguments, entry.Key.RequestElevation, entry.Key.UseShellExecute);
                this.responses[key] = entry.Value;
            }
        }

        public Task<ProcessResult> RunAsync(string fileName, string arguments) => this.RunAsync(fileName, arguments, requestElevation: false, useShellExecute: false);

        public Task<ProcessResult> RunAsync(string fileName, string arguments, bool requestElevation) => this.RunAsync(fileName, arguments, requestElevation, useShellExecute: false);

        public Task<ProcessResult> RunAsync(string fileName, string arguments, bool requestElevation, bool useShellExecute)
        {
            string lookupKey = BuildKey(fileName, arguments, requestElevation, useShellExecute);

            if (!this.responses.TryGetValue(lookupKey, out ProcessResult? result) || result is null)
            {
                throw new InvalidOperationException($"Unexpected command: {fileName} {arguments} (elevated: {requestElevation}, shell: {useShellExecute})");
            }

            ProcessResult nonNullResult = result;
            return Task.FromResult(nonNullResult);
        }

        private static string BuildKey(string fileName, string arguments, bool requestElevation, bool useShellExecute)
        {
            string normalizedFileName = fileName.Trim().ToLowerInvariant();
            string normalizedArguments = NormalizeArguments(arguments);
            return string.Concat(normalizedFileName, "|", normalizedArguments, "|", requestElevation ? "1" : "0", "|", useShellExecute ? "1" : "0");
        }

        // Normalizes argument strings so tests remain resilient to path quoting and casing differences.
        private static string NormalizeArguments(string arguments)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                return string.Empty;
            }

            string normalized = arguments.Replace("\\\\", "\\", StringComparison.Ordinal);
            normalized = normalized.Replace("\"", string.Empty, StringComparison.Ordinal);

            while (normalized.Contains("  ", StringComparison.Ordinal))
            {
                normalized = normalized.Replace("  ", " ", StringComparison.Ordinal);
            }

            return normalized.Trim().ToLowerInvariant();
        }
    }

    private sealed class FakeFileSystem : IFileSystem
    {
        private readonly HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);

        public FakeFileSystem(IDictionary<string, string> initialFiles)
        {
            foreach (KeyValuePair<string, string> kvp in initialFiles)
            {
                this.files[kvp.Key] = kvp.Value;
                string? directory = System.IO.Path.GetDirectoryName(kvp.Key);
                if (!string.IsNullOrEmpty(directory))
                {
                    this.directories.Add(directory);
                }
            }

            this.directories.Add(System.IO.Path.GetDirectoryName(DotfilesSettingsPath)!);
            this.directories.Add(System.IO.Path.GetDirectoryName(DockerSettingsPath)!);
        }

        public bool DirectoryExists(string path) => this.directories.Contains(path);

        public void CreateDirectory(string path) => this.directories.Add(path);

        public bool FileExists(string path) => this.files.ContainsKey(path);

        public string ReadAllText(string path)
        {
            if (!this.files.TryGetValue(path, out string? content) || content is null)
            {
                throw new InvalidOperationException($"File not found: {path}");
            }

            string nonNullContent = content;
            return nonNullContent;
        }

        public void WriteAllText(string path, string contents)
        {
            this.files[path] = contents;
        }

        public void MoveFile(string sourcePath, string destinationPath, bool overwrite)
        {
            if (!this.files.TryGetValue(sourcePath, out string? content) || content is null)
            {
                throw new InvalidOperationException($"Move source missing: {sourcePath}");
            }

            if (!overwrite && this.files.ContainsKey(destinationPath))
            {
                throw new InvalidOperationException($"Destination exists: {destinationPath}");
            }

            string nonNullContent = content;
            this.files[destinationPath] = nonNullContent;
            this.files.Remove(sourcePath);
        }

        public void DeleteFile(string path)
        {
            this.files.Remove(path);
        }
    }
}
