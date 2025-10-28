// <copyright file="InstallWindowsVsCodeStep.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Steps.Windows;

using System;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;

/// <summary>
/// Installs Visual Studio Code on Windows via winget.
/// </summary>
public class InstallWindowsVsCodeStep : IOnboardingStep
{
    private const string WingetCommand = "install --id Microsoft.VisualStudioCode -e --source winget";

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;

    public InstallWindowsVsCodeStep(IProcessRunner processRunner, IUserInteraction userInteraction)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
    }

    public string Description => "Install Visual Studio Code";

    public async Task<bool> ShouldExecuteAsync()
    {
        var result = await processRunner.RunAsync("where", "code.cmd").ConfigureAwait(false);
        return !CommandSucceeded(result);
    }

    public async Task ExecuteAsync()
    {
        var result = await processRunner.RunAsync("winget", WingetCommand).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            string message = string.IsNullOrWhiteSpace(result.StandardError)
                ? "Failed to install Visual Studio Code via winget."
                : result.StandardError.Trim();
            throw new InvalidOperationException(message);
        }

        userInteraction.WriteSuccess("Visual Studio Code installed via winget.");
    }

    private static bool CommandSucceeded(ProcessResult result)
    {
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput);
    }
}
