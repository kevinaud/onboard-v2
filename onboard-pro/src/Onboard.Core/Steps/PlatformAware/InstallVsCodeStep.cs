// <copyright file="InstallVsCodeStep.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Steps.PlatformAware;

using System;
using System.IO;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

using OperatingSystem = Onboard.Core.Models.OperatingSystem;

/// <summary>
/// Installs Visual Studio Code using platform-specific tooling.
/// </summary>
public class InstallVsCodeStep : PlatformAwareStep
{
    private const string LinuxDownloadUrl = "https://update.code.visualstudio.com/latest/linux-deb-x64/stable";
    private const string LinuxPackagePath = "/tmp/vscode.deb";
    private const string MacApplicationPath = "/Applications/Visual Studio Code.app";

    private readonly IUserInteraction ui;

    public InstallVsCodeStep(PlatformFacts platformFacts, IProcessRunner processRunner, IUserInteraction ui)
        : base(platformFacts, processRunner)
    {
        this.ui = ui;

        AddStrategy(OperatingSystem.Windows, ShouldExecuteWindowsAsync, ExecuteWindowsAsync);
        AddStrategy(OperatingSystem.MacOs, ShouldExecuteMacAsync, ExecuteMacAsync);
        AddStrategy(OperatingSystem.Linux, ShouldExecuteLinuxAsync, ExecuteLinuxAsync);
    }

    public override string Description => "Install Visual Studio Code";

    private async Task<bool> ShouldExecuteWindowsAsync()
    {
        bool codeExists = await CommandExistsAsync("where", "code.cmd").ConfigureAwait(false);
        return !codeExists;
    }

    private async Task<bool> ShouldExecuteMacAsync()
    {
        if (await CommandExistsAsync("which", "code").ConfigureAwait(false))
        {
            return false;
        }

        return !Directory.Exists(MacApplicationPath);
    }

    private async Task<bool> ShouldExecuteLinuxAsync()
    {
        bool codeExists = await CommandExistsAsync("which", "code").ConfigureAwait(false);
        return !codeExists;
    }

    private async Task ExecuteWindowsAsync()
    {
        await RunOrThrowAsync(
            "winget",
            "install --id Microsoft.VisualStudioCode -e --source winget",
            "Failed to install Visual Studio Code via winget").ConfigureAwait(false);

        ui.WriteSuccess("Visual Studio Code installed via winget.");
    }

    private async Task ExecuteMacAsync()
    {
        await RunOrThrowAsync(
            "brew",
            "install --cask visual-studio-code",
            "Failed to install Visual Studio Code via Homebrew").ConfigureAwait(false);

        ui.WriteSuccess("Visual Studio Code installed via Homebrew.");
    }

    private async Task ExecuteLinuxAsync()
    {
        await RunOrThrowAsync(
            "curl",
            $"-L \"{LinuxDownloadUrl}\" -o \"{LinuxPackagePath}\"",
            "Failed to download Visual Studio Code package").ConfigureAwait(false);

        try
        {
            await RunOrThrowAsync(
                "sudo",
                $"apt-get install -y \"{LinuxPackagePath}\"",
                "Failed to install Visual Studio Code package").ConfigureAwait(false);
        }
        finally
        {
            await ProcessRunner.RunAsync("rm", $"-f \"{LinuxPackagePath}\"").ConfigureAwait(false);
        }

        ui.WriteSuccess("Visual Studio Code installed via apt.");
    }

    private async Task<bool> CommandExistsAsync(string command, string arguments)
    {
        var result = await ProcessRunner.RunAsync(command, arguments).ConfigureAwait(false);
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    private async Task RunOrThrowAsync(string command, string arguments, string errorMessage)
    {
        var result = await ProcessRunner.RunAsync(command, arguments).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"{errorMessage}: {result.StandardError}");
        }
    }
}
