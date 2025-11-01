// <copyright file="MacOsOrchestrator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Console.Orchestrators;

using Onboard.Core.Abstractions;
using Onboard.Core.Models;
using Onboard.Core.Steps.MacOs;
using Onboard.Core.Steps.Shared;

/// <summary>
/// Orchestrator for macOS onboarding.
/// </summary>
public class MacOsOrchestrator : SequentialOrchestrator
{
  public MacOsOrchestrator(
    IUserInteraction ui,
    ExecutionOptions executionOptions,
    InstallHomebrewStep installHomebrewStep,
    InstallBrewPackagesStep installBrewPackagesStep,
    InstallMacVsCodeStep installMacVsCodeStep,
    ConfigureGitUserStep configureGitUserStep,
    CloneProjectRepoStep cloneProjectRepoStep
  )
    : base(
      ui,
      executionOptions,
      "macOS onboarding",
      new IOnboardingStep[]
      {
        installHomebrewStep,
        installBrewPackagesStep,
        installMacVsCodeStep,
        configureGitUserStep,
        cloneProjectRepoStep,
      }
    ) { }
}
