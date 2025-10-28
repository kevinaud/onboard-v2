// <copyright file="WindowsOrchestrator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Console.Orchestrators;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.PlatformAware;
using Onboard.Core.Steps.Shared;
using Onboard.Core.Steps.Windows;

/// <summary>
/// Orchestrator for Windows host onboarding.
/// </summary>
public class WindowsOrchestrator : SequentialOrchestrator
{
    public WindowsOrchestrator(
        IUserInteraction ui,
        ExecutionOptions executionOptions,
        EnableWslFeaturesStep enableWslFeaturesStep,
        InstallGitForWindowsStep installGitForWindowsStep,
        InstallVsCodeStep installVsCodeStep,
        InstallDockerDesktopStep installDockerDesktopStep,
        ConfigureGitUserStep configureGitUserStep)
        : base(
            ui,
            executionOptions,
            "Windows host onboarding",
            new IOnboardingStep[]
            {
                enableWslFeaturesStep,
                installGitForWindowsStep,
                installVsCodeStep,
                installDockerDesktopStep,
                configureGitUserStep,
            })
    {
    }
}
