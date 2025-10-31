// <copyright file="InstallWindowsVsCodeStep.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Steps.Windows;

using System;
using System.Collections.Generic;
using System.IO;
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
    private readonly OnboardingConfiguration configuration;
    private readonly IEnvironmentRefresher environmentRefresher;

    public InstallWindowsVsCodeStep(IProcessRunner processRunner, IUserInteraction userInteraction, OnboardingConfiguration configuration, IEnvironmentRefresher environmentRefresher)
    {
        this.processRunner = processRunner;
        this.userInteraction = userInteraction;
        this.configuration = configuration;
        this.environmentRefresher = environmentRefresher;
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

        await environmentRefresher.RefreshAsync().ConfigureAwait(false);
        await CaptureCodeCliPathAsync().ConfigureAwait(false);
        userInteraction.WriteSuccess("Visual Studio Code installed via winget.");
    }

    private static bool CommandSucceeded(ProcessResult result)
    {
        return result.IsSuccess && !string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    private static IEnumerable<string> EnumerateLines(string input)
    {
        int start = 0;
        for (int i = 0; i < input.Length; i++)
        {
            char character = input[i];
            if (character == '\r' || character == '\n')
            {
                if (i > start)
                {
                    yield return input[start..i];
                }

                if (character == '\r' && i + 1 < input.Length && input[i + 1] == '\n')
                {
                    i++;
                }

                start = i + 1;
            }
        }

        if (start < input.Length)
        {
            yield return input[start..];
        }
    }

    private async Task CaptureCodeCliPathAsync()
    {
        var result = await processRunner.RunAsync("where", "code.cmd").ConfigureAwait(false);
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return;
        }

        foreach (string line in EnumerateLines(result.StandardOutput))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string candidate = line.Trim();
            if (!File.Exists(candidate))
            {
                continue;
            }

            configuration.VsCodeCliPath = candidate;
            break;
        }
    }
}
