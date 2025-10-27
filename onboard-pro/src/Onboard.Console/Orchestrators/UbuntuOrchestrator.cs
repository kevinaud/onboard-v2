// <copyright file="UbuntuOrchestrator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Console.Orchestrators;

using Onboard.Core.Abstractions;
using Onboard.Core.Steps.PlatformAware;
using Onboard.Core.Steps.Shared;
using Onboard.Core.Steps.Ubuntu;
using Onboard.Core.Steps.WslGuest;

/// <summary>
/// Orchestrator for Ubuntu (native Linux) onboarding.
/// </summary>
public class UbuntuOrchestrator : IPlatformOrchestrator
{
    private readonly IUserInteraction ui;
    private readonly IEnumerable<IOnboardingStep> steps;

    public UbuntuOrchestrator(
        IUserInteraction ui,
        AptUpdateStep aptUpdateStep,
        InstallAptPackagesStep installAptPackagesStep,
        InstallVsCodeStep installVsCodeStep,
        ConfigureGitUserStep configureGitUserStep,
        CloneProjectRepoStep cloneProjectRepoStep)
    {
        this.ui = ui;
        this.steps = new IOnboardingStep[]
        {
            aptUpdateStep,
            installAptPackagesStep,
            installVsCodeStep,
            configureGitUserStep,
            cloneProjectRepoStep,
        };
    }

    public async Task ExecuteAsync()
    {
        this.ui.WriteHeader("Starting Ubuntu Onboarding...");

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
        this.ui.WriteSuccess("Ubuntu onboarding complete!");
    }
}
