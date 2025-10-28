// <copyright file="InstallMacVsCodeStep.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Steps.MacOs;

using System;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Installs Visual Studio Code on macOS using Homebrew.
/// </summary>
public class InstallMacVsCodeStep : IOnboardingStep
{
    private const string ApplicationPath = "/Applications/Visual Studio Code.app";
    private const string BrewCommand = "install --cask visual-studio-code";

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;
    private readonly IFileSystem fileSystem;

    public InstallMacVsCodeStep(
        IProcessRunner processRunner,
        IUserInteraction userInteraction,
        IFileSystem fileSystem)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
        this.fileSystem = fileSystem;
    }

    public string Description => "Install Visual Studio Code";

    public async Task<bool> ShouldExecuteAsync()
    {
        var result = await processRunner.RunAsync("which", "code").ConfigureAwait(false);
        if (CommandSucceeded(result))
        {
            return false;
        }

        return !fileSystem.DirectoryExists(ApplicationPath);
    }

    public async Task ExecuteAsync()
    {
        var result = await processRunner.RunAsync("brew", BrewCommand).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            string message = string.IsNullOrWhiteSpace(result.StandardError)
                ? "Failed to install Visual Studio Code via Homebrew."
                : result.StandardError.Trim();
            throw new InvalidOperationException(message);
        }

        userInteraction.WriteSuccess("Visual Studio Code installed via Homebrew.");
    }

    private static bool CommandSucceeded(ProcessResult result)
    {
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput);
    }
}
