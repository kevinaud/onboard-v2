// <copyright file="InstallVsCodeStep.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Core.Steps.PlatformAware;

using Onboard.Core.Abstractions;

/// <summary>
/// Placeholder step for installing VS Code.
/// </summary>
public class InstallVsCodeStep : IOnboardingStep
{
    public string Description => "Install Visual Studio Code";

    public Task<bool> ShouldExecuteAsync()
    {
        return Task.FromResult(false);
    }

    public Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
