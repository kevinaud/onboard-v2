// <copyright file="InstallLinuxVsCodeStep.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Steps.Linux;

using System;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Installs Visual Studio Code on Linux distributions using apt.
/// </summary>
public class InstallLinuxVsCodeStep : IOnboardingStep
{
    private const string DownloadUrl = "https://update.code.visualstudio.com/latest/linux-deb-x64/stable";
    private const string PackagePath = "/tmp/vscode.deb";

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;

    public InstallLinuxVsCodeStep(IProcessRunner processRunner, IUserInteraction userInteraction)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
    }

    public string Description => "Install Visual Studio Code";

    public async Task<bool> ShouldExecuteAsync()
    {
        var result = await processRunner.RunAsync("which", "code").ConfigureAwait(false);
        return !CommandSucceeded(result);
    }

    public async Task ExecuteAsync()
    {
        await RunOrThrowAsync(
            "curl",
            $"-L \"{DownloadUrl}\" -o \"{PackagePath}\"",
            "Failed to download Visual Studio Code package").ConfigureAwait(false);

        try
        {
            await RunOrThrowAsync(
                "sudo",
                $"apt-get install -y \"{PackagePath}\"",
                "Failed to install Visual Studio Code package").ConfigureAwait(false);
        }
        finally
        {
            await processRunner.RunAsync("rm", $"-f \"{PackagePath}\"").ConfigureAwait(false);
        }

        userInteraction.WriteSuccess("Visual Studio Code installed via apt.");
    }

    private static bool CommandSucceeded(ProcessResult result)
    {
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    private async Task RunOrThrowAsync(string fileName, string arguments, string errorMessage)
    {
        var result = await processRunner.RunAsync(fileName, arguments).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            string message = string.IsNullOrWhiteSpace(result.StandardError)
                ? errorMessage
                : $"{errorMessage}: {result.StandardError.Trim()}";
            throw new InvalidOperationException(message);
        }
    }
}
