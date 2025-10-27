// <copyright file="WslGuestOrchestrator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Console.Orchestrators;

using Onboard.Core.Abstractions;
using Onboard.Core.Steps.PlatformAware;
using Onboard.Core.Steps.Shared;
using Onboard.Core.Steps.WslGuest;

/// <summary>
/// Orchestrator for WSL guest onboarding.
/// </summary>
public class WslGuestOrchestrator : IPlatformOrchestrator
{
    private readonly IUserInteraction ui;
    private readonly IEnumerable<IOnboardingStep> steps;

    public WslGuestOrchestrator(
        IUserInteraction ui,
        AptUpdateStep aptUpdateStep,
        InstallWslPrerequisitesStep installWslPrerequisitesStep,
        InstallVsCodeStep installVsCodeStep,
        ConfigureWslGitCredentialHelperStep configureWslGitCredentialHelperStep,
        ConfigureGitUserStep configureGitUserStep)
    {
        this.ui = ui;
        this.steps = new IOnboardingStep[]
        {
            aptUpdateStep,
            installWslPrerequisitesStep,
            installVsCodeStep,
            configureWslGitCredentialHelperStep,
            configureGitUserStep,
        };
    }

    public async Task ExecuteAsync()
    {
        this.ui.WriteHeader("Starting WSL Guest Onboarding...");

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
        this.ui.WriteSuccess("WSL Guest onboarding complete!");
    }
}
