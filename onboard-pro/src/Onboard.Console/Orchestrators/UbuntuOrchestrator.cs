// <copyright file="UbuntuOrchestrator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Console.Orchestrators;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.Linux;
using Onboard.Core.Steps.Shared;
using Onboard.Core.Steps.Ubuntu;
using Onboard.Core.Steps.WslGuest;

/// <summary>
/// Orchestrator for Ubuntu (native Linux) onboarding.
/// </summary>
public class UbuntuOrchestrator : SequentialOrchestrator
{
    public UbuntuOrchestrator(
        IUserInteraction ui,
        ExecutionOptions executionOptions,
        AptUpdateStep aptUpdateStep,
        InstallAptPackagesStep installAptPackagesStep,
        InstallLinuxVsCodeStep installLinuxVsCodeStep,
        ConfigureGitUserStep configureGitUserStep,
        CloneProjectRepoStep cloneProjectRepoStep)
        : base(
            ui,
            executionOptions,
            "Ubuntu onboarding",
            new IOnboardingStep[]
            {
                aptUpdateStep,
                installAptPackagesStep,
                installLinuxVsCodeStep,
                configureGitUserStep,
                cloneProjectRepoStep,
            })
    {
    }
}
