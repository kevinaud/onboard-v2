// <copyright file="InstallGitForWindowsStep.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Steps.Windows;

using System;
using System.Threading.Tasks;

using Onboard.Core.Abstractions;

/// <summary>
/// Ensures Git for Windows is installed via winget.
/// </summary>
public class InstallGitForWindowsStep : IOnboardingStep
{
    private const string WingetCommand = "install --id Git.Git -e --source winget --accept-package-agreements --accept-source-agreements --disable-interactivity";

    private readonly IProcessRunner processRunner;
    private readonly IUserInteraction userInteraction;
    private readonly IEnvironmentRefresher environmentRefresher;

    public InstallGitForWindowsStep(IProcessRunner processRunner, IUserInteraction userInteraction, IEnvironmentRefresher environmentRefresher)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
        this.environmentRefresher = environmentRefresher;
    }

    public string Description => "Install Git for Windows";

    public async Task<bool> ShouldExecuteAsync()
    {
        var result = await processRunner.RunAsync("where", "git.exe").ConfigureAwait(false);
        return !result.IsSuccess;
    }

    public async Task ExecuteAsync()
    {
        var result = await processRunner.RunAsync("winget", WingetCommand).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            string message = string.IsNullOrWhiteSpace(result.StandardError)
                ? "winget failed to install Git for Windows."
                : result.StandardError.Trim();
            throw new InvalidOperationException(message);
        }

        await environmentRefresher.RefreshAsync().ConfigureAwait(false);
        userInteraction.WriteSuccess("Git for Windows installed via winget.");
    }
}
