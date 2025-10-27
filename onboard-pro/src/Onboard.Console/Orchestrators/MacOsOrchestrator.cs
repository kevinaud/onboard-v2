// <copyright file="MacOsOrchestrator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Console.Orchestrators;

using Onboard.Core.Abstractions;
using Onboard.Core.Steps.MacOs;
using Onboard.Core.Steps.PlatformAware;
using Onboard.Core.Steps.Shared;

/// <summary>
/// Orchestrator for macOS onboarding.
/// </summary>
public class MacOsOrchestrator : IPlatformOrchestrator
{
    private readonly IUserInteraction ui;
    private readonly IEnumerable<IOnboardingStep> steps;

    public MacOsOrchestrator(
        IUserInteraction ui,
        InstallHomebrewStep installHomebrewStep,
        InstallBrewPackagesStep installBrewPackagesStep,
        InstallVsCodeStep installVsCodeStep,
        ConfigureGitUserStep configureGitUserStep,
        CloneProjectRepoStep cloneProjectRepoStep)
    {
        this.ui = ui;
        this.steps = new IOnboardingStep[]
        {
            installHomebrewStep,
            installBrewPackagesStep,
            installVsCodeStep,
            configureGitUserStep,
            cloneProjectRepoStep,
        };
    }

    public async Task ExecuteAsync()
    {
        this.ui.WriteHeader("Starting macOS Onboarding...");

        foreach (var step in this.steps)
        {
            this.ui.WriteLine($"Checking: {step.Description}");

            if (await step.ShouldExecuteAsync().ConfigureAwait(false))
            {
                this.ui.WriteLine($"Executing: {step.Description}");
                await step.ExecuteAsync().ConfigureAwait(false);
            }
            else
            {
                this.ui.WriteSuccess($"Already configured: {step.Description}");
            }
        }

        this.ui.WriteLine(string.Empty);
        this.ui.WriteSuccess("macOS onboarding complete!");
    }
}
