// <copyright file="WslGuestOrchestrator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Console.Orchestrators;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.Linux;
using Onboard.Core.Steps.Shared;
using Onboard.Core.Steps.WslGuest;

/// <summary>
/// Orchestrator for WSL guest onboarding.
/// </summary>
public class WslGuestOrchestrator : SequentialOrchestrator
{
    public WslGuestOrchestrator(
        IUserInteraction ui,
        ExecutionOptions executionOptions,
        AptUpdateStep aptUpdateStep,
        InstallWslPrerequisitesStep installWslPrerequisitesStep,
        InstallLinuxVsCodeStep installLinuxVsCodeStep,
        ConfigureWslGitCredentialHelperStep configureWslGitCredentialHelperStep,
        ConfigureGitUserStep configureGitUserStep,
        CloneProjectRepoStep cloneProjectRepoStep)
        : base(
            ui,
            executionOptions,
            "WSL guest onboarding",
            new IOnboardingStep[]
            {
                aptUpdateStep,
                installWslPrerequisitesStep,
                installLinuxVsCodeStep,
                configureWslGitCredentialHelperStep,
                configureGitUserStep,
                cloneProjectRepoStep,
            })
    {
    }
}
