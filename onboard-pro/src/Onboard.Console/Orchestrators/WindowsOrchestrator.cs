// <copyright file="WindowsOrchestrator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Onboard.Console.Orchestrators;

using Onboard.Core.Abstractions;

/// <summary>
/// Orchestrator for Windows host onboarding.
/// </summary>
public class WindowsOrchestrator : IPlatformOrchestrator
{
    private readonly IUserInteraction ui;
    private readonly IEnumerable<IOnboardingStep> steps;

    public WindowsOrchestrator(
        IUserInteraction ui,
        IOnboardingStep configureGitUserStep)
    {
        this.ui = ui;
        this.steps = new[] { configureGitUserStep };
    }

    public async Task ExecuteAsync()
    {
        this.ui.WriteHeader("Starting Windows Host Onboarding...");

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
        this.ui.WriteSuccess("Windows onboarding complete!");
    }
}
